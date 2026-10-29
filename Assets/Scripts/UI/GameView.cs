using UnityEngine;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 可玩可视化层（Batch 6 简化雏形，提前实现）。
    /// 订阅 GameEvents 把 GameState 变成画面：滚动公路 + 车辆 + 小人 + 宝箱弹出 + 天气/区域色调。
    /// 精灵从 Resources/Art/Sprites 载入（Resources.Load），编辑器与 Windows 构建通用，无需手动拖拽。
    /// 说明：离散变化走事件；每帧只读取 currentSpeed / currentVehicle 做动画，不反写状态。
    /// </summary>
    public class GameView : MonoBehaviour
    {
        [Header("车辆（2 帧轮子）")]
        public Sprite pickup0, pickup1, van0, van1, moto0, moto1;
        [Header("小人（idle 常驻；副本待处理时走 walk0/walk1）")]
        public Sprite survivorIdle, survivorWalk0, survivorWalk1;
        [Header("宝箱")]
        public Sprite chestSprite;
        [Header("区域（0 城郊 / 1 荒漠 / 2 冻原）")]
        public Sprite[] zoneBg = new Sprite[3];
        public Sprite[] zoneRoad = new Sprite[3];
        [Header("天气（0 晴 / 1 暴雨 / 2 沙暴 / 3 极寒）")]
        public Sprite[] weatherIcon = new Sprite[4];

        private SpriteRenderer _bg, _vehicle, _survivor, _chest, _wIcon;
        private SpriteRenderer[] _roadTiles;
        private float _roadTileW = 2.56f;
        private const int RoadCount = 5;

        private Vector3 _vehBase, _survBase, _chestBase;
        private float _animT;
        private int _frame;
        private float _chestT = -1f;
        private const float ChestLife = 0.9f;
        private Color _zoneTint = Color.white, _weatherTint = Color.white;

        private GameState State => ServiceLocator.Get<GameState>();

        private void Awake()
        {
            LoadSprites();
            BuildScene();
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
            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 2.6f;
                cam.transform.position = new Vector3(0.6f, 0.9f, -10f);
            }

            // 背景铺满相机视野（消除四周黑边），天气图标定位到真正的右上角
            FitBackground(cam);
            PlaceWeatherIcon(cam);

            var s = State;
            if (s != null)
            {
                ApplyZone(s.currentZoneIndex);
                ApplyWeather(s.currentWeather);
                ApplySurvivor(s.survivorState);
                if (_vehicle != null) _vehicle.sprite = VehicleSprite((int)s.currentVehicle, 0);
            }
        }

        private void BuildScene()
        {
            _roadTileW = (zoneRoad != null && zoneRoad.Length > 0 && zoneRoad[0] != null)
                ? zoneRoad[0].bounds.size.x : 2.56f;

            _bg = Make("BG", First(zoneBg), -20, new Vector3(0f, 1.1f, 0f), Vector3.one);

            // 多块路带平铺，保证横向铺满相机视野
            _roadTiles = new SpriteRenderer[RoadCount];
            for (int i = 0; i < RoadCount; i++)
            {
                _roadTiles[i] = Make("Road" + i, First(zoneRoad), -10,
                    new Vector3(i * _roadTileW, -0.35f, 0f), Vector3.one);
            }

            _chest = Make("Chest", chestSprite, 5, new Vector3(1.1f, 0.3f, 0f), Vector3.one);
            if (_chest != null) { _chest.enabled = false; _chestBase = _chest.transform.localPosition; }

            // 车辆：抬高到路面之上（轮子落在路面上，不再"陷"进地面）
            _vehicle = Make("Vehicle", VehicleSprite(0, 0), 10, new Vector3(0f, 1.05f, 0f), Vector3.one);
            if (_vehicle != null) _vehBase = _vehicle.transform.localPosition;

            // 小人：明显站在车辆左侧的地面上，缩小比例，加走路摆动 —— 解决"贴在车窗"的观感
            _survivor = Make("Survivor", survivorIdle, 11, new Vector3(-1.9f, 0.69f, 0f), new Vector3(0.55f, 0.55f, 1f));
            if (_survivor != null) _survBase = _survivor.transform.localPosition;

            _wIcon = Make("Weather", First(weatherIcon), 30, new Vector3(1.7f, 1.95f, 0f), Vector3.one * 0.8f);
        }

        private void FitBackground(Camera cam)
        {
            if (_bg == null || _bg.sprite == null || cam == null) return;
            float vh = cam.orthographicSize * 2f;
            float vw = vh * cam.aspect;
            var native = _bg.sprite.bounds.size;
            float s = Mathf.Max(vw / native.x, vh / native.y) * 1.05f; // cover + 余量
            _bg.transform.localScale = new Vector3(s, s, 1f);
            _bg.transform.localPosition = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
        }

        private void PlaceWeatherIcon(Camera cam)
        {
            if (_wIcon == null || cam == null) return;
            float vh = cam.orthographicSize * 2f;
            float vw = vh * cam.aspect;
            float right = cam.transform.position.x + vw / 2f;
            float top = cam.transform.position.y + vh / 2f;
            _wIcon.transform.localPosition = new Vector3(right - 0.65f, top - 0.65f, 0f);
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
            float dt = Time.unscaledDeltaTime;
            bool moving = s.currentSpeedKmPerTick > 0.0001f;

            // 滚动公路（视觉速度放大，天气减速肉眼可见）
            float step = s.currentSpeedKmPerTick * 30f * dt;
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
            if (road.transform.localPosition.x <= -_roadTileW)
                road.transform.localPosition += Vector3.right * (_roadTileW * RoadCount);
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
            if (_bg != null) _bg.color = _zoneTint * _weatherTint;
            if (_wIcon != null && weatherIcon != null && (int)w < weatherIcon.Length)
                _wIcon.sprite = weatherIcon[(int)w]; // 天气图标始终显示（晴/雨/沙/雪）
        }

        private void ApplyZone(int idx)
        {
            idx = Mathf.Clamp(idx, 0, 2);
            _zoneTint = ZoneTint(idx);
            if (_bg != null && zoneBg != null && idx < zoneBg.Length) _bg.sprite = zoneBg[idx];
            if (_roadTiles != null && zoneRoad != null && idx < zoneRoad.Length)
                for (int i = 0; i < _roadTiles.Length; i++)
                    if (_roadTiles[i] != null) _roadTiles[i].sprite = zoneRoad[idx];
            if (_bg != null) _bg.color = _zoneTint * _weatherTint;
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

            zoneBg = new Sprite[3];
            zoneRoad = new Sprite[3];
            zoneBg[0] = LoadSprite("Art/Sprites/Zones/bg_suburb");
            zoneBg[1] = LoadSprite("Art/Sprites/Zones/bg_desert");
            zoneBg[2] = LoadSprite("Art/Sprites/Zones/bg_tundra");
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
