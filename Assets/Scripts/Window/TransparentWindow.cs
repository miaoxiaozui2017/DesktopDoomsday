#if UNITY_STANDALONE_WIN
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DesktopDoomsday.Window
{
    /// <summary>
    /// 桌面窗口层（SPEC 5.9）：将 Unity 窗口变为无边框、透明（桌面可见）、可置顶、可点击穿透的宠物窗。
    /// 只改窗口样式，不改场景绘制；与相机透明背景（Bootstrapper 已设 RGBA(0,0,0,0)）+ Pixel Perfect 共存，
    /// 透明区不渲染像素，因此不会出现黑边（SPEC 9.6）。
    ///
    /// 关键约束（架构红线 4）：OnApplicationQuit 必须恢复原始窗口样式，移除 WS_EX_LAYERED / WS_EX_TRANSPARENT，
    /// 否则进程残留半透明/穿透窗口。
    ///
    /// 守卫说明：HWND 操作仅在「Windows 独立构建」下执行（#if !UNITY_EDITOR）。编辑器 Play 模式不触碰
    /// Game 视图窗口，保持开发期可用。
    /// </summary>
    public class TransparentWindow : MonoBehaviour
    {
        public static TransparentWindow Instance { get; private set; }

        // Win32 常量
        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const uint WS_CAPTION = 0x00C00000;
        private const uint WS_THICKFRAME = 0x00040000;
        private const uint WS_MINIMIZEBOX = 0x00020000;
        private const uint WS_MAXIMIZEBOX = 0x00010000;
        private const uint WS_SYSMENU = 0x00080000;
        private const uint WS_EX_LAYERED = 0x00080000;
        private const uint WS_EX_TRANSPARENT = 0x00000020;
        private const uint WS_EX_TOPMOST = 0x00000008;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const int HWND_TOP = 0;
        private const int HWND_BOTTOM = 1;
        private const int HWND_TOPMOST = -1;
        private const int HWND_NOTOPMOST = -2;
        private const int SW_RESTORE = 9;

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight;
        }

        [DllImport("user32.dll", EntryPoint = "FindWindow", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLong64(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLong64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);
        private const uint LWA_ALPHA = 0x00000002;

        private IntPtr _hwnd;
        private IntPtr _originalExStyle;
        private bool _clickThrough;
        private bool _topmost;

        public bool IsClickThrough => _clickThrough;
        public bool IsTopmost => _topmost;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // SPEC 5.9：所有 Win32 调用必须延迟一帧，确保 Unity 已完成窗口创建。
#if !UNITY_EDITOR
            StartCoroutine(ApplyAfterFrame());
#else
            Debug.Log("[TransparentWindow] 编辑器模式跳过 HWND 操作（仅独立构建生效）。");
#endif
        }

        private System.Collections.IEnumerator ApplyAfterFrame()
        {
            yield return null; // 等待一帧，窗口已建立
            ApplyTransparency();
        }

        private void ApplyTransparency()
        {
            try
            {
                _hwnd = FindWindow("UnityWndClass", null);
                if (_hwnd == IntPtr.Zero) _hwnd = GetActiveWindow();
                if (_hwnd == IntPtr.Zero)
                {
                    Debug.LogWarning("[TransparentWindow] 找不到 Unity 主窗口 HWND，跳过透明化。");
                    return;
                }

                // 1) 无边框：去掉标题栏/可调边框/最小最大化按钮，保留系统菜单（Alt+F4 可用）
                IntPtr style = GetWindowLong(_hwnd, GWL_STYLE);
                style = new IntPtr(style.ToInt64() & ~(long)(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX));
                style = new IntPtr(style.ToInt64() | (long)WS_SYSMENU);
                SetWindowLong(_hwnd, GWL_STYLE, style);

                // 2) 分层（透明前提）；同时默认开启点击穿透（桌面宠物模式：全屏透明窗不挡桌面操作）。
                //    是否「始终置顶」由 GameConfig.windowTopmost 决定（默认 false = 仅置于桌面背景之上，不遮挡其它应用）。
                _originalExStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
                _topmost = (GameConfig.Instance != null) ? GameConfig.Instance.windowTopmost : false;
                IntPtr exStyle = new IntPtr(_originalExStyle.ToInt64() | (long)(WS_EX_LAYERED | WS_EX_TRANSPARENT));
                if (_topmost) exStyle = new IntPtr(exStyle.ToInt64() | (long)WS_EX_TOPMOST);
                SetWindowLong(_hwnd, GWL_EXSTYLE, exStyle);
                _clickThrough = true;

                // 3) 分层窗口全不透明（默认），透明靠 DWM 玻璃扩展 + 相机 alpha
                SetLayeredWindowAttributes(_hwnd, 0, 255, LWA_ALPHA);

                // 4) DWM 把客户区扩展为玻璃：相机渲染 alpha=0 处显示桌面（SPEC 2.2 + 9.6）
                var margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
                DwmExtendFrameIntoClientArea(_hwnd, ref margins);

                // 5) 应用样式变更。置顶模式 → HWND_TOPMOST；否则 → HWND_BOTTOM（仅位于桌面背景之上，正常应用覆盖它）
                SetWindowPos(_hwnd, new IntPtr(_topmost ? HWND_TOPMOST : HWND_BOTTOM), 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_FRAMECHANGED);

                Debug.Log($"[TransparentWindow] 透明窗口已应用（置顶={_topmost}，穿透={_clickThrough}）。");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TransparentWindow] 应用透明窗口失败：{e}");
            }
        }

        /// <summary>切换点击穿透：on=true 时 WS_EX_TRANSPARENT，鼠标穿透到桌面；false 时恢复可交互。</summary>
        public void SetClickThrough(bool on)
        {
            _clickThrough = on;
#if !UNITY_EDITOR
            if (_hwnd == IntPtr.Zero) return;
            try
            {
                IntPtr ex = GetWindowLong(_hwnd, GWL_EXSTYLE);
                long v = ex.ToInt64();
                if (on) v |= (long)WS_EX_TRANSPARENT;
                else v &= ~(long)WS_EX_TRANSPARENT;
                SetWindowLong(_hwnd, GWL_EXSTYLE, new IntPtr(v));
            }
            catch (Exception e)
            {
                Debug.LogError($"[TransparentWindow] 设置点击穿透失败：{e}");
            }
#endif
        }

        /// <summary>运行时切换置顶：on=true 始终置顶；false 回到「仅置于桌面背景之上」（发往 HWND_BOTTOM）。</summary>
        public void SetTopmost(bool on)
        {
            _topmost = on;
#if !UNITY_EDITOR
            if (_hwnd == IntPtr.Zero) return;
            try
            {
                IntPtr ex = GetWindowLong(_hwnd, GWL_EXSTYLE);
                long v = ex.ToInt64();
                if (on) v |= (long)WS_EX_TOPMOST;
                else v &= ~(long)WS_EX_TOPMOST;
                SetWindowLong(_hwnd, GWL_EXSTYLE, new IntPtr(v));
                SetWindowPos(_hwnd, new IntPtr(on ? HWND_TOPMOST : HWND_BOTTOM), 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_FRAMECHANGED);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TransparentWindow] 设置置顶失败：{e}");
            }
#endif
        }

        /// <summary>把窗口发往底层（仅位于桌面背景之上，被正常应用窗口覆盖）。仅非置顶模式下有效。</summary>
        public void SendToBottom()
        {
#if !UNITY_EDITOR
            if (_hwnd == IntPtr.Zero || _topmost) return;
            try
            {
                SetWindowPos(_hwnd, new IntPtr(HWND_BOTTOM), 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_FRAMECHANGED);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TransparentWindow] 发送到底层失败：{e}");
            }
