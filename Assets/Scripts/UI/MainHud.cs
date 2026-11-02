using UnityEngine;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 科技感 HUD（IMGUI 版）。左下暗玻璃面板：里程/区域/天气/小人三需求条/物资/蓝图/成就 + 快捷键提示。
    /// 随分辨率缩放（UIHelper.Scale）；只读 GameState，不反写。
    /// </summary>
    public class MainHud : MonoBehaviour
    {
        private GUIStyle _title, _label, _small, _hint, _btn;
        private bool _inited;
        private float _k;

        private void EnsureStyles()
        {
            float k = UIHelper.Scale;
            if (_inited && Mathf.Abs(k - _k) < 0.001f) return;
            _k = k;
            _title = UIHelper.NewLabelStyle((int)(15 * k), UIHelper.C_Cyan, TextAnchor.UpperLeft, true);
            _label = UIHelper.NewLabelStyle((int)(13 * k), UIHelper.C_Text);
            _small = UIHelper.NewLabelStyle((int)(12 * k), UIHelper.C_Text);
            _hint  = UIHelper.NewLabelStyle((int)(11 * k), UIHelper.C_Dim, TextAnchor.UpperLeft);
            _btn   = UIHelper.NewButtonStyle((int)(12 * k), UIHelper.C_Cyan);
            _inited = true;
        }

        private void OnGUI()
        {
            EnsureStyles();
            var s = ServiceLocator.Get<GameState>();
            if (s == null) return;

            float k = UIHelper.Scale;
            float w = 262f * k, pad = 12f * k;
            float lineH = 20f * k, barH = 9f * k;
            float innerW = w - pad * 2f;
            float contentH = lineH * 4f + 6f * k + barH * 3f + 8f * k * 3f + 16f * k + 28f * k;
            float h = contentH + pad * 2f;
            float x = 12f * k;
            float y = Screen.height - h - 12f * k;

            UIHelper.DrawRect(new Rect(x, y, w, h), UIHelper.C_Panel);
            UIHelper.DrawBorder(x, y, w, h, UIHelper.C_Cyan);

            float cx = x + pad;
            float cy = y + pad;
            GUI.Label(new Rect(cx, cy, innerW, lineH), "末日公路 · 状态面板", _title);
            cy += lineH;
            GUI.Label(new Rect(cx, cy, innerW, lineH),
                $"里程 {s.totalDistanceKm:F1} km　区域 {ZoneName(s.currentZoneIndex)}", _label);
            cy += lineH + 6f * k;

            cy = DrawBar(cx, cy, innerW, barH, "饥饿", s.hunger, k);
            cy = DrawBar(cx, cy + 4f * k, innerW, barH, "口渴", s.thirst, k);
            cy = DrawBar(cx, cy + 4f * k, innerW, barH, "生命", s.health, k);
            cy += 8f * k;

            GUI.Label(new Rect(cx, cy, innerW, lineH),
                $"食 {s.food}　水 {s.water}　药 {s.medicine}", _small);
            cy += lineH;
            GUI.Label(new Rect(cx, cy, innerW, lineH),
                $"油 {s.fuel}　件 {s.parts}　蓝图 {s.blueprintFragments}　成就 {s.unlockedAchievementIds.Count}/{AchievementTotal()}", _small);
            cy += lineH;

            // 快捷按钮（需在可交互模式：先按 Ctrl+Shift+D 把窗口提到前台）
            float bw = (innerW - 8f * k) / 3f;
            float bh = 24f * k;
            if (GUI.Button(new Rect(cx, cy, bw, bh), "背包(I)", _btn)) InventoryPanel.Toggle();
            if (GUI.Button(new Rect(cx + bw + 4f * k, cy, bw, bh), "成就(A)", _btn)) AchievementGallery.Toggle();
            if (GUI.Button(new Rect(cx + (bw + 4f * k) * 2f, cy, bw, bh), "帮助(H)", _btn)) HelpPanel.Toggle();
            cy += bh + 4f * k;
            GUI.Label(new Rect(cx, cy, innerW, 14f * k), "Ctrl+Shift+D 切穿透 · T 切置顶", _hint);
        }

        private float DrawBar(float x, float y, float w, float h, string name, float val, float k)
        {
            float labelW = 34f * k, numW = 32f * k;
            GUI.Label(new Rect(x, y - 1f * k, labelW, h + 4f * k), name, _small);
            float bx = x + labelW;
            float bw = w - labelW - numW;
            UIHelper.DrawRect(new Rect(bx, y, bw, h), UIHelper.C_Dim);
            Color fill = val > 50f ? UIHelper.C_Green : (val > 20f ? UIHelper.C_Amber : UIHelper.C_Red);
            float f = Mathf.Clamp01(val / 100f);
            if (f > 0f) UIHelper.DrawRect(new Rect(bx, y, bw * f, h), fill);
            GUI.Label(new Rect(bx + bw + 2f * k, y - 1f * k, numW, h + 4f * k), $"{val:F0}", _small);
            return y + h;
        }

        private static string ZoneName(int idx)
        {
            var cfg = GameConfig.Instance;
            if (cfg != null && idx >= 0 && idx < cfg.zones.Count) return cfg.zones[idx].name;
            return "未知";
        }

        private static int AchievementTotal()
        {
            var cfg = GameConfig.Instance;
            return cfg != null ? cfg.achievements.Count : 0;
        }
    }
}
