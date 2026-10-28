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

        private void OnEnable()
        {
            GameEvents.OnDistanceAdvanced += OnDistance;
            GameEvents.OnSurvivorStateChanged += OnSurvivor;
            GameEvents.OnWeatherChanged += OnWeather;
            GameEvents.OnChestOpened += OnChest;
        }

        private void OnDisable()
        {
            GameEvents.OnDistanceAdvanced -= OnDistance;
            GameEvents.OnSurvivorStateChanged -= OnSurvivor;
            GameEvents.OnWeatherChanged -= OnWeather;
            GameEvents.OnChestOpened -= OnChest;
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
                $"PLAY    {gs.playTimeSeconds:F0}s\n" +
                $"FPS     {_fps:F1}\n" +
                $"[~] toggle  [U] upgrade track0  (DebugHud)";

            GUI.Box(new Rect(8, 8, 300, 320), "DEBUG HUD");
            GUI.Label(new Rect(18, 30, 280, 285), text);
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
    }
}
