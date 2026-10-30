using System.Collections;
using UnityEngine;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 桌面宠物可视化层（Batch 5 打磨）：底部路带 + 车辆 + 小人 + 宝箱弹出 + 天气图标。
    /// 关键改动（解决"车占满屏 / 挡住桌面"）：
    ///  1) 不画整屏不透明背景 —— 除路带/车/小人外全部透明，桌面应用可见（SPEC 2.2 + 9.6）。
    ///  2) 宠物贴在屏幕底部，按相机实际视野动态布局，分辨率变化自动重排。
    ///  3) 车的大小由 Bootstrapper 的 PixelPerfect 参考分辨率（1920x1080）控制，约为屏高 15%。
    /// 精灵从 Resources/Art/Sprites 载入（Resources.Load），编辑器与 Windows 构建通用。
    /// 离散变化走事件；每帧只读取 currentSpeed / currentVehicle 做动画，不反写状态。
    /// </summary>
    public class GameView : MonoBehaviour
    {
        [Header("车辆（2 帧轮子）")]
        public Sprite pickup0, pickup1, van0, van1, moto0, moto1;
        [Header("小人（idle 常驻；副本待处理时走 walk0/walk1）")]
        public Sprite survivorIdle, survivorWalk0, survivorWalk1;
        [Header("宝箱")]
        public Sprite chestSprite;
        [Header("区域路带（0 城郊 / 1 荒漠 / 2 冻原）")]
        public Sprite[] zoneRoad = new Sprite[3];
        [Header("天气（0 晴 / 1 暴雨 / 2 沙暴 / 3 极寒）")]
        public Sprite[] weatherIcon = new Sprite[4];

        private SpriteRenderer _vehicle, _survivor, _chest, _wIcon;
        private SpriteRenderer[] _roadTiles;
        private float _roadTileW = 2.56f;
        private float _roadTileH = 1.28f;
        private float _leftBound;

        private Vector3 _vehBase, _survBase, _chestBase;
        private float _animT;
        private int _frame;
        private float _chestT = -1f;
        private const float ChestLife = 0.9f;
        private Color _zoneTint = Color.white, _weatherTint = Color.white;
        private Vector2 _lastRes;
        private bool _relaying;

        private GameState State => ServiceLocator.Get<GameState>();

        private void Awake()
        {
            LoadSprites();
        }

        private void OnEnable()
        {
            GameEvents.OnChestOpened += HandleChest;
            GameEvents.OnWeatherChanged += HandleWeather;
            GameEvents.OnZoneChanged += HandleZone;
            GameEvents.OnSurvivorStateChanged += HandleSurvivor;
        }

        private void OnDisable()
        {
            GameEvents.OnChestOpened -= HandleChest;
            GameEvents.OnWeatherChanged -= HandleWeather;
            GameEvents.OnZoneChanged -= HandleZone;
            GameEvents.OnSurvivorStateChanged -= HandleSurvivor;
        }

        private void Start()
        {
            _lastRes = new Vector2(Screen.width, Screen.height);
            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.transform.position = new Vector3(0f, 0f, -10f); // 相机居中，底部 = -orthographicSize
            }
            StartCoroutine(DeferLayout());
        }

        private IEnumerator DeferLayout()
        {
            // 等两帧，确保 PixelPerfectCamera 已设定好 orthographicSize 再布局
            yield return null;
            yield return null;
            _relaying = false;
            Layout();
            var s = State;
            if (s != null)
            {
                ApplyZone(s.currentZoneIndex);
                ApplyWeather(s.currentWeather);
                ApplySurvivor(s.survivorState);
                if (_vehicle != null) _vehicle.sprite = VehicleSprite((int)s.currentVehicle, 0);
            }
        }

        private void Layout()
        {
            // 清空旧精灵（分辨率变化重排时复用）
            var children = new System.Collections.Generic.List<Transform>();
            foreach (Transform t in transform) children.Add(t);
            foreach (var t in children) Destroy(t.gameObject);
            _roadTiles = null;

            var cam = Camera.main;
            if (cam == null) return;

            // 用实时投影矩阵换算屏幕四角的世界坐标（兼容 PixelPerfectCamera 修改后的视口），
            // 不依赖 cam.orthographicSize 的读取值，避免布局与渲染错位导致路带没真正贴底。
            // z=10 表示落到相机前方 10 单位的平面（相机在 z=-10，即世界 z=0 平面）。
            Vector3 bl = cam.ScreenToWorldPoint(new Vector3(0f, 0f, 10f));
            Vector3 br = cam.ScreenToWorldPoint(new Vector3(Screen.width, 0f, 10f));
            Vector3 tl = cam.ScreenToWorldPoint(new Vector3(0f, Screen.height, 10f));
            float bottomY = bl.y;
            float topY = tl.y;
            float leftX = bl.x;
            float rightX = br.x;
            float centerX = (leftX + rightX) / 2f;

            // 路带实际尺寸
            var r0 = (zoneRoad != null && zoneRoad.Length > 0) ? zoneRoad[0] : null;
            if (r0 != null) { _roadTileW = r0.bounds.size.x; _roadTileH = r0.bounds.size.y; }

            // 路带：贴底平铺，覆盖整屏宽度
            float span = rightX - leftX;
            int count = Mathf.CeilToInt((span + _roadTileW * 2f) / _roadTileW);
            _leftBound = leftX - _roadTileW;
            _roadTiles = new SpriteRenderer[count];
            float roadCenterY = bottomY + _roadTileH / 2f; // 路带底边贴屏幕底
            for (int i = 0; i < count; i++)
            {
                var sr = Make("Road" + i, First(zoneRoad), -10,
                    new Vector3(_leftBound + i * _roadTileW, roadCenterY, 0f), Vector3.one);
                if (sr != null) sr.color = _zoneTint * _weatherTint;
                _roadTiles[i] = sr;
            }

            float roadTop = bottomY + _roadTileH;

            // 宝箱：路面上方弹出（靠右一点，避免挡车）
            _chest = Make("Chest", chestSprite, 5, new Vector3(centerX + 2.2f, roadTop + 0.4f, 0f), Vector3.one);
            if (_chest != null) { _chest.enabled = false; _chestBase = _chest.transform.localPosition; }

            // 车辆：轮子落在路面上，居中偏左
            float carH = (VehicleSprite(0, 0) != null ? VehicleSprite(0, 0).bounds.size.y : 1.6f);
            float carW = (VehicleSprite(0, 0) != null ? VehicleSprite(0, 0).bounds.size.x : 2.56f);
            _vehicle = Make("Vehicle", VehicleSprite(0, 0), 10,
                new Vector3(centerX - 1.5f, roadTop + carH / 2f, 0f), Vector3.one);
            if (_vehicle != null) _vehBase = _vehicle.transform.localPosition;

            // 小人：站在车辆左侧路面上
            float survH = (survivorIdle != null ? survivorIdle.bounds.size.y : 1.6f);
            float survW = (survivorIdle != null ? survivorIdle.bounds.size.x : 0.96f);
            _survivor = Make("Survivor", survivorIdle, 11,
                new Vector3(centerX - 1.5f - carW / 2f - survW / 2f - 0.15f, roadTop + survH / 2f, 0f),
                new Vector3(0.55f, 0.55f, 1f));
            if (_survivor != null) _survBase = _survivor.transform.localPosition;

            // 天气图标：右上角小图标（浮在透明区，不挡桌面）
            _wIcon = Make("Weather", First(weatherIcon), 30,
                new Vector3(rightX - 0.5f, topY - 0.5f, 0f), Vector3.one * 0.8f);

            _lastRes = new Vector2(Screen.width, Screen.height);
        }

        private Sprite First(Sprite[] a) => (a != null && a.Length > 0) ? a[0] : null;

        private SpriteRenderer Make(string name, Sprite sp, int order, Vector3 pos, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = order;
            return sr;
        }

        private void Update()
        {
            var s = State;
            if (s == null) return;

            // 分辨率变化 → 重新布局（避免缩放/拖动窗口后错位）
            if (!_relaying && (Screen.width != (int)_lastRes.x || Screen.height != (int)_lastRes.y))
            {
                _relaying = true;
                StartCoroutine(DeferLayout());
            }

            float dt = Time.unscaledDeltaTime;
            bool moving = s.currentSpeedKmPerTick > 0.0001f;

            // 滚动公路（视觉速度放大，天气减速肉眼可见）
            float step = s.currentSpeedKmPerTick * 30f * dt;
            if (_roadTiles != null)
                for (int i = 0; i < _roadTiles.Length; i++)
                    ScrollRoad(_roadTiles[i], step);

            // 帧动画（轮子 / 走路）：公路在动 或 副本待处理（小人在副本里走动）时推进
            bool animate = moving || s.pendingDungeon.HasValue;
            if (animate)
            {
                _animT += dt;
                if (_animT >= 0.12f) { _animT = 0f; _frame ^= 1; }
            }

            if (_vehicle != null)
            {
                var p = _vehBase;
                if (moving) p.y += Mathf.Sin(Time.time * 9f) * 0.03f;
                _vehicle.transform.localPosition = p;
                _vehicle.sprite = VehicleSprite((int)s.currentVehicle, _frame);
            }

            if (_survivor != null)
            {
                // 副本待处理：小人走向副本（走路帧 + 左右轻摆），用于验收移动能力
                if (s.pendingDungeon.HasValue && survivorWalk0 != null && survivorWalk1 != null)
                {
                    var p = _survBase;
                    p.x += Mathf.Sin(Time.time * 6f) * 0.08f;
                    p.y += Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.03f;
                    _survivor.transform.localPosition = p;
                    _survivor.sprite = _frame == 0 ? survivorWalk0 : survivorWalk1;
                }
                else
                {
                    // 常驻待机：不跟随行走
                    _survivor.sprite = survivorIdle;
                    _survivor.transform.localPosition = _survBase;
                }
            }

            // 宝箱弹出
            if (_chestT >= 0f && _chest != null)
            {
                _chestT += dt;
                float k = _chestT / ChestLife;
                if (k >= 1f) { _chest.enabled = false; _chestT = -1f; }
                else
                {
                    var p = _chestBase; p.y += 0.7f * k;
                    _chest.transform.localPosition = p;
                    var c = _chest.color; c.a = 1f - k; _chest.color = c;
                }
            }
        }

        private void ScrollRoad(SpriteRenderer road, float step)
        {
            if (road == null) return;
            road.transform.localPosition += Vector3.left * step;
            if (road.transform.localPosition.x <= _leftBound)
                road.transform.localPosition += Vector3.right * (_roadTileW * _roadTiles.Length);
        }

        private Sprite VehicleSprite(int kind, int f)
        {
            switch (kind)
            {
                case 1: return f == 0 ? van0 : van1;
                case 2: return f == 0 ? moto0 : moto1;
                default: return f == 0 ? pickup0 : pickup1;
            }
        }

        private void HandleChest(ChestOpenResult r)
        {
            if (_chest == null) return;
            _chest.enabled = true;
            _chestT = 0f;
            _chest.transform.localPosition = _chestBase;
            var c = _chest.color; c.a = 1f; _chest.color = c;
        }

        private void HandleWeather(WeatherId w) => ApplyWeather(w);
        private void HandleZone(int idx) => ApplyZone(idx);
        private void HandleSurvivor(SurvivorState st) => ApplySurvivor(st);

        private void ApplyWeather(WeatherId w)
        {
            _weatherTint = WeatherTint(w);
            ApplyTints();
            if (_wIcon != null && weatherIcon != null && (int)w < weatherIcon.Length)
                _wIcon.sprite = weatherIcon[(int)w]; // 天气图标始终显示（晴/雨/沙/雪）
        }

        private void ApplyZone(int idx)
        {
            idx = Mathf.Clamp(idx, 0, 2);
            _zoneTint = ZoneTint(idx);
            if (_roadTiles != null && zoneRoad != null && idx < zoneRoad.Length)
                for (int i = 0; i < _roadTiles.Length; i++)
                    if (_roadTiles[i] != null) _roadTiles[i].sprite = zoneRoad[idx];
            ApplyTints();
        }

        private void ApplyTints()
        {
            if (_roadTiles == null) return;
            var c = _zoneTint * _weatherTint;
            for (int i = 0; i < _roadTiles.Length; i++)
                if (_roadTiles[i] != null) _roadTiles[i].color = c;
        }

        private void ApplySurvivor(SurvivorState st)
        {
            if (_survivor != null)
                _survivor.color = st == SurvivorState.Weakened ? new Color(1f, 0.45f, 0.45f) : Color.white;
        }

        private static Color ZoneTint(int idx)
        {
            switch (idx)
            {
                case 1: return new Color(1f, 0.9f, 0.75f);
                case 2: return new Color(0.85f, 0.95f, 1f);
                default: return Color.white;
            }
        }

        private static Color WeatherTint(WeatherId w)
        {
            switch (w)
            {
                case WeatherId.Rainstorm: return new Color(0.72f, 0.82f, 1f);
                case WeatherId.Sandstorm: return new Color(1f, 0.86f, 0.62f);
                case WeatherId.ExtremeCold: return new Color(0.8f, 0.95f, 1f);
                default: return Color.white;
            }
        }

        /// <summary>
        /// 从 Resources/Art/Sprites 载入精灵（编辑器与 Windows 构建通用）。
        /// 原先用 AssetDatabase 仅在编辑器生效，打包后精灵字段全空导致画面空白；
        /// 改为 Resources.Load 后，构建内也能正确加载。
        /// </summary>
        private void LoadSprites()
        {
            pickup0 = LoadSprite("Art/Sprites/Vehicles/pickup_0");
            pickup1 = LoadSprite("Art/Sprites/Vehicles/pickup_1");
            van0 = LoadSprite("Art/Sprites/Vehicles/van_0");
            van1 = LoadSprite("Art/Sprites/Vehicles/van_1");
            moto0 = LoadSprite("Art/Sprites/Vehicles/moto_0");
            moto1 = LoadSprite("Art/Sprites/Vehicles/moto_1");

            survivorIdle = LoadSprite("Art/Sprites/Survivor/survivor_idle");
            survivorWalk0 = LoadSprite("Art/Sprites/Survivor/survivor_walk0");
            survivorWalk1 = LoadSprite("Art/Sprites/Survivor/survivor_walk1");

            chestSprite = LoadSprite("Art/Sprites/Chests/chest_random");

            zoneRoad = new Sprite[3];
            zoneRoad[0] = LoadSprite("Art/Sprites/Zones/road_suburb");
            zoneRoad[1] = LoadSprite("Art/Sprites/Zones/road_desert");
            zoneRoad[2] = LoadSprite("Art/Sprites/Zones/road_tundra");

            weatherIcon = new Sprite[4];
            weatherIcon[0] = LoadSprite("Art/Sprites/Weather/weather_sunny");
            weatherIcon[1] = LoadSprite("Art/Sprites/Weather/weather_rain");
            weatherIcon[2] = LoadSprite("Art/Sprites/Weather/weather_sandstorm");
            weatherIcon[3] = LoadSprite("Art/Sprites/Weather/weather_blizzard");
        }

        private static Sprite LoadSprite(string path)
        {
            var s = Resources.Load<Sprite>(path);
            if (s != null && s.texture != null)
                s.texture.filterMode = FilterMode.Point; // 保持像素锐利
            return s;
        }
    }
}
