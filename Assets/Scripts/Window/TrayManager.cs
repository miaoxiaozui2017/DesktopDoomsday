#if UNITY_STANDALONE_WIN
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DesktopDoomsday.Window
{
    /// <summary>
    /// 托盘管理（SPEC 5.10）：最小化时隐藏主窗口并显示托盘图标；托盘右键弹出菜单
    /// （显示/隐藏、穿透开关、退出）。所有 Win32 调用防御式包裹，任一失败不影响游戏运行。
    ///
    /// 实现要点：自建一个「消息专用窗口」(message-only window) 接收托盘回调，并在 Update 中手动泵消息，
    /// 因为 Unity 主循环不会代泵我们私有窗口的消息。守卫 #if UNITY_STANDALONE_WIN，且 HWND 操作仅在
    /// 独立构建下执行（编辑器跳过），保持开发期 Play 可用。
    /// </summary>
    public class TrayManager : MonoBehaviour
    {
        private const int WM_APP_TRAY = 0x8000 + 1;
        private const uint WM_RBUTTONUP = 0x0205;
        private const uint WM_LBUTTONDBLCLK = 0x0203;

        private const uint NIM_ADD = 0x00000000;
        private const uint NIM_DELETE = 0x00000002;
        private const uint NIM_MODIFY = 0x00000001;
        private const uint NIF_MESSAGE = 0x00000001;
        private const uint NIF_ICON = 0x00000002;
        private const uint NIF_TIP = 0x00000004;

        private const int ID_SHOW = 1001;
        private const int ID_HIDE = 1002;
        private const int ID_TOGGLE = 1003;
        private const int ID_EXIT = 1004;

        // ---- 结构与委托 ----
        private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct WNDCLASSEX
        {
            public int cbSize;
            public uint style;
            public WndProc lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct NOTIFYICONDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public int x;
            public int y;
        }

        // ---- P/Invoke ----
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName,
            uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu,
            IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

        [DllImport("user32.dll")]
        private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll")]
        private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

        [DllImport("user32.dll")]
        private static extern bool TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

        [DllImport("user32.dll")]
        private static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG lpMsg);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }

        private const uint MF_STRING = 0x00000000;
        private const uint MF_SEPARATOR = 0x00000800;
        private const uint TPM_BOTTOMALIGN = 0x0020;
        private const uint TPM_RIGHTALIGN = 0x0008;
        private const uint PM_REMOVE = 0x0001;
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int SW_RESTORE = 9;
        private const int SW_MINIMIZE = 6;
        private const int IDI_APPLICATION = 32512;
        private static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);

        // ---- 状态 ----
        private WndProc _wndProc;
        private IntPtr _trayHwnd;
        private IntPtr _unityHwnd;
        private NOTIFYICONDATA _nid;
        private bool _trayMinimized;

        private void Start()
        {
#if !UNITY_EDITOR
            try
            {
                _unityHwnd = FindWindow("UnityWndClass", null);
                if (_unityHwnd == IntPtr.Zero) _unityHwnd = FindWindow(null, Application.productName);
                CreateTrayWindow();
                AddTrayIcon();
                Debug.Log("[TrayManager] 托盘已就绪（右键图标打开菜单）。");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TrayManager] 初始化失败（游戏继续运行）：{e}");
            }
#else
            Debug.Log("[TrayManager] 编辑器模式跳过托盘（仅独立构建生效）。");
