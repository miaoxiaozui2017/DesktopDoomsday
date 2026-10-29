using UnityEngine;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 幸存者独立属性页（按 K 单独调取）。
    /// 展示小人三维数据（状态 + 饥饿/口渴/健康），并预留后续属性扩展位。
    /// 仅读取 GameState，不反写。精灵在 Editor 下自动载入肖像。
    /// </summary>
    public class SurvivorInfoPanel : MonoBehaviour
    {
        public bool show;
        private Texture2D _white;
        private Sprite _portrait;
        private GUIStyle _title, _text, _small;
        private bool _stylesReady;

        private void Awake()
        {
            _white = Texture2D.whiteTexture;
#if UNITY_EDITOR
            _portrait = LoadPortrait();
#endif
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.K)) show = !show;
        }

        private void OnGUI()
        {
            if (!show) return;
            var s = ServiceLocator.Get<GameState>();
            if (s == null) return;
            EnsureStyles();

            float w = 372f, h = 268f;
            var p = new Rect(Screen.width - w - 16f, 16f, w, h);
            Fill(p, new Color(0.043f, 0.055f, 0.078f, 0.92f));
            Border(p);

            float x = p.x + 12f, y = p.y + 10f, cw = w - 24f;
            GUI.Label(new Rect(x, y, cw, 20f), "幸存者 · SURVIVOR", _title); y += 26f;

            // 肖像（右侧）
            if (_portrait != null)
                GUI.DrawTexture(new Rect(p.x + w - 120f, y, 96f, 160f), _portrait.texture, ScaleMode.ScaleToFit);

            int dataW = (int)(w - 150f);
            GUI.Label(new Rect(x, y, dataW, 18f), $"状态  {SurvName(s.survivorState)}", _text); y += 24f;
            DrawBar(x, y, dataW, s.hunger / 100f, "饥饿"); y += 20f;
            DrawBar(x, y, dataW, s.thirst / 100f, "口渴"); y += 20f;
            DrawBar(x, y, dataW, s.health / 100f, "健康"); y += 24f;

            GUI.Label(new Rect(x, y, dataW, 16f), "后续属性：体力 / 经验 / 技能 / 负重 …", _small); y += 18f;
            GUI.Label(new Rect(x, y, dataW, 16f), $"里程 {s.totalDistanceKm:F1} km   区域 {ZoneName(s.currentZoneIndex)}", _small); y += 18f;
            GUI.Label(new Rect(x, y, dataW, 16f), "[K] 关闭面板  ·  [M] HUD  ·  [~] Debug", _small);
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

        private void Border(Rect p)
        {
            var b = new Color(0.13f, 0.83f, 0.93f, 0.85f);
            Fill(new Rect(p.x, p.y, p.width, 1f), b);
            Fill(new Rect(p.x, p.yMax - 1f, p.width, 1f), b);
            Fill(new Rect(p.x, p.y, 1f, p.height), b);
            Fill(new Rect(p.xMax - 1f, p.y, 1f, p.height), b);
            Fill(new Rect(p.x, p.y, 46f, 2f), new Color(1f, 0.69f, 0f, 0.95f));
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            var font = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Courier New", "Menlo" }, 13);
            _title = new GUIStyle { font = font, fontSize = 14, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.13f, 0.83f, 0.93f) } };
            _text = new GUIStyle { font = font, fontSize = 12, normal = { textColor = new Color(0.9f, 0.95f, 1f) } };
            _small = new GUIStyle { font = font, fontSize = 11, normal = { textColor = new Color(0.7f, 0.78f, 0.88f) } };
            _stylesReady = true;
        }

        private void Fill(Rect r, Color c)
        {
            var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, _white); GUI.color = old;
        }

        private static string SurvName(SurvivorState s) => s == SurvivorState.Weakened ? "虚弱" : "正常";
        private static string ZoneName(int idx) { switch (idx) { case 1: return "荒漠"; case 2: return "冻原"; default: return "城郊"; } }

#if UNITY_EDITOR
        private static Sprite LoadPortrait()
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Survivor/survivor_idle.png");
            if (s != null && s.texture != null) s.texture.filterMode = FilterMode.Point;
            return s;
        }
#endif
    }
}
