using UnityEngine;
using System.Linq;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;
using DesktopDoomsday.Systems;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 副本交互 UI（SPEC 5.6 + Batch 6，IMGUI 版）。
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

        private void EnsureStyles()
        {
            if (_inited) return;
            _title = UIHelper.NewLabelStyle(16, UIHelper.C_Amber, TextAnchor.MiddleCenter, true);
            _label = UIHelper.NewLabelStyle(13, UIHelper.C_Text, TextAnchor.MiddleLeft);
            _small = UIHelper.NewLabelStyle(12, UIHelper.C_Text, TextAnchor.MiddleLeft);
            _btn = UIHelper.NewButtonStyle(13, UIHelper.C_Cyan);
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

            var ds = ServiceLocator.Get<DungeonSystem>();
            var cfg = GameConfig.Instance;
            var def = (cfg != null) ? cfg.dungeons.FirstOrDefault(d => d.id == s.pendingDungeon.Value) : null;
            string dname = def != null ? def.displayName : s.pendingDungeon.Value.ToString();

            if (!_panelOpen)
            {
                float w = 380f, h = 36f;
                float x = (Screen.width - w) / 2f;
                float y = Screen.height - h - 14f;
                UIHelper.DrawRect(new Rect(x, y, w, h), UIHelper.C_Panel);
                UIHelper.DrawBorder(x, y, w, h, UIHelper.C_Amber);
                GUI.Label(new Rect(x + 12f, y, w - 120f, h), $"⚠ 发现副本：{dname}（{s.pendingDungeonTimeout:F0}s）", _label);
                if (GUI.Button(new Rect(x + w - 110f, y + 4f, 98f, h - 8f), "查看 ▶", _btn))
                    _panelOpen = true;
            }
            else
            {
                int bc = def != null ? def.branches.Count : 0;
                float w = 400f;
                float lineH = 22f;
                float h = 64f + bc * 42f + 40f;
                float x = (Screen.width - w) / 2f;
                float y = (Screen.height - h) / 2f;
                UIHelper.DrawRect(new Rect(x, y, w, h), UIHelper.C_Panel);
                UIHelper.DrawBorder(x, y, w, h, UIHelper.C_Cyan);
                float cx = x + 16f;
                float cy = y + 12f;
                GUI.Label(new Rect(cx, cy, w - 32f, lineH), $"副本：{dname}", _title);
                cy += lineH + 4f;
                GUI.Label(new Rect(cx, cy, w - 32f, lineH), "选择一个行动分支（奖励约为同期挂机 2–3 倍）：", _small);
                cy += lineH + 8f;
                for (int i = 0; i < bc; i++)
                {
                    var b = def.branches[i];
                    string txt = $"{b.branchName}　×{b.rewardMultiplier:F1} 收益　蓝图+{b.blueprintFragmentReward}";
                    if (GUI.Button(new Rect(cx, cy, w - 32f, 36f), txt, _btn) && ds != null && s.pendingDungeon.HasValue)
                    {
                        ds.Complete(s.pendingDungeon.Value, i, s);
                        _panelOpen = false;
                    }
                    cy += 42f;
                }
                if (GUI.Button(new Rect(cx, cy, w - 32f, 32f), "忽略（继续挂机）", _small) && ds != null && s.pendingDungeon.HasValue)
                {
                    ds.IgnorePending(s);
                    _panelOpen = false;
                }
            }
        }
    }
}
