#if UNITY_STANDALONE_WIN
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using DesktopDoomsday.Core;

namespace DesktopDoomsday.Window
{
    /// <summary>
    /// 点击穿透管理（SPEC 5.9）：通过全局快捷键 Ctrl+Shift+D 切换窗口点击穿透。
    /// 使用低层键盘钩子（WH_KEYBOARD_LL），即使窗口处于穿透态（不接收鼠标）也能捕获快捷键。
    ///
    /// 行为：
    /// - 开穿透：WS_EX_TRANSPARENT 置位 → 鼠标穿透到桌面；同时 GameLoop.SetBackground(true) 把 tick 降到 5s。
    /// - 关穿透：移除 WS_EX_TRANSPARENT → UI 按钮可点击；GameLoop.SetBackground(false) 恢复正常 1s tick。
    /// </summary>
    public class ClickThroughManager : MonoBehaviour
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int VK_D = 0x44;
        private const int VK_LCONTROL = 0xA2;
        private const int VK_RCONTROL = 0xA3;
        private const int VK_LSHIFT = 0xA0;
        private const int VK_RSHIFT = 0xA1;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private LowLevelKeyboardProc _proc;
        private IntPtr _hookId = IntPtr.Zero;
        private GameLoop _loop;

        private void Awake()
        {
            _proc = HookProc;
        }

        private void Start()
        {
            _loop = FindObjectOfType<GameLoop>();
#if !UNITY_EDITOR
            InstallHook();
#else
            Debug.Log("[ClickThroughManager] 编辑器模式跳过键盘钩子（仅独立构建生效）。");
#endif
        }

        private void InstallHook()
        {
            try
            {
                IntPtr module = GetModuleHandle(null);
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, module, 0);
                if (_hookId == IntPtr.Zero)
                    Debug.LogWarning("[ClickThroughManager] 键盘钩子安装失败（穿透热键不可用，但不影响游戏）。");
                else
                    Debug.Log("[ClickThroughManager] Ctrl+Shift+D 点击穿透热键已就绪。");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ClickThroughManager] 安装钩子异常：{e}");
            }
        }

        private IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                var kb = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                if (kb.vkCode == VK_D)
                {
                    bool ctrl = (GetAsyncKeyState(VK_LCONTROL) & 0x8000) != 0 || (GetAsyncKeyState(VK_RCONTROL) & 0x8000) != 0;
                    bool shift = (GetAsyncKeyState(VK_LSHIFT) & 0x8000) != 0 || (GetAsyncKeyState(VK_RSHIFT) & 0x8000) != 0;
                    if (ctrl && shift)
                    {
                        Toggle();
                        return (IntPtr)1; // 吞掉该按键，避免输入到游戏
                    }
                }
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        /// <summary>切换点击穿透（也供托盘菜单调用）。</summary>
        public void Toggle()
        {
            if (TransparentWindow.Instance == null) return;
            bool next = !TransparentWindow.Instance.IsClickThrough;
            TransparentWindow.Instance.SetClickThrough(next);
            if (_loop != null) _loop.SetBackground(next); // 穿透时降频到 5s tick
            Debug.Log($"[ClickThroughManager] 点击穿透 {(next ? "开启（鼠标穿透桌面，tick=5s）" : "关闭（UI 可点击，tick=1s）")}");
        }

        private void OnDisable()
        {
            RemoveHook();
        }

        private void OnApplicationQuit()
        {
            RemoveHook();
        }

        private void RemoveHook()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }
    }
}
#endif
