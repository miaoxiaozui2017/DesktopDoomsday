using UnityEngine;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 开发期调试覆盖层（IMGUI，零预制体，Play 即可见）。
    /// 订阅 GameEvents 实时显示 里程/速度/小人状态/天气/FPS；按 ~ 键开关。
    /// 直接支撑「可视化 + 可调式」：tick 推进、数值循环、状态切换肉眼可见。
    /// 发布前可隐藏此组件或整文件删除。
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

        private void OnEnable()
        {
            GameEvents.OnDistanceAdvanced += OnDistance;
            GameEvents.OnSurvivorStateChanged += OnSurvivor;
            GameEvents.OnWeatherChanged += OnWeather;
        }

        private void OnDisable()
        {
            GameEvents.OnDistanceAdvanced -= OnDistance;
            GameEvents.OnSurvivorStateChanged -= OnSurvivor;
            GameEvents.OnWeatherChanged -= OnWeather;
        }

        private void OnDistance(float d) => _lastDist = d;
        private void OnSurvivor(SurvivorState s) => _survivor = s;
        private void OnWeather(WeatherId w) => _weather = w;

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
        }

        private void OnGUI()
        {
            if (!_show) return;
            var gs = ServiceLocator.Get<GameState>();
            string text = gs == null ? "(no GameState)"
                : $"DIST    {gs.totalDistanceKm:F2} km\n" +
                  $"SPEED   {gs.currentSpeedKmPerTick:F2}\n" +
                  $"ZONE    {gs.currentZoneIndex}\n" +
                  $"SURV    {_survivor}  H{gs.hunger:F0} T{gs.thirst:F0} HP{gs.health:F0}\n" +
                  $"WEATHER {_weather}\n" +
                  $"PLAY    {gs.playTimeSeconds:F0}s\n" +
                  $"FPS     {_fps:F1}\n" +
                  $"[~] toggle";

            GUI.Box(new Rect(8, 8, 250, 180), "DEBUG");
            GUI.Label(new Rect(18, 30, 230, 150), text);
        }
    }
}
