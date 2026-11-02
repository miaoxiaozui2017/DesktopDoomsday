using UnityEngine;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 成就图鉴（Batch 8，IMGUI）。按 A 键开关。列出 GameConfig.achievements 全部条目，显示中文名/说明与已解锁状态。
    /// 打开期间关闭点击穿透（SetInteractive）以便操作；关闭后恢复桌面宠物穿透态。
    /// </summary>
    public class AchievementGallery : MonoBehaviour
    {
        public static AchievementGallery Instance { get; private set; }
        private GUIStyle _title, _head, _label, _small, _lock, _btn;
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
            _lock  = UIHelper.NewLabelStyle((int)(12 * k), UIHelper.C_Muted, TextAnchor.MiddleLeft);
            _btn   = UIHelper.NewButtonStyle((int)(13 * k), UIHelper.C_Cyan);
            _inited = true;
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (!_open) return;
            var s = ServiceLocator.Get<GameState>();
            if (s == null) return;
            var cfg = GameConfig.Instance;

            int total = (cfg != null && cfg.achievements != null) ? cfg.achievements.Count : 0;
            int unlocked = s.unlockedAchievementIds.Count;

            float k = UIHelper.Scale;
            float w = 460f * k;
            float h = (96f + total * 54f + 16f) * k;
            float x = (Screen.width - w) / 2f;
            float y = (Screen.height - h) / 2f;
            UIHelper.DrawRect(new Rect(x, y, w, h), UIHelper.C_Panel);
            UIHelper.DrawBorder(x, y, w, h, UIHelper.C_Cyan);

            float cx = x + 16f * k;
            float cy = y + 14f * k;
            GUI.Label(new Rect(cx, cy, w - 32f * k, 24f * k), $"成就图鉴  {unlocked}/{total}", _title);
            cy += 30f * k;
            GUI.Label(new Rect(cx, cy, w - 32f * k, 18f * k), "达成条件后自动解锁，进度随存档保存。", _small);
            cy += 24f * k;

            if (cfg != null && cfg.achievements != null)
            {
                foreach (var a in cfg.achievements)
                {
                    bool got = s.unlockedAchievementIds.Contains(a.id);
                    string name = string.IsNullOrEmpty(a.displayName) ? a.id : a.displayName;
                    GUI.Label(new Rect(cx, cy, w - 120f * k, 22f * k), (got ? "★ " : "☆ ") + name, got ? _head : _label);
                    GUI.Label(new Rect(cx, cy + 20f * k, w - 120f * k, 18f * k),
                        string.IsNullOrEmpty(a.description) ? $"条件: {a.conditionType}≥{a.parameter}" : a.description,
                        got ? _small : _lock);
                    GUI.Label(new Rect(x + w - 96f * k, cy + 4f * k, 80f * k, 22f * k),
                        got ? "已解锁" : "未解锁", got ? _head : _lock);
                    cy += 54f * k;
                }
            }

            float btnH = 30f * k;
            if (GUI.Button(new Rect(x + w - 110f * k, y + h - btnH - 10f * k, 96f * k, btnH), "关闭 (A)", _btn))
            {
                _open = false;
                UIHelper.SetInteractive(false);
            }
        }
    }
}
