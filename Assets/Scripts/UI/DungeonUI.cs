using UnityEngine;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;
using DesktopDoomsday.Systems;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 副本交互 UI（SPEC 5.6 + Batch 6）：非模态底部提示 + 轻量分支面板。
    /// 点击提示打开面板，选择分支 → DungeonSystem.Complete 结算；或忽略。不暂停公路（PauseRoadDuringDungeon 默认 false）。
    /// 显示期间关闭点击穿透（EnterInteractive）以便点击；关闭/超时后恢复宠物穿透态。
    /// </summary>
    public class DungeonUI : MonoBehaviour
    {
        private GameObject _prompt;
        private GameObject _panel;
        private DungeonId _current;
        private bool _interactive;

        private void OnEnable()
        {
            GameEvents.OnDungeonAvailable += ShowPrompt;
        }

        private void OnDisable()
        {
            GameEvents.OnDungeonAvailable -= ShowPrompt;
            Close();
        }

        private void Update()
        {
            // 副本超时自动忽略（DungeonSystem 清空 pending 不抛事件），同步关闭面板
            if ((_prompt != null || _panel != null))
            {
                var s = ServiceLocator.Get<GameState>();
                if (s == null || !s.pendingDungeon.HasValue)
                    Close();
            }
        }

        private void ShowPrompt(DungeonId id)
        {
            if (_prompt != null || _panel != null) return;
            _current = id;
            EnterInteractive();

            var cfg = GameConfig.Instance;
            var def = cfg != null ? cfg.dungeons.Find(d => d.id == id) : null;
            string name = def != null ? def.displayName : id.ToString();

            float w = 320f, h = 40f;
            float x = (Screen.width - w) / 2f;
            _prompt = UIHelper.Panel("DungeonPrompt", x, 70f, w, h, UIHelper.Palette.Panel, UIHelper.Palette.Amber);
            var btn = UIHelper.Button(_prompt, $"⚠ 副本出现：{name} — 点击查看", 0, 0, w, h, OpenPanel);
            btn.targetGraphic.color = new Color(0.10f, 0.14f, 0.18f, 0.96f);
        }

        private void OpenPanel()
        {
            if (_panel != null) return;
            var cfg = GameConfig.Instance;
            var def = cfg != null ? cfg.dungeons.Find(d => d.id == _current) : null;
            if (def == null) { Close(); return; }

            if (_prompt != null) { Destroy(_prompt); _prompt = null; }

            float w = 360f, h = 60f + def.branches.Count * 46f + 40f;
            float x = (Screen.width - w) / 2f;
            float y = (Screen.height - h) / 2f;
            _panel = UIHelper.Panel("DungeonPanel", x, y, w, h, UIHelper.Palette.Panel, UIHelper.Palette.Amber);

            UIHelper.Label(_panel, def.displayName, 14f, 10f, w - 28f, 22f, 15, UIHelper.Palette.Amber);
            UIHelper.Label(_panel, "选择一个行动分支（奖励约为同期挂机 2–3 倍）", 14f, 34f, w - 28f, 16f, 11, UIHelper.Palette.Sub);

            float by = 56f;
            for (int i = 0; i < def.branches.Count; i++)
            {
                int idx = i;
                var b = def.branches[i];
                UIHelper.Button(_panel, $"{b.branchName}  (×{b.rewardMultiplier:F1}, +{b.blueprintFragmentReward}蓝)",
                    14f, by, w - 28f, 38f, () => Choose(idx));
                by += 46f;
            }
            UIHelper.Button(_panel, "忽略（继续挂机）", 14f, by + 2f, w - 28f, 32f, Ignore);
        }

        private void Choose(int branchIndex)
        {
            var ds = ServiceLocator.Get<DungeonSystem>();
            var s = ServiceLocator.Get<GameState>();
            if (ds != null && s != null && s.pendingDungeon.HasValue)
                ds.Complete(_current, branchIndex, s);
            Close();
        }

        private void Ignore()
        {
            var ds = ServiceLocator.Get<DungeonSystem>();
            var s = ServiceLocator.Get<GameState>();
            if (ds != null && s != null && s.pendingDungeon.HasValue)
                ds.IgnorePending(s);
            Close();
        }

        private void Close()
        {
            if (_prompt != null) { Destroy(_prompt); _prompt = null; }
            if (_panel != null) { Destroy(_panel); _panel = null; }
            if (_interactive) { UIHelper.ExitInteractive(); _interactive = false; }
        }

        private void EnterInteractive()
        {
            UIHelper.EnterInteractive();
            _interactive = true;
        }
    }
}
