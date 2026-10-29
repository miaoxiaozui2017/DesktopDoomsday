using System.Collections.Generic;
using UnityEngine;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;
using DesktopDoomsday.Systems;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 开发期调试覆盖层（IMGUI，零预制体，Play 即可见）。
    /// 订阅 GameEvents 实时显示 里程/速度/区域/小人需求/天气/物资/宝箱计数/FPS；按 ~ 键开关。
    /// 直接支撑「可视化 + 可调式」：tick 推进、数值循环、状态切换肉眼可见。
    /// 发布前可隐藏此组件或整文件删除。正式 UI 在 Batch 6 按 SPEC 第 9 节科技感规范实现。
    /// </summary>
    public class DebugHud : MonoBehaviour
    {
        private bool _show = true;
        private float _fps;
        private float _accum;
        private int _frames;

        private float _lastDist;
        private SurvivorState _survivor = SurvivorState.Normal;
        private WeatherId _weather = WeatherId.Sunny;
        private int _chestCount;
        private List<string> _dungeonLog = new List<string>();

        private void OnEnable()
        {
            GameEvents.OnDistanceAdvanced += OnDistance;
            GameEvents.OnSurvivorStateChanged += OnSurvivor;
            GameEvents.OnWeatherChanged += OnWeather;
            GameEvents.OnChestOpened += OnChest;
            GameEvents.OnDungeonAvailable += OnDungeonAvailable;
            GameEvents.OnDungeonCompleted += OnDungeonCompleted;
        }

        private void OnDisable()
        {
            GameEvents.OnDistanceAdvanced -= OnDistance;
            GameEvents.OnSurvivorStateChanged -= OnSurvivor;
            GameEvents.OnWeatherChanged -= OnWeather;
            GameEvents.OnChestOpened -= OnChest;
            GameEvents.OnDungeonAvailable -= OnDungeonAvailable;
            GameEvents.OnDungeonCompleted -= OnDungeonCompleted;
        }

        private void OnDungeonAvailable(DungeonId id) => PushLog($"副本出现: {id}");
        private void OnDungeonCompleted(DungeonResult r) => PushLog($"副本完成: {r.id} (+{r.blueprintFragments}蓝)");
        private void PushLog(string s)
        {
            _dungeonLog.Insert(0, s);
            if (_dungeonLog.Count > 3) _dungeonLog.RemoveAt(_dungeonLog.Count - 1);
        }

        private void OnDistance(float d) => _lastDist = d;
        private void OnSurvivor(SurvivorState s) => _survivor = s;
        private void OnWeather(WeatherId w) => _weather = w;
        private void OnChest(ChestOpenResult r) => _chestCount++;

        private void Update()
        {
            _accum += Time.unscaledDeltaTime;
            _frames++;
            if (_accum >= 0.5f)
            {
                _fps = _frames / _accum;
                _accum = 0f;
                _frames = 0;
            }
            if (Input.GetKeyDown(KeyCode.BackQuote))
                _show = !_show;

            // 调试：按 U 对当前车辆改装线 0 尝试升级（验证 VehicleSystem.TryUpgrade）
            if (Input.GetKeyDown(KeyCode.U))
            {
                var vs = ServiceLocator.Get<VehicleSystem>();
                var gs = ServiceLocator.Get<GameState>();
                if (vs != null && gs != null)
                {
                    bool ok = vs.TryUpgrade(gs, gs.currentVehicle, 0);
                    Debug.Log($"[DebugHud] Upgrade track0 -> {ok} (parts={gs.parts}, blue={gs.blueprintFragments}, lvl={gs.upgradeLevels[((int)gs.currentVehicle) * 2]})");
                }
            }

            // 调试：发放测试资源（蓝图碎片目前仅 Batch4 副本宝箱产出，此处便于提前验证改装）
            if (Input.GetKeyDown(KeyCode.B))
            {
                var gs = ServiceLocator.Get<GameState>();
                if (gs != null) { gs.blueprintFragments += 5; Debug.Log($"[DebugHud] +5 blueprint -> {gs.blueprintFragments}"); }
            }
            if (Input.GetKeyDown(KeyCode.P))
            {
                var gs = ServiceLocator.Get<GameState>();
                if (gs != null) { gs.parts += 20; Debug.Log($"[DebugHud] +20 parts -> {gs.parts}"); }
            }

            // 调试：强制触发一个副本（跳过概率/冷却，便于无面板时验收 Batch4）
            if (Input.GetKeyDown(KeyCode.D))
            {
                var ds = ServiceLocator.Get<DungeonSystem>();
                var gs = ServiceLocator.Get<GameState>();
                if (ds != null && gs != null)
                {
                    bool ok = ds.ForceTrigger(gs);
                    Debug.Log($"[DebugHud] Force dungeon -> {ok} (pending={gs.pendingDungeon})");
                }
            }

            // 调试：以分支 0 完成当前待处理副本（验收奖励结算）
            if (Input.GetKeyDown(KeyCode.C))
            {
                var ds = ServiceLocator.Get<DungeonSystem>();
                var gs = ServiceLocator.Get<GameState>();
                if (ds != null && gs != null && gs.pendingDungeon.HasValue)
                {
                    var res = ds.Complete(gs.pendingDungeon.Value, 0, gs);
                    Debug.Log($"[DebugHud] Complete {res.id} rewards={res.rewards.Count} bp={res.blueprintFragments}");
                }
            }
        }

        private void OnGUI()
        {
            if (!_show) return;
            var gs = ServiceLocator.Get<GameState>();
            if (gs == null)
            {
                GUI.Box(new Rect(8, 8, 220, 40), "DEBUG HUD");
                GUI.Label(new Rect(18, 30, 200, 20), "(no GameState)");
                return;
            }

            string text =
                $"DIST    {gs.totalDistanceKm:F2} km\n" +
                $"SPEED   {gs.currentSpeedKmPerTick:F3} km/s\n" +
                $"ZONE    {gs.currentZoneIndex}  ({ZoneName(gs.currentZoneIndex)})\n" +
                $"WEATHER {_weather}  ({gs.weatherRemainingSeconds:F0}s)\n" +
                $"VEHICLE {gs.currentVehicle}\n" +
                $"SURV    {_survivor}\n" +
                $"  HUNGER {Bar(gs.hunger)} {gs.hunger:F0}\n" +
                $"  THIRST {Bar(gs.thirst)} {gs.thirst:F0}\n" +
                $"  HEALTH {Bar(gs.health)} {gs.health:F0}\n" +
                $"FOOD {gs.food}  WATER {gs.water}  MED {gs.medicine}\n" +
                $"FUEL {gs.fuel}  PARTS {gs.parts}  BLUE {gs.blueprintFragments}\n" +
                $"CHESTS  {_chestCount}\n" +
                $"UPG     {UpgradeCostLine(gs)}\n" +
                $"DUNGEON pending={(gs.pendingDungeon.HasValue ? gs.pendingDungeon.Value.ToString() : "—")}\n" +
                $"  timeout={gs.pendingDungeonTimeout:F0}s  cooldown={gs.dungeonCooldownRemaining:F0}s\n" +
                $"  autoIgnore={gs.autoIgnoreDungeons}  pauseRoad={gs.pauseRoadDuringDungeon}\n" +
                $"  {( _dungeonLog.Count > 0 ? string.Join("  |  ", _dungeonLog.ToArray()) : "")}\n" +
                $"PLAY    {gs.playTimeSeconds:F0}s\n" +
                $"FPS     {_fps:F1}\n" +
                $"[~] toggle  [U] upgrade  [P] +parts  [B] +blue  [D] 触发副本  [C] 完成分支0  [K] 小人  [V] 车辆  (DebugHud)";

            GUI.Box(new Rect(8, 8, 300, 360), "DEBUG HUD");
            GUI.Label(new Rect(18, 30, 280, 325), text);
        }

        private static string Bar(float v)
        {
            int n = Mathf.Clamp(Mathf.RoundToInt(v / 100f * 10f), 0, 10);
            return "[" + new string('#', n) + new string('-', 10 - n) + "]";
        }

        private static string ZoneName(int idx)
        {
            switch (idx)
            {
                case 0: return "城郊";
                case 1: return "荒漠";
                case 2: return "冻原";
                default: return "循环";
            }
        }

        /// <summary>显示两条改装线「下一级」所需零件/蓝图，方便知道多少 P/B 可升级。</summary>
        private static string UpgradeCostLine(GameState gs)
        {
            var vs = ServiceLocator.Get<VehicleSystem>();
            if (vs == null) return "(无 VehicleSystem)";
            var sb = new System.Text.StringBuilder();
            for (int t = 0; t < 2; t++)
            {
                if (vs.GetUpgradeCost(gs, gs.currentVehicle, t, out int pc, out int fc, out bool mx))
                    sb.Append($"T{t}:{pc}件+{fc}蓝  ");
                else if (mx)
                    sb.Append($"T{t}:满级  ");
            }
            return sb.ToString().TrimEnd();
        }
    }
}
