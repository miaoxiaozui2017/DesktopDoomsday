using UnityEngine;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;
using DesktopDoomsday.Systems;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 背包 / 物资页（Batch 8，IMGUI）。按 I 键开关。详细列出所有资源数量、当前车辆与改装等级、小人三项需求与状态。
    /// 打开期间关闭点击穿透（SetInteractive）以便操作；关闭后恢复桌面宠物穿透态。
    /// </summary>
    public class InventoryPanel : MonoBehaviour
    {
        public static InventoryPanel Instance { get; private set; }
        private GUIStyle _title, _head, _label, _small, _btn, _green, _amber, _red, _hint;
        private bool _inited;
        private bool _open;
        private float _k;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public static void Toggle()
        {
            if (Instance == null) return;
            Instance._open = !Instance._open;
            UIHelper.SetInteractive(Instance._open);
        }

        private void EnsureStyles()
        {
            float k = UIHelper.Scale;
            if (_inited && Mathf.Abs(k - _k) < 0.001f) return;
            _k = k;
            _title = UIHelper.NewLabelStyle((int)(18 * k), UIHelper.C_Cyan, TextAnchor.MiddleCenter, true);
            _head  = UIHelper.NewLabelStyle((int)(14 * k), UIHelper.C_Amber, TextAnchor.MiddleLeft, true);
            _label = UIHelper.NewLabelStyle((int)(13 * k), UIHelper.C_Text, TextAnchor.MiddleLeft);
            _small = UIHelper.NewLabelStyle((int)(12 * k), UIHelper.C_Text, TextAnchor.MiddleLeft);
            _btn   = UIHelper.NewButtonStyle((int)(13 * k), UIHelper.C_Cyan);
            _green = UIHelper.NewLabelStyle((int)(12 * k), UIHelper.C_Green, TextAnchor.MiddleLeft);
            _amber = UIHelper.NewLabelStyle((int)(12 * k), UIHelper.C_Amber, TextAnchor.MiddleLeft);
            _red   = UIHelper.NewLabelStyle((int)(12 * k), UIHelper.C_Red, TextAnchor.MiddleLeft);
            _hint  = UIHelper.NewLabelStyle((int)(11 * k), UIHelper.C_Muted, TextAnchor.MiddleLeft);
            _inited = true;
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (!_open) return;
            var s = ServiceLocator.Get<GameState>();
            if (s == null) return;
            var cfg = GameConfig.Instance;

            float k = UIHelper.Scale;
            float w = 380f * k;
            float h = 476f * k;
            float x = (Screen.width - w) / 2f;
            float y = (Screen.height - h) / 2f;
            UIHelper.DrawRect(new Rect(x, y, w, h), UIHelper.C_Panel);
            UIHelper.DrawBorder(x, y, w, h, UIHelper.C_Cyan);

            float cx = x + 16f * k;
            float cy = y + 14f * k;
            GUI.Label(new Rect(cx, cy, w - 32f * k, 26f * k), "背包 / 物资", _title);
            cy += 32f * k;

            // 资源清单
            GUI.Label(new Rect(cx, cy, w - 32f * k, 24f * k), "资源", _head);
            cy += 28f * k;
            float colW = (w - 32f * k) / 2f;
            Row(cx, cy, "食物", s.food, colW, k); Row(cx + colW, cy, "水", s.water, colW, k); cy += 22f * k;
            Row(cx, cy, "药品", s.medicine, colW, k); Row(cx + colW, cy, "燃油", s.fuel, colW, k); cy += 22f * k;
            Row(cx, cy, "零件", s.parts, colW, k); Row(cx + colW, cy, "蓝图碎片", s.blueprintFragments, colW, k); cy += 26f * k;

            // 当前车辆与改装
            GUI.Label(new Rect(cx, cy, w - 32f * k, 24f * k), "车辆", _head);
            cy += 28f * k;
            string vname = "未知";
            var vc = (cfg != null && cfg.vehicles != null) ? cfg.vehicles.Find(v => v.id == s.currentVehicle) : null;
            if (vc != null) vname = vc.displayName;
            GUI.Label(new Rect(cx, cy, w - 32f * k, 22f * k), $"当前：{vname}", _small);
            cy += 22f * k;
            if (vc != null && vc.upgradeTracks != null)
            {
                for (int t = 0; t < vc.upgradeTracks.Count; t++)
                {
                    int lvl = 0;
                    int idx = (int)s.currentVehicle * 2 + t;
                    if (s.upgradeLevels != null && idx < s.upgradeLevels.Length) lvl = s.upgradeLevels[idx];
                    int max = vc.upgradeTracks[t].maxLevel;
                    GUI.Label(new Rect(cx, cy, w - 32f * k, 22f * k),
                        $"{vc.upgradeTracks[t].trackName}：Lv {lvl}/{max}", _small);
                    cy += 22f * k;
                }
            }
            cy += 8f * k;

            // 小人需求
            GUI.Label(new Rect(cx, cy, w - 32f * k, 24f * k), "小人状态", _head);
            cy += 26f * k;
            cy = Bar(cx, cy, w - 32f * k, 9f * k, "饥饿", s.hunger, k);
            cy = Bar(cx, cy + 5f * k, w - 32f * k, 9f * k, "口渴", s.thirst, k);
            cy = Bar(cx, cy + 5f * k, w - 32f * k, 9f * k, "生命", s.health, k);
            cy += 8f * k;
            GUI.Label(new Rect(cx, cy, w - 32f * k, 22f * k),
                "状态：" + (s.survivorState == SurvivorState.Weakened ? "虚弱（降速）" : "正常"),
                s.survivorState == SurvivorState.Weakened ? _red : _green);
            cy += 26f * k;

            // 一键补满：按库存消耗 食/水/药 各 1 单位把三项需求补满（SurvivorSystem.FillAllNeeds）
            bool full = s.hunger >= 100f && s.thirst >= 100f && s.health >= 100f;
            bool noSupply = s.food < 1f && s.water < 1f && s.medicine < 1f;
            string fillText = full ? "状态已满"
                : noSupply ? "物资不足"
                : $"一键补满（食{s.food:F0} 水{s.water:F0} 药{s.medicine:F0}）";
            GUI.enabled = !full && !noSupply;
            if (GUI.Button(new Rect(cx, cy, w - 32f * k, 28f * k), fillText, _btn))
            {
                var sys = ServiceLocator.Get<SurvivorSystem>();
                if (sys != null) sys.FillAllNeeds(s);
            }
            GUI.enabled = true;
            cy += 32f * k;

            GUI.Label(new Rect(cx, cy, w - 32f * k, 20f * k),
                "提示：需求低于 40 自动补货；背包空了才进虚弱（不会死亡）", _hint);

            if (GUI.Button(new Rect(x + w - 110f * k, y + h - 40f * k, 96f * k, 30f * k), "关闭 (I)", _btn))
            {
                _open = false;
                UIHelper.SetInteractive(false);
            }
        }

        private void Row(float x, float y, string name, float val, float colW, float k)
        {
            GUI.Label(new Rect(x, y, colW, 22f * k), $"{name}：{val:F0}", _small);
        }

        private float Bar(float x, float y, float w, float h, string name, float val, float k)
        {
            float labelW = 46f * k, numW = 30f * k; // 标签区加宽，容纳两字需求名 + 默认内边距
            GUI.Label(new Rect(x, y - 3f * k, labelW, 22f * k), name, _small);
            float bx = x + labelW;
            float bw = w - labelW - numW;
            UIHelper.DrawRect(new Rect(bx, y, bw, h), UIHelper.C_Dim);
            Color fill = val > 50f ? UIHelper.C_Green : (val > 20f ? UIHelper.C_Amber : UIHelper.C_Red);
            float f = Mathf.Clamp01(val / 100f);
            if (f > 0f) UIHelper.DrawRect(new Rect(bx, y, bw * f, h), fill);
            GUI.Label(new Rect(bx + bw + 2f * k, y - 3f * k, numW, 22f * k), $"{val:F0}", _small);
            return y + h;
        }
    }
}
