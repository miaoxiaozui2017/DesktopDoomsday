using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug; // 消除 System.Diagnostics.Debug 与 UnityEngine.Debug 的歧义（本文件用 Process 采样 CPU/内存）
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;
using DesktopDoomsday.Systems;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 开发期调试覆盖层（IMGUI，零预制体，Play 即可见）。
    /// 订阅 GameEvents 实时显示 里程/速度/区域/小人需求/天气/物资/宝箱计数/FPS；按 ~ 键开关。
    /// 底部「验收观测区」（红色高亮）实时显示三项 SPEC 验收指标：
    ///   ① CPU&lt;3% / 内存&lt;300MB（每 0.5s 采样进程 TotalProcessorTime 与 WorkingSet64）
    ///   ② 后台 tick：按 R 快照 DIST+PLAY+时间，恢复后看增量是否等于 速度×经过秒数（误差&lt;1%）
    ///   ③ 确定性：显示当前 rngSeed；每次开箱记录精确掉落；同存档读档后种子与掉落应一致
    /// 发布前可隐藏此组件或整文件删除。正式 UI 在 Batch 6 按 SPEC 第 9 节科技感规范实现。
    /// </summary>
    public class DebugHud : MonoBehaviour
    {
        private bool _show = false; // 默认隐藏（发布态减少 OnGUI 开销）；按 ~ 开关
        private float _fps;
        private float _accum;
        private int _frames;

        // ---- 验收 ① 性能 ----
        private Process _proc;
        private double _lastCpuTime;
        private float _cpuPercent;
        private float _procMB;
        private float _gcMB;

        // ---- 验收 ② 后台 tick 快照 ----
        private bool _hasSnapshot;
        private float _snapDist;
        private float _snapPlay;
        private float _snapRealtime;

        // ---- 验收 ③ 确定性 ----
        private List<string> _chestDetailLog = new List<string>();

        private float _lastDist;
        private SurvivorState _survivor = SurvivorState.Normal;
        private WeatherId _weather = WeatherId.Sunny;
        private int _chestCount;
        private List<string> _dungeonLog = new List<string>();

        private void OnEnable()
        {
            _proc = Process.GetCurrentProcess();
            _lastCpuTime = _proc.TotalProcessorTime.TotalSeconds;

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
        private void OnChest(ChestOpenResult r)
        {
            _chestCount++;
            // 验收 ③：记录每次开箱的精确掉落（资源名+数量+遇怪标记），供对比读档前后序列是否一致
            var parts = new List<string>();
            if (r.rewards != null)
                foreach (var rw in r.rewards) parts.Add($"{rw.resource}+{rw.delta}");
            if (r.blueprintFragments > 0) parts.Add($"blue+{r.blueprintFragments}");
            if (r.monsterEncounter) parts.Add("MONSTER!");
            string detail = $"#{_chestCount} [{string.Join(", ", parts.ToArray())}]";
            _chestDetailLog.Insert(0, detail);
            if (_chestDetailLog.Count > 5) _chestDetailLog.RemoveAt(_chestDetailLog.Count - 1);
            if (r.monsterEncounter) PushLog("开箱遇怪! 三需求下降");
        }

        private void Update()
        {
            _accum += Time.unscaledDeltaTime;
            _frames++;
            if (_accum >= 0.5f)
            {
                _fps = _frames / _accum;

                // 验收 ①：采样 CPU% 与内存（每 0.5s）
                double curCpu = _proc.TotalProcessorTime.TotalSeconds;
                double cpuDelta = curCpu - _lastCpuTime;
                _lastCpuTime = curCpu;
                // CPU% = (cpuTime增量 / 真实时间增量) / 逻辑核心数 × 100
                int cores = System.Environment.ProcessorCount;
                if (cores < 1) cores = 1;
                _cpuPercent = (float)((cpuDelta / _accum) / cores * 100.0);
                _procMB = _proc.WorkingSet64 / 1048576f;
                _gcMB = System.GC.GetTotalMemory(false) / 1048576f;

                _accum = 0f;
                _frames = 0;
            }
            if (Input.GetKeyDown(KeyCode.BackQuote))
                _show = !_show;

            // 验收 ②：按 R 快照当前里程/时间，用于最小化 10 分钟前后对比
            if (Input.GetKeyDown(KeyCode.R))
            {
                var gs = ServiceLocator.Get<GameState>();
                if (gs != null)
                {
                    _snapDist = gs.totalDistanceKm;
                    _snapPlay = gs.playTimeSeconds;
                    _snapRealtime = Time.realtimeSinceStartup;
                    _hasSnapshot = true;
                    PushLog($"快照: DIST={_snapDist:F2} PLAY={_snapPlay:F0}");
                }
            }

            // 调试：强制触发一个副本（跳过概率/冷却，便于验收 Batch4）
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
                $"[~]hide [D]dungeon [C]complete [R]snapshot";

            float boxH = 390f;
            GUI.Box(new Rect(8, 8, 300, boxH), "DEBUG HUD");
            GUI.Label(new Rect(18, 30, 280, 325), text);

            // ========== 验收观测区（红色高亮，独立画在屏幕右上角，不与左上常规区/左下 MainHud 重叠） ==========
            float vw = 290f;
            float vx = Screen.width - vw - 8f;
            float vy = 8f;

            // 构建验收文本
            bool cpuOk = _cpuPercent < 3f;
            bool memOk = _procMB < 300f;

            // ① 性能
            string perfText =
                $"--- 验收① 性能 ---\n" +
                $"CPU    {_cpuPercent:F1}%  {(cpuOk ? "<3% OK" : ">=3% FAIL")}\n" +
                $"MEM    {_procMB:F0}MB  {(memOk ? "<300MB OK" : ">=300MB FAIL")}\n" +
                $"  (GC {_gcMB:F0}MB)";

            // ② 后台 tick 快照
            string bgText;
            if (_hasSnapshot)
            {
                float dDist = gs.totalDistanceKm - _snapDist;
                float dPlay = gs.playTimeSeconds - _snapPlay;
                float dReal = Time.realtimeSinceStartup - _snapRealtime;
                float expectedDist = gs.currentSpeedKmPerTick * dReal;
                float errPct = expectedDist > 0.001f ? Mathf.Abs(dDist - expectedDist) / expectedDist * 100f : 0f;
                bool bgOk = errPct < 1f;
                bgText =
                    $"--- 验收② 后台tick ---\n" +
                    $"SNAP   DIST={_snapDist:F2} PLAY={_snapPlay:F0}\n" +
                    $"NOW    ΔDIST={dDist:F2}km  ΔPLAY={dPlay:F0}s  ΔREAL={dReal:F0}s\n" +
                    $"EXPECT {expectedDist:F2}km (=speed×ΔREAL)\n" +
                    $"ERR    {errPct:F2}%  {(bgOk ? "<1% OK" : ">=1% FAIL")}";
            }
            else
            {
                bgText =
                    $"--- 验收② 后台tick ---\n" +
                    $"按 R 快照 → 最小化/等待 → 恢复看增量";
            }

            // ③ 确定性
            string detText =
                $"--- 验收③ 确定性 ---\n" +
                $"SEED   {gs.rngSeed}\n" +
                $"CHEST  LOG (last 5):";
            foreach (var cl in _chestDetailLog)
                detText += $"\n  {cl}";

            string verifyText = perfText + "\n" + bgText + "\n" + detText;

            // 红色高亮绘制（右上角独立面板，带边框便于辨识）
            UIHelper.DrawRect(new Rect(vx, vy, vw, 230), new Color(0.08f, 0.04f, 0.04f, 0.92f));
            UIHelper.DrawBorder(vx, vy, vw, 230, UIHelper.C_Red);
            GUI.color = new Color(1f, 0.3f, 0.3f, 1f);
            GUI.Label(new Rect(vx + 10f, vy + 8f, vw - 20f, 215), verifyText);
            GUI.color = Color.white;
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

        /// <summary>显示两条改装线「下一级」所需零件/蓝图。</summary>
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