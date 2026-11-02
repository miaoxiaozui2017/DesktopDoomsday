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

        private static Font _cjk;
        /// <summary>
        /// 中文 CJK 字体（运行时从 Windows 系统字体加载）。无则回退到 Unity 默认字体（中文会显示为缺字框）。
        /// 仅 Windows 构建/编辑器生效（路径为 Windows 专属）。
        /// </summary>
        public static Font CjkFont
        {
            get
            {
                if (_cjk == null)
                {
#if UNITY_STANDALONE_WIN
                    string p = "C:/Windows/Fonts/msyh.ttc";
                    if (File.Exists(p))
                    {
                        try { _cjk = Font.CreateDynamicFontFromOSFile(p); }
                        catch (System.Exception) { _cjk = null; }
                    }
#endif
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

        /// <summary>交互态切换点击穿透：interactive=true 关闭穿透（可点 UI）；false 恢复桌面宠物穿透态。</summary>
        public static void SetInteractive(bool interactive)
        {
#if UNITY_STANDALONE_WIN
            var tw = TransparentWindow.Instance;
            if (tw != null) tw.SetClickThrough(!interactive);
#endif
        }
    }
}
