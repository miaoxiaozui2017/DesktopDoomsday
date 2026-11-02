using UnityEngine;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 离线收益报告（SPEC 5.8，IMGUI 版）。Bootstrapper 加载后调用 OfflineSimulator 并暂存到 OfflineSimulator.Pending；
    /// 本组件在 OnGUI 时读取并弹窗，仅当离线 >= 60s 才展示。关闭后恢复穿透。
    /// </summary>
    public class OfflineReportUI : MonoBehaviour
    {
        private GUIStyle _title, _label, _btn, _small;
        private bool _inited;
        private bool _shown;

        private void EnsureStyles()
        {
            if (_inited) return;
            _title = UIHelper.NewLabelStyle(17, UIHelper.C_Cyan, TextAnchor.MiddleCenter, true);
            _label = UIHelper.NewLabelStyle(13, UIHelper.C_Text, TextAnchor.MiddleLeft);
            _small = UIHelper.NewLabelStyle(12, UIHelper.C_Text, TextAnchor.MiddleLeft);
            _btn = UIHelper.NewButtonStyle(13, UIHelper.C_Cyan);
            _inited = true;
        }

        private void OnGUI()
        {
            EnsureStyles();
            var r = OfflineSimulator.Pending;
            if (r.offlineSeconds < 60f) return; // 短暂停机不弹，避免打扰
            if (_shown) return;

            UIHelper.SetInteractive(true); // 弹窗期间关闭穿透以便点击

            float w = 360f, h = 280f;
            float x = (Screen.width - w) / 2f;
            float y = (Screen.height - h) / 2f;
            UIHelper.DrawRect(new Rect(x, y, w, h), UIHelper.C_Panel);
            UIHelper.DrawBorder(x, y, w, h, UIHelper.C_Cyan);
            float cx = x + 18f;
            float cy = y + 16f;
            GUI.Label(new Rect(cx, cy, w - 36f, 24f), "离线收益结算", _title);
            cy += 30f;
            GUI.Label(new Rect(cx, cy, w - 36f, 20f), $"你离开了约 {FormatDuration(r.offlineSeconds)}", _small);
            cy += 26f;
            GUI.Label(new Rect(cx, cy, w - 36f, 20f), $"推进里程：+{r.distanceGain:F1} km", _label);
            cy += 24f;
            GUI.Label(new Rect(cx, cy, w - 36f, 20f), "资源变化：", _label);
            cy += 22f;
            if (r.resourceChanges != null)
            {
                foreach (var d in r.resourceChanges)
                {
                    if (d.delta == 0) continue;
                    GUI.Label(new Rect(cx + 12f, cy, w - 48f, 18f), $"{ResName(d.resource)} {d.delta:+0;-0}", _small);
                    cy += 18f;
                }
            }
            cy += 6f;
            GUI.Label(new Rect(cx, cy, w - 36f, 20f),
                $"小人状态：{(r.survivorState == SurvivorState.Weakened ? "虚弱（降速）" : "正常")}", _small);
            cy += 34f;
            if (GUI.Button(new Rect(cx, cy, w - 36f, 34f), "收下", _btn))
            {
                _shown = true;
                OfflineSimulator.Pending = default(OfflineReport);
                UIHelper.SetInteractive(false);
            }
        }

        private static string FormatDuration(float sec)
        {
            int s = Mathf.FloorToInt(sec);
            int h = s / 3600; int m = (s % 3600) / 60; int ss = s % 60;
            if (h > 0) return $"{h} 小时 {m} 分";
            if (m > 0) return $"{m} 分 {ss} 秒";
            return $"{ss} 秒";
        }

        private static string ResName(string k) => k switch
        {
            "food" => "食物", "water" => "水", "medicine" => "药品",
            "fuel" => "燃料", "parts" => "零件", "blueprintFragments" => "蓝图碎片",
            _ => k
        };
    }
}
