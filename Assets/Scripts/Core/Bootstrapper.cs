using System;
using UnityEngine;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;
using DesktopDoomsday.Systems;
using DesktopDoomsday.UI;
using DesktopDoomsday.Window;

namespace DesktopDoomsday.Core
{
    /// <summary>
    /// 入口。挂到 Main.unity 唯一 GameObject。
    /// 职责：加载存档或新建 GameState、注册服务、连接 GameLoop（Batch 2 起驱动各 System）、退出时保存。
    /// 开局选车面板（VehicleSystem）在 Batch 3 接入；天气/区域在 Batch 3 接入。
    /// </summary>
    public class Bootstrapper : MonoBehaviour
    {
        private GameState _state;
        private SaveSystem _save;
        private RoadSystem _road;
        private ChestSystem _chest;
        private SurvivorSystem _survivor;
        private WeatherSystem _weather;
        private VehicleSystem _vehicle;
        private DungeonSystem _dungeon;

        private void Awake()
        {
            _save = new SaveSystem();
            ServiceLocator.Register(_save);

            // 先创建各系统（InitNewGame 需要 _vehicle / _weather）
            _road = new RoadSystem();
            _chest = new ChestSystem();
            _survivor = new SurvivorSystem();
            _weather = new WeatherSystem();
            _vehicle = new VehicleSystem();
            _dungeon = new DungeonSystem();
            ServiceLocator.Register(_road);
            ServiceLocator.Register(_chest);
            ServiceLocator.Register(_survivor);
            ServiceLocator.Register(_weather);
            ServiceLocator.Register(_vehicle);
            ServiceLocator.Register(_dungeon);

            var data = _save.Load();
            bool isNew = (data == null || data.state == null);
            if (!isNew)
            {
                _state = data.state;
                float offline = _save.ComputeOfflineSeconds(_state.lastSaveUnixMs);
                if (offline > 0f)
                    Debug.Log($"[Bootstrapper] Offline seconds pending: {offline}");
            }
            else
            {
                _state = new GameState();
                InitNewGame(_state);
            }
            ServiceLocator.Register(_state);

            var loop = FindObjectOfType<GameLoop>();
            if (loop != null)
                GameLoop.OnTick += TickAll;

            ConfigureRenderAndFrameRate();
            EnsureVisualLayer();
        }

        /// <summary>新开局初始化：确定性种子 + 初始物资 + 首个里程宝箱阈值。</summary>
        private void InitNewGame(GameState s)
        {
            // 首次种子用时间派生；之后全程由 rngSeed 驱动，读档可复现（SPEC 验收 10）。
            s.rngSeed = (int)((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() & 0x7FFFFFFF) | 1);

            var cfg = GameConfig.Instance;
            if (cfg != null && cfg.resources != null)
            {
                s.food = cfg.resources.initialFood;
                s.water = cfg.resources.initialWater;
                s.medicine = cfg.resources.initialMedicine;
                s.fuel = cfg.resources.initialFuel;
                s.parts = cfg.resources.initialParts;
            }
            else
            {
                // 兜底初始物资，保证首局可长时间维持 Normal（后续可在 GameConfig 调整）。
                s.food = 50; s.water = 50; s.medicine = 30; s.fuel = 100; s.parts = 20;
            }
            s.nextChestThresholdKm = 2f;

            // Batch 3：默认选车（三选一面板在 Batch 6 接入）；初始化首个天气与持续时间
            _vehicle.SelectVehicle(s, VehicleId.Pickup);
            _weather.Initialize(s);
        }

        /// <summary>SPEC 2.2 渲染/帧率前置设置（代码 API，Unity 设置面板无对应项）。</summary>
        private void ConfigureRenderAndFrameRate()
        {
            Application.targetFrameRate = 30;
            QualitySettings.vSyncCount = 0;

            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            }
        }

        /// <summary>
        /// 可视化层自动装配：可玩视图、科技 HUD、快进工具，并尝试挂 Pixel Perfect 相机。
        /// 让用户 Play 即可看到画面，不必在 Inspector 手动挂组件。
        /// </summary>
        private void EnsureVisualLayer()
        {
            if (FindObjectOfType<GameView>() == null) gameObject.AddComponent<GameView>();
            if (FindObjectOfType<TechHud>() == null) gameObject.AddComponent<TechHud>();
            if (FindObjectOfType<FastForward>() == null) gameObject.AddComponent<FastForward>();
            if (FindObjectOfType<SurvivorInfoPanel>() == null) gameObject.AddComponent<SurvivorInfoPanel>();
            if (FindObjectOfType<VehicleInfoPanel>() == null) gameObject.AddComponent<VehicleInfoPanel>();
            EnsureWindowLayer();
            EnsurePixelPerfect();
        }

        /// <summary>桌面窗口层（Batch 5）：透明/置顶/穿透 + 托盘。仅在 Windows 独立构建生效，编辑器为 no-op。</summary>
        private void EnsureWindowLayer()
        {
            if (FindObjectOfType<TransparentWindow>() == null) gameObject.AddComponent<TransparentWindow>();
            if (FindObjectOfType<ClickThroughManager>() == null) gameObject.AddComponent<ClickThroughManager>();
            if (FindObjectOfType<TrayManager>() == null) gameObject.AddComponent<TrayManager>();
        }

        /// <summary>反射式挂 Pixel Perfect Camera：包未导入时静默跳过，不产生硬编译依赖。</summary>
        private void EnsurePixelPerfect()
        {
            if (Camera.main == null) return;
            System.Type t = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                t = asm.GetType("UnityEngine.U2D.PixelPerfectCamera");
                if (t != null) break;
            }
            if (t == null) return;
            var pp = Camera.main.GetComponent(t);
            if (pp == null) pp = Camera.main.gameObject.AddComponent(t);

            // 桌面宠物比例：放大参考分辨率，让可视世界更大、车更小（默认 320x180 会把车撑满整屏）。
            // 用反射写序列化字段，兼容不同 Unity 小版本的属性名差异。
            SetPpcValue(t, pp, "m_RefResolutionX", 1920);
            SetPpcValue(t, pp, "m_RefResolutionY", 1080);
            SetPpcValue(t, pp, "m_AssetsPPU", 100f);
        }

        private static void SetPpcValue(System.Type t, UnityEngine.Object comp, string field, object val)
        {
            var f = t.GetField(field, System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (f != null) { f.SetValue(comp, val); return; }
            var p = t.GetProperty(field, System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (p != null && p.CanWrite) p.SetValue(comp, val, null);
        }

        /// <summary>核心循环（SPEC 第 1 节）：天气 → 副本 → 公路/宝箱（可被暂停）→ 小人需求。</summary>
        private void TickAll(float dt)
        {
            _state.playTimeSeconds += dt;
            _weather.Tick(_state, dt);   // 先更新天气（影响后续公路/宝箱/小人修正）
            _dungeon.Tick(_state, dt);   // 推进副本冷却/超时/触发

            // PauseRoadDuringDungeon（SPEC 5.6 设置项，默认 false）：打开副本面板（pending 期间）暂停公路与宝箱 tick
            bool pauseRoad = _state.pauseRoadDuringDungeon && _state.pendingDungeon.HasValue;
            if (!pauseRoad)
            {
                _road.Tick(_state, dt);
                _chest.Tick(_state, dt);
            }
            _survivor.Tick(_state, dt);
        }

        private void OnApplicationQuit()
        {
            if (_save != null && _state != null)
                _save.Save(_state);
        }
    }
}
