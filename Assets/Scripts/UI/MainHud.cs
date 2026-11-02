using UnityEngine;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 科技感 HUD（IMGUI 版，替代 uGUI，规避 UnityEngine.UI 模块未接入）。
    /// 左下暗玻璃面板：里程/区域/天气/小人三需求条/物资/蓝图/成就。只读 GameState，每帧 OnGUI 直接读，不轮询反写。
    /// </summary>
    public class MainHud : MonoBehaviour
    {
        private GUIStyle _title, _label, _small;
        private bool _inited;

        private void EnsureStyles()
        {
            if (_inited) return;
            _title = UIHelper.NewLabelStyle(15, UIHelper.C_Cyan, TextAnchor.UpperLeft, true);
            _label = UIHelper.NewLabelStyle(13, UIHelper.C_Text);
            _small = UIHelper.NewLabelStyle(12, UIHelper.C_Text);
            _inited = true;
        }

        private void OnGUI()
        {
            EnsureStyles();
            var s = ServiceLocator.Get<GameState>();
            if (s == null) return;

            float w = 258f, pad = 12f;
            float lineH = 20f, barH = 9f;
            float innerW = w - pad * 2f;
            float contentH = lineH + lineH + 6f + barH * 3 + 8f * 3 + lineH * 2;
            float h = contentH + pad * 2f;
            float x = 12f;
            float y = Screen.height - h - 12f;

            UIHelper.DrawRect(new Rect(x, y, w, h), UIHelper.C_Panel);
            UIHelper.DrawBorder(x, y, w, h, UIHelper.C_Cyan);

            float cx = x + pad;
            float cy = y + pad;
            GUI.Label(new Rect(cx, cy, innerW, lineH), "末日公路 · 状态面板", _title);
            cy += lineH;

            GUI.Label(new Rect(cx, cy, innerW, lineH),
                $"里程 {s.totalDistanceKm:F1} km　区域 {ZoneName(s.currentZoneIndex)}", _label);
            cy += lineH + 6f;

            cy = DrawBar(cx, cy, innerW, barH, "饥饿", s.hunger);
            cy = DrawBar(cx, cy + 4f, innerW, barH, "口渴", s.thirst);
            cy = DrawBar(cx, cy + 4f, innerW, barH, "生命", s.health);
            cy += 8f;

            GUI.Label(new Rect(cx, cy, innerW, lineH),
                $"食 {s.food}　水 {s.water}　药 {s.medicine}", _small);
            cy += lineH;
            GUI.Label(new Rect(cx, cy, innerW, lineH),
                $"油 {s.fuel}　件 {s.parts}　蓝图 {s.blueprintFragments}　成就 {s.unlockedAchievementIds.Count}/{AchievementTotal()}", _small);
        }

        private float DrawBar(float x, float y, float w, float h, string name, float val)
        {
            float labelW = 34f, numW = 32f;
            GUI.Label(new Rect(x, y - 1f, labelW, h + 4f), name, _small);
            float bx = x + labelW;
            float bw = w - labelW - numW;
            UIHelper.DrawRect(new Rect(bx, y, bw, h), UIHelper.C_Dim);
            Color fill = val > 50f ? UIHelper.C_Green : (val > 20f ? UIHelper.C_Amber : UIHelper.C_Red);
            float f = Mathf.Clamp01(val / 100f);
            if (f > 0f) UIHelper.DrawRect(new Rect(bx, y, bw * f, h), fill);
            GUI.Label(new Rect(bx + bw + 2f, y - 1f, numW, h + 4f), $"{val:F0}", _small);
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
