using UnityEngine;
using DesktopDoomsday.Core;

namespace DesktopDoomsday.Core
{
    /// <summary>
    /// 入口。挂到 Main.unity 唯一 GameObject。
    /// 职责：加载存档或新建 GameState、注册服务、连接 GameLoop、退出时保存。
    /// 开局选车面板（VehicleSystem）在后续 Batch 接入。
    /// </summary>
    public class Bootstrapper : MonoBehaviour
    {
        private GameState _state;
        private SaveSystem _save;

        private void Awake()
        {
            _save = new SaveSystem();
            ServiceLocator.Register(_save);

            var data = _save.Load();
            if (data != null && data.state != null)
            {
                _state = data.state;
                float offline = _save.ComputeOfflineSeconds(_state.lastSaveUnixMs);
                if (offline > 0f)
                {
                    // 离线结算在 Batch 5 接入；此处仅记录。
                    Debug.Log($"[Bootstrapper] Offline seconds pending: {offline}");
                }
            }
            else
            {
                _state = new GameState();
            }
            ServiceLocator.Register(_state);

            var loop = FindObjectOfType<GameLoop>();
            if (loop != null)
                GameLoop.OnTick += TickAll;

            ConfigureRenderAndFrameRate();
        }

        /// <summary>
        /// SPEC 2.2 的渲染/帧率前置设置。
        /// - targetFrameRate=30（静止挂机可调到 10 省 CPU），并关闭 vSync 让帧上限生效。
        /// - 主相机 Solid Color + 背景 Alpha=0：透明窗口前置，Batch 5 接 Win32 分层后才真正透出桌面。
        /// 这些都是代码 API，Unity 设置面板里没有对应项，故在此统一初始化。
        /// </summary>
        private void ConfigureRenderAndFrameRate()
        {
            Application.targetFrameRate = 30;
            QualitySettings.vSyncCount = 0;

            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            }
        }

        private void TickAll(float dt)
        {
            _state.playTimeSeconds += dt;
            // 各 System 自行订阅 GameLoop.OnTick 实现逻辑（Batch 2+）。
        }

        private void OnApplicationQuit()
        {
            if (_save != null && _state != null)
                _save.Save(_state);
        }
    }
}
