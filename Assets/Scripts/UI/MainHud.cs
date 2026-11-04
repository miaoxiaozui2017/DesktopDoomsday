using UnityEngine;
using System.Linq;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;
using DesktopDoomsday.Systems;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 科技感 HUD（IMGUI 版）。左下暗玻璃面板：里程/区域/天气/小人三需求条/物资/蓝图/成就
    /// + 副本倒计时与预计挂机收益 + 快捷按钮（背包/成就/帮助/退出）。
    /// 行高按字体 ~1.8 倍取值（13px→24k、12px→22k、11px→20k），确保中文竖向不截字；
    /// 面板高度按内容实际累加；底边距 48px×scale 避开任务栏。
    /// 面板上方浮动层（自下而上）：副本横幅（待处理时常驻，琥珀边框，带「查看」入口）→ 遇怪警示条（4 秒，红色）。
    /// StackTopY 记录最顶层浮动元素的 Y，供 DungeonUI 分支面板继续向上堆叠（同锚点不遮挡）。
    /// 订阅 OnChestOpened：开箱遇怪时在面板上方弹 4 秒警示条。
    /// 只读 GameState，不反写（退出按钮仅调 Application.Quit，存档由 Bootstrapper 统一处理）。
    /// </summary>
    public class MainHud : MonoBehaviour
    {
        private GUIStyle _title, _label, _small, _hint, _warn, _btn, _btnExit, _toast;
        private bool _inited;
        private float _k;
        private float _monsterToastUntil;

        private void OnEnable() { GameEvents.OnChestOpened += OnChest; }
        private void OnDisable() { GameEvents.OnChestOpened -= OnChest; }
        private void OnChest(ChestOpenResult r)
        {
            if (r.monsterEncounter) _monsterToastUntil = Time.realtimeSinceStartup + 4f;
        }

        private void EnsureStyles()
        {
            float k = UIHelper.Scale;
            if (_inited && Mathf.Abs(k - _k) < 0.001f) return;
            _k = k;
            _title   = UIHelper.NewLabelStyle((int)(15 * k), UIHelper.C_Cyan, TextAnchor.UpperLeft, true);
            _label   = UIHelper.NewLabelStyle((int)(13 * k), UIHelper.C_Text);
            _small   = UIHelper.NewLabelStyle((int)(12 * k), UIHelper.C_Text);
            _hint    = UIHelper.NewLabelStyle((int)(11 * k), UIHelper.C_Muted, TextAnchor.UpperLeft);
            _warn    = UIHelper.NewLabelStyle((int)(12 * k), UIHelper.C_Amber);
            _toast   = UIHelper.NewLabelStyle((int)(12 * k), UIHelper.C_Red, TextAnchor.MiddleCenter);
            _btn     = UIHelper.NewButtonStyle((int)(12 * k), UIHelper.C_Cyan);
            _btnExit = UIHelper.NewButtonStyle((int)(12 * k), UIHelper.C_Red);
            _inited = true;
        }

        private void OnGUI()
        {
            EnsureStyles();
            var s = ServiceLocator.Get<GameState>();
            if (s == null) return;

            float k = UIHelper.Scale;
            var cfg = GameConfig.Instance;

            // ---- 行式布局参数：行高 = 字号 × ~1.8，杜绝竖向截字 ----
            float w = 312f * k;
            float pad = 14f * k;
            float titleH = 26f * k;  // 15px 标题
            float lineH = 24f * k;   // 13px 正文行
            float rowH = 22f * k;    // 12px 行（物资/副本/需求条标签）
            float btnH = 28f * k;
            float hintH = 20f * k;   // 11px 提示行
            float innerW = w - pad * 2f;

            bool hasDungeon = s.pendingDungeon.HasValue;

            float content =
                titleH
                + lineH * 2f
                + (hasDungeon ? rowH * 2f + 4f * k : 0f)
                + 6f * k + rowH * 3f
                + 6f * k + lineH * 2f
                + 8f * k + btnH + 6f * k
                + hintH;
            float h = content + pad * 2f;
            float x = 16f * k;
            float y = Screen.height - h - 48f * k; // 不贴底：预留任务栏高度

            UIHelper.DrawRect(new Rect(x, y, w, h), UIHelper.C_Panel);
            UIHelper.DrawBorder(x, y, w, h, UIHelper.C_Cyan);

            float cx = x + pad;
            float cy = y + pad;
            GUI.Label(new Rect(cx, cy, innerW, titleH), "末日公路", _title);
            cy += titleH;

            GUI.Label(new Rect(cx, cy, innerW, lineH),
                $"里程 {s.totalDistanceKm:F1} km　区域 {ZoneName(s.currentZoneIndex, cfg)}", _label);
            cy += lineH;
            GUI.Label(new Rect(cx, cy, innerW, lineH),
                $"天气 {WeatherName(s.currentWeather, cfg)}　{s.weatherRemainingSeconds:F0}s", _label);
            cy += lineH;

            // 副本状态（只读展示；「查看」入口在面板上方副本横幅，出现时自动提前台可操作）
            if (hasDungeon)
            {
                GUI.Label(new Rect(cx, cy, innerW, rowH),
                    $"⚠ 副本待处理：{DungeonDisplayName(s, cfg)}（剩 {s.pendingDungeonTimeout:F0}s）", _warn);
                cy += rowH;
                var ds = ServiceLocator.Get<DungeonSystem>();
                if (ds != null && ds.TryEstimatePendingReward(s, out int rmin, out int rmax))
                    GUI.Label(new Rect(cx, cy, innerW, rowH),
                        $"预计收益：+{rmin}~{rmax} 物资（同期挂机 × 倍数）", _small);
                else
                    GUI.Label(new Rect(cx, cy, innerW, rowH),
                        "预计收益：+物资若干（同期挂机 × 倍数）", _small);
                cy += rowH + 4f * k;
            }

            cy += 6f * k;
            DrawBar(cx, cy, innerW, "饥饿", s.hunger, k); cy += rowH;
            DrawBar(cx, cy, innerW, "口渴", s.thirst, k); cy += rowH;
            DrawBar(cx, cy, innerW, "生命", s.health, k); cy += rowH;

            cy += 6f * k;
            GUI.Label(new Rect(cx, cy, innerW, lineH),
                $"食 {s.food:F0}　水 {s.water:F0}　药 {s.medicine:F0}", _small);
            cy += lineH;
            GUI.Label(new Rect(cx, cy, innerW, lineH),
                $"油 {s.fuel:F0} 件 {s.parts:F0} 蓝 {s.blueprintFragments} 成就 {s.unlockedAchievementIds.Count}/{AchievementTotal(cfg)}", _small);
            cy += lineH;

            // 快捷按钮（6 个：背包/成就/小人/车辆/帮助/退出）
            cy += 8f * k;
            float bw = (innerW - 30f * k) / 6f;
            if (GUI.Button(new Rect(cx, cy, bw, btnH), "背包", _btn)) InventoryPanel.Toggle();
            if (GUI.Button(new Rect(cx + (bw + 6f * k), cy, bw, btnH), "成就", _btn)) AchievementGallery.Toggle();
            if (GUI.Button(new Rect(cx + (bw + 6f * k) * 2f, cy, bw, btnH), "小人", _btn)) SurvivorInfoPanel.Toggle();
            if (GUI.Button(new Rect(cx + (bw + 6f * k) * 3f, cy, bw, btnH), "车辆", _btn)) VehicleInfoPanel.Toggle();
            if (GUI.Button(new Rect(cx + (bw + 6f * k) * 4f, cy, bw, btnH), "帮助", _btn)) HelpPanel.Toggle();
            if (GUI.Button(new Rect(cx + (bw + 6f * k) * 5f, cy, bw, btnH), "退出", _btnExit)) Application.Quit();
            cy += btnH + 6f * k;
            GUI.Label(new Rect(cx, cy, innerW, hintH), "Ctrl+Shift+D 切换点击穿透", _hint);

            // ---- 面板上方浮动层（自下而上：副本横幅 → 遇怪警示条），与遇怪条同风格堆叠 ----
            float stackTop = y; // 面板顶

            // 副本横幅：待处理副本期间常驻，琥珀边框 + 倒计时 + 「查看」打开分支面板
            if (hasDungeon)
            {
                float bh = 32f * k;
                float by = stackTop - bh - 6f * k;
                UIHelper.DrawRect(new Rect(x, by, w, bh), UIHelper.C_Panel);
                UIHelper.DrawBorder(x, by, w, bh, UIHelper.C_Amber);
                GUI.Label(new Rect(x + 10f * k, by, w - 120f * k, bh),
                    $"⚠ 发现副本：{DungeonDisplayName(s, cfg)}（{s.pendingDungeonTimeout:F0}s）", _label);
                if (GUI.Button(new Rect(x + w - 106f * k, by + 4f * k, 96f * k, bh - 8f * k), "查看 ▶", _btn))
                    DungeonUI.OpenPanel();
                stackTop = by;
            }

            // 遇怪警示条（4 秒，叠在副本横幅之上）
            if (Time.realtimeSinceStartup < _monsterToastUntil)
            {
                float th = 26f * k;
                float ty = stackTop - th - 6f * k;
                UIHelper.DrawRect(new Rect(x, ty, w, th), UIHelper.C_Panel);
                UIHelper.DrawBorder(x, ty, w, th, UIHelper.C_Red);
                GUI.Label(new Rect(x, ty, w, th), "⚠ 遭遇怪物！小人属性下降", _toast);
                stackTop = ty;
            }

            // 对外暴露堆叠顶，供 DungeonUI 分支面板继续向上堆叠（同锚点、不遮挡）
            StackTopY = stackTop;
        }

        /// <summary>最近一帧浮动层最顶部的 Y（面板上方副本横幅/遇怪条之上）；无浮动层时等于面板顶。DungeonUI 分支面板据此向上堆叠。</summary>
        public static float StackTopY { get; private set; } = float.MinValue;

        private void DrawBar(float x, float y, float w, string name, float val, float k)
        {
            float rowH = 22f * k;   // 12px 标签行高
            float barH = 10f * k;
            float labelW = 46f * k, numW = 30f * k;
            GUI.Label(new Rect(x, y, labelW, rowH), name, _small);
            float bx = x + labelW;
            float bw = w - labelW - numW;
            UIHelper.DrawRect(new Rect(bx, y + (rowH - barH) / 2f, bw, barH), UIHelper.C_Dim);
            Color fill = val > 50f ? UIHelper.C_Green : (val > 20f ? UIHelper.C_Amber : UIHelper.C_Red);
            float f = Mathf.Clamp01(val / 100f);
            if (f > 0f) UIHelper.DrawRect(new Rect(bx, y + (rowH - barH) / 2f, bw * f, barH), fill);
            GUI.Label(new Rect(bx + bw + 2f * k, y, numW, rowH), $"{val:F0}", _small);
        }

        private static string ZoneName(int idx, GameConfig cfg)
            => (cfg != null && idx >= 0 && idx < cfg.zones.Count) ? cfg.zones[idx].name : "未知";

        private static string WeatherName(WeatherId w, GameConfig cfg)
        {
            var wc = (cfg != null) ? cfg.weathers.FirstOrDefault(x => x.id == w) : null;
            return wc != null ? wc.displayName : w.ToString();
        }

        private static string DungeonDisplayName(GameState s, GameConfig cfg)
        {
            if (cfg == null || !s.pendingDungeon.HasValue) return "?";
            var d = cfg.dungeons.FirstOrDefault(x => x.id == s.pendingDungeon.Value);
            return d != null ? d.displayName : s.pendingDungeon.Value.ToString();
        }

        private static int AchievementTotal(GameConfig cfg)
            => cfg != null ? cfg.achievements.Count : 0;
    }
}
