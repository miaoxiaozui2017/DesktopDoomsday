using UnityEngine;
using DesktopDoomsday.Core;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 帮助页（Batch 8，IMGUI）。按 H 键开关。说明玩法、如何退出、正式/调试快捷键、窗口图层行为。
    /// 打开期间关闭点击穿透（SetInteractive）以便操作；关闭后恢复桌面宠物穿透态。
    /// </summary>
    public class HelpPanel : MonoBehaviour
    {
        public static HelpPanel Instance { get; private set; }
        private GUIStyle _title, _head, _wrap, _key, _btn;
        private bool _inited;
        private bool _open;
        private Vector2 _scroll;
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
            _head  = UIHelper.NewLabelStyle((int)(14 * k), UIHelper.C_Amber, TextAnchor.UpperLeft, true);
            _wrap  = UIHelper.NewLabelStyle((int)(13 * k), UIHelper.C_Text, TextAnchor.UpperLeft);
            _wrap.wordWrap = true;
            _key   = UIHelper.NewLabelStyle((int)(13 * k), UIHelper.C_Green, TextAnchor.UpperLeft);
            _key.wordWrap = true;
            _btn   = UIHelper.NewButtonStyle((int)(13 * k), UIHelper.C_Cyan);
            _inited = true;
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (!_open) return;
            float k = UIHelper.Scale;
            float w = 540f * k, h = 440f * k;
            float x = (Screen.width - w) / 2f;
            float y = (Screen.height - h) / 2f;
            UIHelper.DrawRect(new Rect(x, y, w, h), UIHelper.C_Panel);
            UIHelper.DrawBorder(x, y, w, h, UIHelper.C_Cyan);

            GUILayout.BeginArea(new Rect(x, y, w, h));
            GUILayout.Label("末日公路 · 帮助", _title);
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(h - 80f * k));

            GUILayout.Label("【玩法】", _head);
            GUILayout.Label("这是一款桌面放置游戏。你的小车沿末日公路自动前进，挂机累积里程与资源。" +
                "沿途宝箱补充物资（物资有限，掉落随机，要精打细算）；小人消耗食物/水/药品，需求过低会虚弱降速（但不会死亡）。" +
                "蓝图碎片主要来自副本分支奖励——挂机宝箱不掉蓝图、零件也极少；想升级改装引擎/货箱，就多接副本。" +
                "开箱有几率遭遇怪物：前期城郊+晴天几乎遇不到，越往后的区域（荒漠/冻原）风险越高；遇怪会扣小人三项属性（钳制不为零，不会死），补给充足时可「一键补满」恢复。" +
                "天气与区域改变车速、消耗与遇怪风险；离线按 0.5 倍速率推进（零件每 2 小时至多 +1、蓝图零产出）；" +
                "达成里程/宝箱/副本/区域等条件解锁成就。", _wrap);
            GUILayout.Space(6f * k);

            GUILayout.Label("【如何退出】", _head);
            GUILayout.Label("方式一：左下角状态面板的「退出」按钮（推荐）。\n方式二：如果 UI 卡死无法点击，按 Alt+F4 强制退出。", _wrap);
            GUILayout.Space(6f * k);

            GUILayout.Label("【正式快捷键】", _head);
            GUILayout.Label("I　打开/关闭 背包（物资详情 + 一键补满）\n" +
                            "A　打开/关闭 成就图鉴\n" +
                            "H　打开/关闭 本帮助页\n" +
                            "K　打开/关闭 小人属性页（含「一键补满」按钮，按库存补满食/水/药）\n" +
                            "V　打开/关闭 车辆属性页（含引擎/货箱改装「升级」按钮）\n" +
                            "Ctrl+Shift+D　切换点击穿透（默认开启，鼠标可穿过窗口操作后方应用）", _key);
            GUILayout.Space(6f * k);

            GUILayout.Label("【调试快捷键（开发用）】", _head);
            GUILayout.Label("~　开关调试 HUD\n" +
                            "D　强制触发副本　C　以分支0完成副本", _key);
            GUILayout.Space(6f * k);

            GUILayout.Label("【窗口与图层】", _head);
            GUILayout.Label("默认窗口仅位于桌面背景之上，不会遮挡其它应用窗口；" +
                "需要点击游戏内按钮/面板时，对应面板会自动临时转为可交互并提到前台，关闭后自动恢复。" +
                "副本出现时会自动切到可操作状态（提前台 + 可点击），选择分支或超时后自动恢复挂机穿透态，无需手动切换。", _wrap);
            GUILayout.Space(6f * k);

            GUILayout.Label("【重置存档】", _head);
            GUILayout.Label("存档为单份全局 save.json（在游戏用户目录）。删除该文件即清空进度重新开始。", _wrap);

            GUILayout.EndScrollView();
            if (GUILayout.Button("关闭 (H)", _btn)) { _open = false; UIHelper.SetInteractive(false); }
            GUILayout.EndArea();
        }
    }
}