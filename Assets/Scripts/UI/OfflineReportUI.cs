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
        private bool _interactiveOn;
        private float _k;

        private void EnsureStyles()
        {
            float k = UIHelper.Scale;
            if (_inited && Mathf.Abs(k - _k) < 0.001f) return;
            _k = k;
            _title = UIHelper.NewLabelStyle((int)(17 * k), UIHelper.C_Cyan, TextAnchor.MiddleCenter, true);
            _label = UIHelper.NewLabelStyle((int)(13 * k), UIHelper.C_Text, TextAnchor.MiddleLeft);
            _small = UIHelper.NewLabelStyle((int)(12 * k), UIHelper.C_Text, TextAnchor.MiddleLeft);
            _btn = UIHelper.NewButtonStyle((int)(13 * k), UIHelper.C_Cyan);
            _inited = true;
        }

        private void OnGUI()
        {
            EnsureStyles();
            var r = OfflineSimulator.Pending;
            if (r.offlineSeconds < 60f) return; // 短暂停机不弹，避免打扰
            if (_shown) return;

            // 启动即弹的离线报告：TransparentWindow 透明化在协程里延迟一帧，会把刚关的穿透重新打开。
            // 因此打开时计数 +1（SetInteractive），之后每帧持续确保穿透关闭 + 窗口在前（EnsureForeground，不碰计数），
            // 对抗延迟生效的穿透样式与 Windows 防抢焦点限制，保证「收下」按钮立即可点。
            if (!_interactiveOn) { _interactiveOn = true; UIHelper.SetInteractive(true); }
            UIHelper.EnsureForeground(); // 每帧重申：关穿透 + 强制激活（AttachThreadInput 绕过系统限制）

            float k = UIHelper.Scale;
            float w = 380f * k;
            float h = 320f * k;
            float x = (Screen.width - w) / 2f;
            float y = (Screen.height - h) / 2f;
            UIHelper.DrawRect(new Rect(x, y, w, h), UIHelper.C_Panel);
            UIHelper.DrawBorder(x, y, w, h, UIHelper.C_Cyan);
            float cx = x + 18f * k;
            float cy = y + 16f * k;
            GUI.Label(new Rect(cx, cy, w - 36f * k, 26f * k), "离线收益结算", _title);
            cy += 32f * k;
            GUI.Label(new Rect(cx, cy, w - 36f * k, 22f * k), $"你离开了约 {FormatDuration(r.offlineSeconds)}", _small);
            cy += 26f * k;
            GUI.Label(new Rect(cx, cy, w - 36f * k, 22f * k), $"推进里程：+{r.distanceGain:F1} km", _label);
            cy += 26f * k;
            GUI.Label(new Rect(cx, cy, w - 36f * k, 22f * k), "资源变化：", _label);
            cy += 24f * k;
            if (r.resourceChanges != null)
            {
                foreach (var d in r.resourceChanges)
                {
                    if (d.delta == 0) continue;
                    GUI.Label(new Rect(cx + 12f * k, cy, w - 48f * k, 22f * k), $"{ResName(d.resource)} {d.delta:+0;-0}", _small);
                    cy += 22f * k;
                }
            }
            cy += 6f * k;
            GUI.Label(new Rect(cx, cy, w - 36f * k, 22f * k),
                $"小人状态：{(r.survivorState == SurvivorState.Weakened ? "虚弱（降速）" : "正常")}", _small);
            cy += 34f * k;
            if (GUI.Button(new Rect(cx, cy, w - 36f * k, 34f * k), "收下", _btn))
            {
                _shown = true;
                OfflineSimulator.Pending = default(OfflineReport);
                _interactiveOn = false;
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
