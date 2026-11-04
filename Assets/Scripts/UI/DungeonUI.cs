using UnityEngine;
using System.Linq;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;
using DesktopDoomsday.Systems;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 副本交互 UI（SPEC 5.6 + Batch 6，IMGUI 版，随分辨率缩放）。
    /// 发现副本的常驻横幅（倒计时 + 「查看」入口）由 MainHud 画在状态面板上方（同遇怪警示条风格）；
    /// 本组件只负责分支选择面板：点「查看」后打开，锚定在 MainHud 浮动层（StackTopY）正上方继续堆叠，
    /// 选分支 → DungeonSystem.Complete 结算，或忽略。
    /// 不暂停公路（PauseRoadDuringDungeon 默认 false）。待处理期间关闭点击穿透（SetInteractive）以便点击；
    /// 副本被结算/超时清除后自动恢复桌面宠物穿透态（仅状态切换时调用，不每帧覆盖，避免与手动热键冲突）。
    /// </summary>
    public class DungeonUI : MonoBehaviour
    {
        public static DungeonUI Instance { get; private set; }

        private GUIStyle _title, _label, _btn, _small;
        private bool _inited;
        private bool _active;
        private bool _panelOpen;
        private float _k;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>打开分支选择面板（由 MainHud 副本横幅上的「查看」按钮调用）。</summary>
        public static void OpenPanel()
        {
            if (Instance != null) Instance._panelOpen = true;
        }

        private void EnsureStyles()
        {
            float k = UIHelper.Scale;
            if (_inited && Mathf.Abs(k - _k) < 0.001f) return;
            _k = k;
            _title = UIHelper.NewLabelStyle((int)(16 * k), UIHelper.C_Amber, TextAnchor.MiddleLeft, true);
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

            if (!has || !_panelOpen) return;

            // 面板打开期间每帧确保穿透关闭 + 窗口在前（对抗用户切走再回来、或延迟透明化覆盖），保证分支按钮始终可点
            UIHelper.EnsureForeground();

            float k = UIHelper.Scale;
            var ds = ServiceLocator.Get<DungeonSystem>();
            var cfg = GameConfig.Instance;
            var def = (cfg != null) ? cfg.dungeons.FirstOrDefault(d => d.id == s.pendingDungeon.Value) : null;
            string dname = def != null ? def.displayName : s.pendingDungeon.Value.ToString();

            int bc = def != null ? def.branches.Count : 0;
            float w = 400f * k;
            float lineH = 22f * k;
            float h = 64f * k + bc * 42f * k + 40f * k;

            // 锚定在 MainHud 浮动层（副本横幅/遇怪条）正上方，同左侧锚点继续堆叠；
            // StackTopY 尚未初始化（首帧）或小屏越界时退回屏幕居中。
            float x = 16f * k;
            float y = MainHud.StackTopY - h - 8f * k;
            if (MainHud.StackTopY == float.MinValue || y < 8f * k)
            {
                x = (Screen.width - w) / 2f;
                y = (Screen.height - h) / 2f;
            }

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
