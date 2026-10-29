using UnityEngine;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 科技感 HUD 雏形（IMGUI，暗玻璃 + 霓虹描边 + 霓虹条形进度）。
    /// 对齐 SPEC 9.4：暗底面板、1px 青色描边、细长霓虹条；正式 uGUI 版在 Batch 6 落地。
    /// 按 M 开关。只读 GameState 渲染，不反写。
    /// </summary>
    public class TechHud : MonoBehaviour
    {
        public bool show = true;

        private Texture2D _white;
        private GUIStyle _title, _text, _small;
        private bool _stylesReady;

        private void Awake() { _white = Texture2D.whiteTexture; }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M)) show = !show;
        }

        private void OnGUI()
        {
            if (!show) return;
            var s = ServiceLocator.Get<GameState>();
            if (s == null) return;
            EnsureStyles();

            float w = 336f, h = 200f;
            var p = new Rect(16f, Screen.height - h - 16f, w, h);
            Fill(p, new Color(0.043f, 0.055f, 0.078f, 0.9f));

            var border = new Color(0.13f, 0.83f, 0.93f, 0.85f);
            Fill(new Rect(p.x, p.y, p.width, 1f), border);
            Fill(new Rect(p.x, p.yMax - 1f, p.width, 1f), border);
            Fill(new Rect(p.x, p.y, 1f, p.height), border);
            Fill(new Rect(p.xMax - 1f, p.y, 1f, p.height), border);
            // 左上角科技强调
            Fill(new Rect(p.x, p.y, 46f, 2f), new Color(1f, 0.69f, 0f, 0.95f));

            float x = p.x + 12f, y = p.y + 10f, cw = p.width - 24f;
            GUI.Label(new Rect(x, y, cw, 20f), "DESKTOP DOOMSDAY", _title); y += 24f;
            GUI.Label(new Rect(x, y, cw, 18f), $"里程 {s.totalDistanceKm:F2} km    区域 {ZoneName(s.currentZoneIndex)}", _text); y += 19f;
            GUI.Label(new Rect(x, y, cw, 18f), $"天气 {WeatherName(s.currentWeather)}   {s.weatherRemainingSeconds:F0}s    小人 {SurvName(s.survivorState)}", _text); y += 22f;

            DrawBar(x, y, cw, s.hunger / 100f, "饥饿"); y += 17f;
            DrawBar(x, y, cw, s.thirst / 100f, "口渴"); y += 17f;
            DrawBar(x, y, cw, s.health / 100f, "健康"); y += 21f;

            GUI.Label(new Rect(x, y, cw, 16f), $"食{s.food} 水{s.water} 药{s.medicine} 燃{s.fuel} 件{s.parts} 蓝{s.blueprintFragments}", _small); y += 17f;
            GUI.Label(new Rect(x, y, cw, 16f), "[M] HUD  [K] 小人  [V] 车辆  [~] Debug  [F] 加速  [G] +60s  [J] +300s", _small);
        }

        private void DrawBar(float x, float y, float w, float v, string label)
        {
            GUI.Label(new Rect(x, y - 3f, 44f, 16f), label, _small);
            var track = new Rect(x + 46f, y, w - 46f, 10f);
            Fill(track, new Color(0.08f, 0.11f, 0.15f, 0.95f));
            Color fill = v > 0.6f ? new Color(0.24f, 0.86f, 0.52f)
                       : v > 0.3f ? new Color(1f, 0.69f, 0f)
                       : new Color(1f, 0.3f, 0.3f);
            Fill(new Rect(track.x + 1f, track.y + 1f, (track.width - 2f) * Mathf.Clamp01(v), track.height - 2f), fill);
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            var font = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Courier New", "Menlo" }, 13);
            _title = new GUIStyle
            {
                font = font,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.13f, 0.83f, 0.93f) }
            };
            _text = new GUIStyle
            {
                font = font,
                fontSize = 12,
                normal = { textColor = new Color(0.9f, 0.95f, 1f) }
            };
            _small = new GUIStyle
            {
                font = font,
                fontSize = 11,
                normal = { textColor = new Color(0.7f, 0.78f, 0.88f) }
            };
            _stylesReady = true;
        }

        private void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, _white);
            GUI.color = old;
        }

        private static string ZoneName(int idx)
        {
            switch (idx) { case 1: return "荒漠"; case 2: return "冻原"; default: return "城郊"; }
        }

        private static string WeatherName(WeatherId w)
        {
            switch (w)
            {
                case WeatherId.Rainstorm: return "暴雨";
                case WeatherId.Sandstorm: return "沙暴";
                case WeatherId.ExtremeCold: return "极寒";
                default: return "晴天";
            }
        }

        private static string SurvName(SurvivorState s) => s == SurvivorState.Weakened ? "虚弱" : "正常";
    }
}
