using UnityEngine;
using System.IO;
using DesktopDoomsday.Window;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// IMGUI 工具集（不依赖 UnityEngine.UI 程序集，规避部分工程 uGUI 模块未接入导致的 CS0234）。
    /// 提供：纯色纹理、文本/按钮样式构造、中文 CJK 字体加载、点击穿透交互态切换、描边绘制。
    /// 统一科技感配色（SPEC 9：暗底 #0B0E14 / 科技青 #22D3EE / 琥珀 #FFB000）。
    /// </summary>
    public static class UIHelper
    {
        public static readonly Color C_Bg    = new Color(0.043f, 0.055f, 0.078f, 0.82f);
        public static readonly Color C_Panel = new Color(0.043f, 0.055f, 0.078f, 0.90f);
        public static readonly Color C_Cyan  = new Color(0.133f, 0.827f, 0.933f);
        public static readonly Color C_Amber = new Color(1f, 0.690f, 0f);
        public static readonly Color C_Text  = new Color(0.86f, 0.91f, 0.96f);
        public static readonly Color C_Green = new Color(0.30f, 0.90f, 0.45f);
        public static readonly Color C_Red   = new Color(0.95f, 0.32f, 0.32f);
        public static readonly Color C_Dim   = new Color(0.16f, 0.20f, 0.26f, 0.95f);
        public static readonly Color C_Muted = new Color(0.46f, 0.54f, 0.62f); // 次要文字（提示/未解锁），保证暗底可读

        private static Texture2D _white;
        public static Texture2D WhiteTex
        {
            get
            {
                if (_white == null)
                {
                    _white = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    _white.SetPixel(0, 0, Color.white);
                    _white.Apply();
                }
                return _white;
            }
        }

        public static void DrawRect(Rect r, Color c)
        {
            Color prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, WhiteTex);
            GUI.color = prev;
        }

        public static void DrawBorder(float x, float y, float w, float h, Color c, float t = 1.5f)
        {
            DrawRect(new Rect(x, y, w, t), c);
            DrawRect(new Rect(x, y + h - t, w, t), c);
            DrawRect(new Rect(x, y, t, h), c);
            DrawRect(new Rect(x + w - t, y, t, h), c);
        }

        /// <summary>UI 全局缩放：以约 800px 屏高为基准，随分辨率等比放大/缩小，避免小屏 UI 过小。</summary>
        public static float Scale => Mathf.Clamp((float)Screen.height / 800f, 0.75f, 2.2f);
        public static float S(float v) => v * Scale;
        public static Rect R(float x, float y, float w, float h)
        {
            float k = Scale;
            return new Rect(x * k, y * k, w * k, h * k);
        }

        private static Font _cjk;
        /// <summary>
        /// 中文 CJK 字体。加载优先级：
        /// 1) 工程内置资源 <see cref="Resources"/>/Fonts/msyh（打包内嵌，跨平台生效，最稳）；
        /// 2) 反射调用老 API Font.CreateDynamicFontFromOSFile（仅旧 Unity 版本存在，2022.3 已移除，会走到 null 分支）；
        /// 3) 都为 null 时回退 Unity 默认字体（中文会显示为缺字框，但不影响编译与运行）。
        /// </summary>
        public static Font CjkFont
        {
            get
            {
                if (_cjk != null) return _cjk;
                // 1) 内置资源优先
                _cjk = Resources.Load<Font>("Fonts/msyh");
                if (_cjk != null) return _cjk;
                // 2) 反射兜底老 API（编译期不依赖该符号，避免 CS0117）
                var m = typeof(Font).GetMethod("CreateDynamicFontFromOSFile", new[] { typeof(string) });
                if (m != null)
                {
                    string p = "C:/Windows/Fonts/msyh.ttc";
                    if (File.Exists(p))
                    {
                        try { _cjk = m.Invoke(null, new object[] { p }) as Font; }
                        catch (System.Exception) { _cjk = null; }
                    }
                }
                return _cjk;
            }
        }

        public static GUIStyle NewLabelStyle(int size, Color color, TextAnchor anchor = TextAnchor.UpperLeft, bool bold = false)
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontSize = size;
            s.normal.textColor = color;
            s.alignment = anchor;
            s.wordWrap = false;
            if (CjkFont != null) s.font = CjkFont;
            return s;
        }

        public static GUIStyle NewButtonStyle(int size, Color color)
        {
            var s = new GUIStyle(GUI.skin.button);
            s.fontSize = size;
            s.normal.textColor = color;
            s.alignment = TextAnchor.MiddleCenter;
            if (CjkFont != null) s.font = CjkFont;
            return s;
        }

        /// <summary>
        /// 交互态切换点击穿透（引用计数，支持多个面板同时开/关互不干扰）。
        /// interactive=true：计数 +1，关闭穿透（可点 UI）；
        /// interactive=false：计数 -1，归零时恢复桌面宠物穿透态并把窗口发往底层（仅桌面背景之上）。
        /// </summary>
        private static int _interactiveCount;
        public static void SetInteractive(bool interactive)
        {
#if UNITY_STANDALONE_WIN
            var tw = TransparentWindow.Instance;
            if (tw == null) return;
            if (interactive)
            {
                _interactiveCount++;
                tw.SetClickThrough(false);
                tw.BringToFront();
            }
            else
            {
                _interactiveCount = Mathf.Max(0, _interactiveCount - 1);
                if (_interactiveCount == 0)
                {
                    tw.SetClickThrough(true);
                    tw.SendToBottom();
                }
            }
#endif
        }
    }
}