#endif
        }

        /// <summary>交互时把窗口提到前台（HWND_TOP，不置顶），便于点击面板按钮；关闭交互后由 SendToBottom 复位。</summary>
        public void BringToFront()
        {
#if !UNITY_EDITOR
            if (_hwnd == IntPtr.Zero) return;
            try
            {
                SetWindowPos(_hwnd, new IntPtr(HWND_TOP), 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_FRAMECHANGED);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TransparentWindow] 提到前台失败：{e}");
            }
#endif
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>激活窗口（置前台并恢复，若被最小化），使键盘快捷键（I/A/H/T）能收到输入；配合 BringToFront 使用。</summary>
        public void Activate()
        {
#if !UNITY_EDITOR
            if (_hwnd == IntPtr.Zero) return;
            try
            {
                ShowWindow(_hwnd, SW_RESTORE);
                SetForegroundWindow(_hwnd);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TransparentWindow] 激活窗口失败：{e}");
            }
#endif
        }

        /// <summary>架构红线 4：退出时恢复原始窗口样式，移除分层/穿透/置顶，避免残留。</summary>
        public void Restore()
        {
#if !UNITY_EDITOR
            if (_hwnd == IntPtr.Zero) return;
            try
            {
                IntPtr ex = _originalExStyle != IntPtr.Zero ? _originalExStyle : GetWindowLong(_hwnd, GWL_EXSTYLE);
                long v = ex.ToInt64() & ~(long)(WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOPMOST);
                SetWindowLong(_hwnd, GWL_EXSTYLE, new IntPtr(v));
                SetWindowPos(_hwnd, new IntPtr(HWND_NOTOPMOST), 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_FRAMECHANGED);
                Debug.Log("[TransparentWindow] 窗口样式已恢复。");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TransparentWindow] 恢复窗口样式失败：{e}");
            }
#endif
        }

        private void OnApplicationQuit()
        {
            Restore();
        }

        // 32/64 位兼容的 Get/SetWindowLong 包装
        private static IntPtr GetWindowLong(IntPtr hWnd, int nIndex)
            => IntPtr.Size == 8 ? GetWindowLong64(hWnd, nIndex) : GetWindowLong32(hWnd, nIndex);

        private static void SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8) SetWindowLong64(hWnd, nIndex, dwNewLong);
            else SetWindowLong32(hWnd, nIndex, dwNewLong);
        }
    }
}
#endif
