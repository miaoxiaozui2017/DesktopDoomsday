using UnityEngine;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 离线收益报告（SPEC 5.8）。Bootstrapper 加载后调用 OfflineSimulator 并暂存到 OfflineSimulator.Pending；
    /// 本组件在 Start 时读取并弹窗，仅当离线 >= 60s 才展示。关闭后恢复穿透。
    /// </summary>
    public class OfflineReportUI : MonoBehaviour
    {
        private GameObject _panel;
        private bool _interactive;

        private void Start()
        {
            var r = OfflineSimulator.Pending;
            OfflineSimulator.Pending = null;
            if (r.offlineSeconds < 60f) return; // 短暂停机不弹，避免打扰
            Show(r);
        }

        private void Show(OfflineReport r)
        {
            EnterInteractive();
            float w = 340f, h = 220f;
            float x = (Screen.width - w) / 2f;
            float y = (Screen.height - h) / 2f;
            _panel = UIHelper.Panel("OfflineReport", x, y, w, h, UIHelper.Palette.Panel, UIHelper.Palette.Cyan);

            UIHelper.Label(_panel, "离线收益结算", 16f, 12f, w - 32f, 22f, 15, UIHelper.Palette.Cyan);
            string time = FormatDuration(r.offlineSeconds);
            var lines = new System.Text.StringBuilder();
            lines.AppendLine($"离线时长：{time}");
            lines.AppendLine($"推进里程：+{r.distanceGain:F2} km");
            lines.AppendLine($"小人状态：{(r.survivorState == SurvivorState.Weakened ? "虚弱（降速）" : "正常")}");
            lines.AppendLine("物资变化：");
            if (r.resourceChanges != null && r.resourceChanges.Count > 0)
            {
                foreach (var d in r.resourceChanges)
                    lines.AppendLine($"  {ResName(d.resource)} {d.delta:+0;-0}");
            }
            else
                lines.AppendLine("  （无）");

            UIHelper.Label(_panel, lines.ToString(), 16f, 40f, w - 32f, h - 90f, 12, UIHelper.Palette.Text, TextAnchor.UpperLeft);
            UIHelper.Button(_panel, "收下", 16f, h - 42f, w - 32f, 30f, Close);
        }

        private void Close()
        {
            if (_panel != null) { Destroy(_panel); _panel = null; }
            if (_interactive) { UIHelper.ExitInteractive(); _interactive = false; }
        }

        private void EnterInteractive()
        {
            UIHelper.EnterInteractive();
            _interactive = true;
        }

        private static string FormatDuration(float sec)
        {
            int s = Mathf.FloorToInt(sec);
            int h = s / 3600; s %= 3600;
            int m = s / 60; s %= 60;
            if (h > 0) return $"{h} 小时 {m} 分";
            if (m > 0) return $"{m} 分 {s} 秒";
            return $"{s} 秒";
        }

        private static string ResName(string k) => k switch
        {
            "food" => "食物", "water" => "水", "medicine" => "药品",
            "fuel" => "燃料", "parts" => "零件", "blueprintFragments" => "蓝图碎片",
            _ => k
        };
    }
}
