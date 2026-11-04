using UnityEngine;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;
using DesktopDoomsday.Systems;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 幸存者独立属性页（按 K 单独调取，IMGUI 版，随分辨率缩放 + 中文字体）。
    /// 展示小人三维数据（状态 + 饥饿/口渴/生命）+ 肖像 + 「一键补满」按钮
    /// （按库存消耗 食/水/药 各 1 单位，把未满需求补到 100，详见 SurvivorSystem.FillAllNeeds）。
    /// 打开期间自动关闭点击穿透（SetInteractive）以便点击；关闭后恢复桌面宠物穿透态。
    /// </summary>
    public class SurvivorInfoPanel : MonoBehaviour
    {
        public static SurvivorInfoPanel Instance { get; private set; }
        public bool show;
        private Sprite _portrait;
        private GUIStyle _title, _text, _small, _hint, _btn;
        private bool _stylesReady;
        private float _k;

        private void Awake()
        {
            Instance = this;
            _portrait = LoadPortrait();
        }

        private void OnDestroy()
        {
            if (show) UIHelper.SetInteractive(false);
            if (Instance == this) Instance = null;
        }

        /// <summary>开关面板（供 MainHud 按钮调用，与 InventoryPanel.Toggle 同模式）。</summary>
        public static void Toggle()
        {
            if (Instance == null) return;
            Instance.show = !Instance.show;
            UIHelper.SetInteractive(Instance.show);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.K)) Toggle();
        }

        private void OnGUI()
        {
            if (!show) return;
            var s = ServiceLocator.Get<GameState>();
            if (s == null) return;
            EnsureStyles();

            float k = UIHelper.Scale;
            float w = 380f * k;
            float pad = 14f * k;
            float lineH = 24f * k;   // 13px 行
            float barRow = 22f * k;  // 12px 需求条行
            float btnH = 28f * k;

            // 内容高度：标题 + 状态行 + 三条 + 补满按钮 + 里程/说明两行 + 关闭按钮（右侧肖像 160k 并行，不占额外高度）
            float content = 26f * k + lineH + barRow * 3f + 6f * k + btnH + 8f * k + lineH * 2f + 8f * k + btnH;
            float h = content + pad * 2f;
            float x = Screen.width - w - 20f * k; // 右上角（天气图标在左上，互不遮挡）
            float y = 20f * k;

            UIHelper.DrawRect(new Rect(x, y, w, h), UIHelper.C_Panel);
            UIHelper.DrawBorder(x, y, w, h, UIHelper.C_Cyan);

            // 肖像（右侧）
            if (_portrait != null)
                GUI.DrawTexture(new Rect(x + w - pad - 96f * k, y + pad + 24f * k, 96f * k, 160f * k),
                    _portrait.texture, ScaleMode.ScaleToFit);

            float dataW = w - pad * 2f - 110f * k; // 数据列宽（给右侧肖像留位）
            float cx = x + pad;
            float cy = y + pad;
            GUI.Label(new Rect(cx, cy, dataW, 26f * k), "幸存者 · SURVIVOR", _title);
            cy += 26f * k;

            GUI.Label(new Rect(cx, cy, dataW, lineH),
                $"状态：{(s.survivorState == SurvivorState.Weakened ? "虚弱（降速）" : "正常")}", _text);
            cy += lineH;

            cy = DrawBar(cx, cy, dataW, "饥饿", s.hunger, k); cy += barRow - 10f * k;
            cy = DrawBar(cx, cy, dataW, "口渴", s.thirst, k); cy += barRow - 10f * k;
            cy = DrawBar(cx, cy, dataW, "生命", s.health, k); cy += barRow - 10f * k + 6f * k;
            // 一键补满（SurvivorSystem.FillAllNeeds：食/水/药 各消耗 1 单位补满未满项）
            bool full = s.hunger >= 100f && s.thirst >= 100f && s.health >= 100f;
            bool noSupply = s.food < 1f && s.water < 1f && s.medicine < 1f;
            string fillText = full ? "状态已满"
                : noSupply ? "物资不足（等待宝箱补给）"
                : $"一键补满（食{s.food:F0} 水{s.water:F0} 药{s.medicine:F0}）";
            GUI.enabled = !full && !noSupply;
            if (GUI.Button(new Rect(cx, cy, dataW, btnH), fillText, _btn))
            {
                var sys = ServiceLocator.Get<SurvivorSystem>();
                if (sys != null) sys.FillAllNeeds(s);
            }
            GUI.enabled = true;
            cy += btnH + 8f * k;

            GUI.Label(new Rect(cx, cy, dataW, lineH),
                $"里程 {s.totalDistanceKm:F1} km　区域 {ZoneName(s.currentZoneIndex)}", _small);
            cy += lineH;
            GUI.Label(new Rect(cx, cy, dataW, lineH),
                "需求低于 40 自动补货，空背包才虚弱", _small);
            cy += lineH + 8f * k;
            // 关闭按钮（替代原提示行，与背包页风格统一）
            if (GUI.Button(new Rect(cx, cy, dataW, btnH), "关闭 (K)", _btn))
            {
                show = false;
                UIHelper.SetInteractive(false);
            }
        }

        private float DrawBar(float x, float y, float w, string name, float val, float k)
        {
            float rowH = 22f * k;   // 12px 标签行高
            float h = 10f * k;
            float labelW = 46f * k, numW = 30f * k; // 标签区加宽，容纳两字需求名 + 默认内边距
            GUI.Label(new Rect(x, y, labelW, rowH), name, _small);
            float bx = x + labelW;
            float bw = w - labelW - numW;
            UIHelper.DrawRect(new Rect(bx, y + (rowH - h) / 2f, bw, h), UIHelper.C_Dim);
            Color fill = val > 50f ? UIHelper.C_Green : (val > 20f ? UIHelper.C_Amber : UIHelper.C_Red);
            float f = Mathf.Clamp01(val / 100f);
            if (f > 0f) UIHelper.DrawRect(new Rect(bx, y + (rowH - h) / 2f, bw * f, h), fill);
            GUI.Label(new Rect(bx + bw + 2f * k, y, numW, rowH), $"{val:F0}", _small);
            return y + h;
        }

        private void EnsureStyles()
        {
            float k = UIHelper.Scale;
            if (_stylesReady && Mathf.Abs(k - _k) < 0.001f) return;
            _k = k;
            _title = UIHelper.NewLabelStyle((int)(15 * k), UIHelper.C_Cyan, TextAnchor.UpperLeft, true);
            _text  = UIHelper.NewLabelStyle((int)(13 * k), UIHelper.C_Text);
            _small = UIHelper.NewLabelStyle((int)(12 * k), UIHelper.C_Text);
            _hint  = UIHelper.NewLabelStyle((int)(11 * k), UIHelper.C_Muted);
            _btn   = UIHelper.NewButtonStyle((int)(12 * k), UIHelper.C_Cyan);
            _stylesReady = true;
        }

        private static string ZoneName(int idx) { switch (idx) { case 1: return "荒漠"; case 2: return "冻原"; default: return "城郊"; } }

        private static Sprite LoadPortrait()
        {
            var s = Resources.Load<Sprite>("Art/Sprites/Survivor/survivor_idle");
            if (s != null && s.texture != null) s.texture.filterMode = FilterMode.Point;
            return s;
        }
    }
}
