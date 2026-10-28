using UnityEngine;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 可玩可视化层（Batch 6 简化雏形，提前实现）。
    /// 订阅 GameEvents 把 GameState 变成画面：滚动公路 + 车辆 + 小人 + 宝箱弹出 + 天气/区域色调。
    /// 精灵在 Editor 下自动从 Assets/Art/Sprites 载入（AssetDatabase），Play 即可见，无需手动拖拽。
    /// 说明：离散变化走事件；每帧只读取 currentSpeed / currentVehicle 做动画，不反写状态。
    /// </summary>
    public class GameView : MonoBehaviour
    {
        [Header("车辆（2 帧轮子）")]
        public Sprite pickup0, pickup1, van0, van1, moto0, moto1;
        [Header("小人（idle / walk 循环）")]
        public Sprite survivorIdle, survivorWalk0, survivorWalk1;
        [Header("宝箱")]
        public Sprite chestSprite;
        [Header("区域（0 城郊 / 1 荒漠 / 2 冻原）")]
        public Sprite[] zoneBg = new Sprite[3];
        public Sprite[] zoneRoad = new Sprite[3];
        [Header("天气（0 晴 / 1 暴雨 / 2 沙暴 / 3 极寒）")]
        public Sprite[] weatherIcon = new Sprite[4];

        private SpriteRenderer _bg, _roadA, _roadB, _vehicle, _survivor, _chest, _wIcon;
        private Vector3 _vehBase, _survBase, _chestBase;
        private float _animT;
        private int _frame;
        private float _chestT = -1f;
        private const float ChestLife = 0.9f;
        private Color _zoneTint = Color.white, _weatherTint = Color.white;
        private float _roadWidth = 2.56f;

        private GameState State => ServiceLocator.Get<GameState>();

        private void Awake()
        {
#if UNITY_EDITOR
            LoadSpritesEditor();
#endif
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
            _roadWidth = (zoneRoad != null && zoneRoad.Length > 0 && zoneRoad[0] != null)
                ? zoneRoad[0].bounds.size.x : 2.56f;

            _bg = Make("BG", First(zoneBg), -20, new Vector3(0f, 1.1f, 0f), Vector3.one);
            _roadA = Make("RoadA", First(zoneRoad), -10, new Vector3(0f, -0.35f, 0f), Vector3.one);
            _roadB = Make("RoadB", First(zoneRoad), -10, new Vector3(_roadWidth, -0.35f, 0f), Vector3.one);

            _chest = Make("Chest", chestSprite, 5, new Vector3(1.1f, 0.3f, 0f), Vector3.one);
            if (_chest != null) { _chest.enabled = false; _chestBase = _chest.transform.localPosition; }

            _vehicle = Make("Vehicle", VehicleSprite(0, 0), 10, new Vector3(0f, 0.35f, 0f), Vector3.one);
            if (_vehicle != null) _vehBase = _vehicle.transform.localPosition;

            _survivor = Make("Survivor", survivorIdle, 11, new Vector3(-1.35f, 0.3f, 0f), Vector3.one);
            if (_survivor != null) _survBase = _survivor.transform.localPosition;

            _wIcon = Make("Weather", First(weatherIcon), 30, new Vector3(1.7f, 1.95f, 0f), Vector3.one * 0.45f);
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
            ScrollRoad(_roadA, step);
            ScrollRoad(_roadB, step);

            // 帧动画（轮子 / 走路）
            if (moving)
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
                var p = _survBase;
                if (moving)
                {
                    p.y += Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.04f;
                    _survivor.sprite = _frame == 0 ? survivorWalk0 : survivorWalk1;
                }
                else _survivor.sprite = survivorIdle;
                _survivor.transform.localPosition = p;
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
            if (road.transform.localPosition.x <= -_roadWidth)
                road.transform.localPosition += Vector3.right * (_roadWidth * 2f);
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
                _wIcon.sprite = weatherIcon[(int)w];
        }

        private void ApplyZone(int idx)
        {
            idx = Mathf.Clamp(idx, 0, 2);
            _zoneTint = ZoneTint(idx);
            if (_bg != null && zoneBg != null && idx < zoneBg.Length) _bg.sprite = zoneBg[idx];
            if (_roadA != null && zoneRoad != null && idx < zoneRoad.Length) _roadA.sprite = zoneRoad[idx];
            if (_roadB != null && zoneRoad != null && idx < zoneRoad.Length) _roadB.sprite = zoneRoad[idx];
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

#if UNITY_EDITOR
        private void LoadSpritesEditor()
        {
            pickup0 = LoadSprite("Assets/Art/Sprites/Vehicles/pickup_0.png");
            pickup1 = LoadSprite("Assets/Art/Sprites/Vehicles/pickup_1.png");
            van0 = LoadSprite("Assets/Art/Sprites/Vehicles/van_0.png");
            van1 = LoadSprite("Assets/Art/Sprites/Vehicles/van_1.png");
            moto0 = LoadSprite("Assets/Art/Sprites/Vehicles/moto_0.png");
            moto1 = LoadSprite("Assets/Art/Sprites/Vehicles/moto_1.png");

            survivorIdle = LoadSprite("Assets/Art/Sprites/Survivor/survivor_idle.png");
            survivorWalk0 = LoadSprite("Assets/Art/Sprites/Survivor/survivor_walk0.png");
            survivorWalk1 = LoadSprite("Assets/Art/Sprites/Survivor/survivor_walk1.png");

            chestSprite = LoadSprite("Assets/Art/Sprites/Chests/chest_random.png");

            zoneBg = new Sprite[3];
            zoneRoad = new Sprite[3];
            zoneBg[0] = LoadSprite("Assets/Art/Sprites/Zones/bg_suburb.png");
            zoneBg[1] = LoadSprite("Assets/Art/Sprites/Zones/bg_desert.png");
            zoneBg[2] = LoadSprite("Assets/Art/Sprites/Zones/bg_tundra.png");
            zoneRoad[0] = LoadSprite("Assets/Art/Sprites/Zones/road_suburb.png");
            zoneRoad[1] = LoadSprite("Assets/Art/Sprites/Zones/road_desert.png");
            zoneRoad[2] = LoadSprite("Assets/Art/Sprites/Zones/road_tundra.png");

            weatherIcon = new Sprite[4];
            weatherIcon[0] = LoadSprite("Assets/Art/Sprites/Weather/weather_sunny.png");
            weatherIcon[1] = LoadSprite("Assets/Art/Sprites/Weather/weather_rain.png");
            weatherIcon[2] = LoadSprite("Assets/Art/Sprites/Weather/weather_sandstorm.png");
            weatherIcon[3] = LoadSprite("Assets/Art/Sprites/Weather/weather_blizzard.png");
        }

        private static Sprite LoadSprite(string path)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null)
            {
                if (s.texture != null) s.texture.filterMode = FilterMode.Point;
                return s;
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                tex.filterMode = FilterMode.Point;
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            return null;
        }
#endif
    }
}
