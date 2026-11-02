using UnityEngine;
using System.Linq;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;
using DesktopDoomsday.Systems;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 副本交互 UI（SPEC 5.6 + Batch 6，IMGUI 版，随分辨率缩放）。
    /// 底部提示条显示待处理副本与倒计时；点击「查看」打开分支面板，选分支 → DungeonSystem.Complete 结算，或忽略。
    /// 不暂停公路（PauseRoadDuringDungeon 默认 false）。显示期间关闭点击穿透（SetInteractive）以便点击；
    /// 副本被结算/超时清除后自动恢复桌面宠物穿透态（仅状态切换时调用，不每帧覆盖，避免与手动热键冲突）。
    /// </summary>
    public class DungeonUI : MonoBehaviour
    {
        private GUIStyle _title, _label, _btn, _small;
        private bool _inited;
        private bool _active;
        private bool _panelOpen;
        private float _k;

        private void EnsureStyles()
        {
            float k = UIHelper.Scale;
            if (_inited && Mathf.Abs(k - _k) < 0.001f) return;
            _k = k;
            _title = UIHelper.NewLabelStyle((int)(16 * k), UIHelper.C_Amber, TextAnchor.MiddleCenter, true);
            _label = UIHelper.NewLabelStyle((int)(13 * k), UIHelper.C_Text, TextAnchor.MiddleLeft);
            _small = UIHelper.NewLabelStyle((int)(12 * k), UIHelper.C_Text, TextAnchor.MiddleLeft);
            _btn   = UIHelper.NewButtonStyle((int)(13 * k), UIHelper.C_Cyan);
            _inited = true;
        }

        private void OnGUI()
        {
            EnsureStyles();
            var s = ServiceLocator.Get<GameState>();
            if (s == null) return;

            bool has = s.pendingDungeon.HasValue;
            if (has && !_active) { _active = true; _panelOpen = false; UIHelper.SetInteractive(true); }
            else if (!has && _active) { _active = false; _panelOpen = false; UIHelper.SetInteractive(false); }

            if (!has) return;

            float k = UIHelper.Scale;
            var ds = ServiceLocator.Get<DungeonSystem>();
            var cfg = GameConfig.Instance;
            var def = (cfg != null) ? cfg.dungeons.FirstOrDefault(d => d.id == s.pendingDungeon.Value) : null;
            string dname = def != null ? def.displayName : s.pendingDungeon.Value.ToString();

            if (!_panelOpen)
            {
                float w = 380f * k, h = 36f * k;
                float x = (Screen.width - w) / 2f;
                float y = Screen.height - h - 14f * k;
                UIHelper.DrawRect(new Rect(x, y, w, h), UIHelper.C_Panel);
                UIHelper.DrawBorder(x, y, w, h, UIHelper.C_Amber);
                GUI.Label(new Rect(x + 12f * k, y, w - 120f * k, h), $"⚠ 发现副本：{dname}（{s.pendingDungeonTimeout:F0}s）", _label);
                if (GUI.Button(new Rect(x + w - 110f * k, y + 4f * k, 98f * k, h - 8f * k), "查看 ▶", _btn))
                    _panelOpen = true;
            }
            else
            {
                int bc = def != null ? def.branches.Count : 0;
                float w = 400f * k;
                float lineH = 22f * k;
                float h = 64f * k + bc * 42f * k + 40f * k;
                float x = (Screen.width - w) / 2f;
                float y = (Screen.height - h) / 2f;
                UIHelper.DrawRect(new Rect(x, y, w, h), UIHelper.C_Panel);
                UIHelper.DrawBorder(x, y, w, h, UIHelper.C_Cyan);
                float cx = x + 16f * k;
                float cy = y + 12f * k;
                GUI.Label(new Rect(cx, cy, w - 32f * k, lineH), $"副本：{dname}", _title);
                cy += lineH + 4f * k;
                GUI.Label(new Rect(cx, cy, w - 32f * k, lineH), "选择一个行动分支（奖励约为同期挂机 2–3 倍）：", _small);
                cy += lineH + 8f * k;
                for (int i = 0; i < bc; i++)
                {
                    var b = def.branches[i];
                    string txt = $"{b.branchName}　×{b.rewardMultiplier:F1} 收益　蓝图+{b.blueprintFragmentReward}";
                    if (GUI.Button(new Rect(cx, cy, w - 32f * k, 36f * k), txt, _btn) && ds != null && s.pendingDungeon.HasValue)
                    {
                        ds.Complete(s.pendingDungeon.Value, i, s);
                        _panelOpen = false;
                    }
                    cy += 42f * k;
                }
                if (GUI.Button(new Rect(cx, cy, w - 32f * k, 32f * k), "忽略（继续挂机）", _small) && ds != null && s.pendingDungeon.HasValue)
                {
                    ds.IgnorePending(s);
                    _panelOpen = false;
                }
            }
        }
    }
}