#endif
        }

        private void CreateTrayWindow()
        {
            _wndProc = TrayWndProc;
            var wc = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<WNDCLASSEX>(),
                style = 0,
                lpfnWndProc = _wndProc,
                hInstance = GetModuleHandle(null),
                lpszClassName = "DDTrayMessageWnd"
            };
            RegisterClassEx(ref wc);
            _trayHwnd = CreateWindowEx(0, "DDTrayMessageWnd", null, 0, 0, 0, 0, 0, HWND_MESSAGE, IntPtr.Zero, wc.hInstance, IntPtr.Zero);
        }

        private void AddTrayIcon()
        {
            _nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _trayHwnd,
                uID = 1,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = WM_APP_TRAY,
                hIcon = LoadIcon(IntPtr.Zero, new IntPtr(IDI_APPLICATION)),
                szTip = "Desktop Doomsday"
            };
            Shell_NotifyIcon(NIM_ADD, ref _nid);
        }

        private IntPtr TrayWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_APP_TRAY)
            {
                uint evt = (uint)(lParam.ToInt64() & 0xFFFF);
                if (evt == WM_RBUTTONUP)
                    ShowContextMenu();
                else if (evt == WM_LBUTTONDBLCLK)
                    ToggleMainWindow();
                return IntPtr.Zero;
            }
            if (msg == 0x0111) // WM_COMMAND
            {
                uint id = (uint)(wParam.ToInt64() & 0xFFFF);
                HandleMenu(id);
                return IntPtr.Zero;
            }
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private void ShowContextMenu()
        {
            IntPtr menu = CreatePopupMenu();
            AppendMenu(menu, MF_STRING, ID_SHOW, "显示窗口");
            AppendMenu(menu, MF_STRING, ID_HIDE, "隐藏窗口");
            AppendMenu(menu, MF_SEPARATOR, 0, null);
            AppendMenu(menu, MF_STRING, ID_TOGGLE, "切换点击穿透 (Ctrl+Shift+D)");
            AppendMenu(menu, MF_SEPARATOR, 0, null);
            AppendMenu(menu, MF_STRING, ID_EXIT, "退出");

            POINT pt = default;
            GetCursorPos(ref pt);
            SetForegroundWindow(_trayHwnd); // 让菜单能正确消失
            TrackPopupMenu(menu, TPM_BOTTOMALIGN | TPM_RIGHTALIGN, pt.x, pt.y, 0, _trayHwnd, IntPtr.Zero);
            PostMessage(_trayHwnd, 0, IntPtr.Zero, IntPtr.Zero); // WM_NULL，刷新菜单焦点
            DestroyMenu(menu);
        }

        private void HandleMenu(uint id)
        {
            switch (id)
            {
                case ID_SHOW: ShowMainWindow(); break;
                case ID_HIDE: HideMainWindow(); break;
                case ID_TOGGLE:
                    var ct = FindObjectOfType<ClickThroughManager>();
                    ct?.Toggle();
                    break;
                case ID_EXIT:
                    Application.Quit();
                    break;
            }
        }

        private void ToggleMainWindow()
        {
            if (_trayMinimized) ShowMainWindow(); else HideMainWindow();
        }

        private void ShowMainWindow()
        {
            if (_unityHwnd == IntPtr.Zero) return;
            ShowWindow(_unityHwnd, SW_RESTORE);
            ShowWindow(_unityHwnd, SW_SHOW);
            _trayMinimized = false;
        }

        private void HideMainWindow()
        {
            if (_unityHwnd == IntPtr.Zero) return;
            ShowWindow(_unityHwnd, SW_HIDE);
            _trayMinimized = true;
        }

        private void Update()
        {
#if !UNITY_EDITOR
            // 泵取托盘窗口消息（菜单/回调）
            if (_trayHwnd != IntPtr.Zero)
            {
                MSG msg;
                while (PeekMessage(out msg, _trayHwnd, 0, 0, PM_REMOVE))
                {
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }
            }
            // 自动最小化检测：玩家点了窗口最小化 → 隐藏并留在托盘
            if (_unityHwnd != IntPtr.Zero && !_trayMinimized && IsIconic(_unityHwnd))
            {
                HideMainWindow();
            }
#endif
        }

        private void OnApplicationQuit()
        {
#if !UNITY_EDITOR
            if (_nid.hWnd != IntPtr.Zero)
            {
                Shell_NotifyIcon(NIM_DELETE, ref _nid);
            }
            if (_trayHwnd != IntPtr.Zero)
            {
                DestroyWindow(_trayHwnd);
                _trayHwnd = IntPtr.Zero;
            }
#endif
        }
    }
}
#endif
