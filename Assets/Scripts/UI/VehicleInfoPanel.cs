using System.Linq;
using UnityEngine;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;
using DesktopDoomsday.Systems;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 车辆独立属性页（按 V 调取，IMGUI 版，随分辨率缩放 + 中文字体）。
    /// 展示当前车辆肖像、两条改装线（引擎=速度 / 货箱=容量）的等级、当前效果、下一级成本，
    /// 并提供每条线的「升级」按钮（VehicleSystem.TryUpgrade：消耗零件 + 蓝图碎片）。
    /// 打开期间自动关闭点击穿透（SetInteractive）以便点击；关闭后恢复桌面宠物穿透态。
    /// </summary>
    public class VehicleInfoPanel : MonoBehaviour
    {
        public static VehicleInfoPanel Instance { get; private set; }
        public bool show;
        private Sprite[] _vehFrames = new Sprite[3]; // 三种车各取第 0 帧
        private GUIStyle _title, _text, _small, _hint, _btn, _btnOff;
        private bool _stylesReady;
        private float _k;

        private void Awake()
        {
            Instance = this;
            LoadFrames();
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
            if (Input.GetKeyDown(KeyCode.V)) Toggle();
        }

        private void OnGUI()
        {
            if (!show) return;
            var s = ServiceLocator.Get<GameState>();
            var vs = ServiceLocator.Get<VehicleSystem>();
            if (s == null || vs == null) return;
            EnsureStyles();

            float k = UIHelper.Scale;
            var cfg = GameConfig.Instance;
            var vc = cfg?.vehicles?.FirstOrDefault(v => v.id == s.currentVehicle);

            float w = 400f * k;
            float pad = 14f * k;
            float lineH = 24f * k;   // 13px 行
            float rowH = 22f * k;    // 12px 行
            float btnH = 28f * k;
            int trackCount = (vc != null && vc.upgradeTracks != null) ? vc.upgradeTracks.Count : 2;

            // 内容高度：标题 + 车辆行 + 每线（信息行+成本行+等级条+按钮）×2 + 库存行 + 关闭按钮
            float content = 26f * k + lineH
                + trackCount * (lineH + rowH + 8f * k + 4f * k + btnH + 8f * k)
                + lineH + 8f * k + btnH;
            float h = content + pad * 2f;
            float x = Screen.width - w - 20f * k; // 右侧垂直居中（与 K 小人面板错位，避免同开遮挡）
            float y = (Screen.height - h) / 2f;

            UIHelper.DrawRect(new Rect(x, y, w, h), UIHelper.C_Panel);
            UIHelper.DrawBorder(x, y, w, h, UIHelper.C_Cyan);

            // 肖像（右上）
            var fr = _vehFrames.Length > (int)s.currentVehicle ? _vehFrames[(int)s.currentVehicle] : null;
            if (fr != null)
                GUI.DrawTexture(new Rect(x + w - pad - 116f * k, y + pad + 24f * k, 116f * k, 80f * k),
                    fr.texture, ScaleMode.ScaleToFit);

            float dataW = w - pad * 2f - 128f * k; // 数据列（给肖像留位）
            float cx = x + pad;
            float cy = y + pad;
            GUI.Label(new Rect(cx, cy, dataW, 26f * k), "车辆 · VEHICLE", _title);
            cy += 26f * k;

            GUI.Label(new Rect(cx, cy, dataW, lineH), $"当前：{VehicleName(s.currentVehicle)}", _text);
            cy += lineH;

            for (int t = 0; t < trackCount; t++)
            {
                string tName = (vc != null && t < vc.upgradeTracks.Count) ? vc.upgradeTracks[t].trackName : $"改装线{t}";
                int flat = (int)s.currentVehicle * 2 + t;
                int lvl = (s.upgradeLevels != null && flat < s.upgradeLevels.Length) ? s.upgradeLevels[flat] : 0;
                int maxLv = (vc != null && t < vc.upgradeTracks.Count) ? vc.upgradeTracks[t].maxLevel : 2;

                // 效果说明：引擎=速度加成（已生效）；货箱/油箱=容量类加成（MVP 预留，暂未接入公路数值）
                string effect = "—";
                var track = (vc != null && t < vc.upgradeTracks.Count) ? vc.upgradeTracks[t] : null;
                if (track != null)
                {
                    if (track.speedBonusPerLevel != null && track.speedBonusPerLevel.Length > 0)
                        effect = $"速度 +{vs.GetSpeedBonus(s, s.currentVehicle, t):F2}（每级 +{track.speedBonusPerLevel[0]:F2}）";
                    else if (track.capacityBonusPerLevel != null && track.capacityBonusPerLevel.Length > 0)
                        effect = $"容量 +{track.capacityBonusPerLevel.Take(lvl).Sum():F0}（每级 +{track.capacityBonusPerLevel[0]:F0}，预留）";
                }

                GUI.Label(new Rect(cx, cy, dataW, lineH), $"{tName}　Lv{lvl}/{maxLv}　{effect}", _text);
                cy += lineH;

                bool can = vs.GetUpgradeCost(s, s.currentVehicle, t, out int pc, out int fc, out bool mx);
                bool afford = can && s.parts >= pc && s.blueprintFragments >= fc;
                GUI.Label(new Rect(cx, cy, dataW, rowH),
                    can ? $"下一级成本：{pc} 零件 + {fc} 蓝图碎片" : (mx ? "已满级" : "—"), _small);
                cy += rowH;

                // 等级条
                float barW = dataW;
                UIHelper.DrawRect(new Rect(cx, cy, barW, 8f * k), UIHelper.C_Dim);
                float ratio = maxLv > 0 ? (float)lvl / maxLv : 0f;
                if (ratio > 0f) UIHelper.DrawRect(new Rect(cx, cy, barW * ratio, 8f * k), UIHelper.C_Cyan);
                cy += 8f * k + 4f * k;

                // 升级按钮（满级/资源不足置灰，并说明缺什么）
                string upText = !can ? (mx ? "已满级" : "无法升级")
                    : afford ? $"升 级（{pc}件 + {fc}蓝）"
                    : (s.parts < pc ? $"零件不足（需 {pc}）" : $"蓝图不足（需 {fc}）");
                GUI.enabled = afford;
                if (GUI.Button(new Rect(cx, cy, dataW, btnH), upText, afford ? _btn : _btnOff))
                {
                    bool ok = vs.TryUpgrade(s, s.currentVehicle, t);
                    Debug.Log($"[VehicleInfoPanel] 升级 {tName} -> {ok}（剩 零件{s.parts} 蓝{s.blueprintFragments}）");
                }
                GUI.enabled = true;
                cy += btnH + 8f * k;
            }

            GUI.Label(new Rect(cx, cy, dataW, lineH),
                $"库存：零件 {s.parts}　蓝图碎片 {s.blueprintFragments}", _small);
            cy += lineH + 8f * k;
            // 关闭按钮（替代原提示行，与背包页风格统一）
            if (GUI.Button(new Rect(cx, cy, dataW, btnH), "关闭 (V)", _btn))
            {
                show = false;
                UIHelper.SetInteractive(false);
            }
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
            _btnOff = UIHelper.NewButtonStyle((int)(12 * k), UIHelper.C_Muted);
            _stylesReady = true;
        }

        private static string VehicleName(VehicleId id)
        {
            switch (id) { case VehicleId.Van: return "厢式货车"; case VehicleId.Motorcycle: return "摩托"; default: return "皮卡"; }
        }

        private void LoadFrames()
        {
            string[] paths = {
                "Art/Sprites/Vehicles/pickup_0",
                "Art/Sprites/Vehicles/van_0",
                "Art/Sprites/Vehicles/moto_0"
            };
            for (int i = 0; i < 3; i++)
            {
                var s = Resources.Load<Sprite>(paths[i]);
                if (s != null && s.texture != null) s.texture.filterMode = FilterMode.Point;
                _vehFrames[i] = s;
            }
        }
    }
}
