using System;
using UnityEngine;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;
using DesktopDoomsday.Systems;

namespace DesktopDoomsday.Core
{
    /// <summary>
    /// 入口。挂到 Main.unity 唯一 GameObject。
    /// 职责：加载存档或新建 GameState、注册服务、连接 GameLoop（Batch 2 起驱动各 System）、退出时保存。
    /// 开局选车面板（VehicleSystem）在 Batch 3 接入；天气/区域在 Batch 3 接入。
    /// </summary>
    public class Bootstrapper : MonoBehaviour
    {
        private GameState _state;
        private SaveSystem _save;
        private RoadSystem _road;
        private ChestSystem _chest;
        private SurvivorSystem _survivor;

        private void Awake()
        {
            _save = new SaveSystem();
            ServiceLocator.Register(_save);

            var data = _save.Load();
            bool isNew = (data == null || data.state == null);
            if (!isNew)
            {
                _state = data.state;
                float offline = _save.ComputeOfflineSeconds(_state.lastSaveUnixMs);
                if (offline > 0f)
                    Debug.Log($"[Bootstrapper] Offline seconds pending: {offline}");
            }
            else
            {
                _state = new GameState();
                InitNewGame(_state);
            }
            ServiceLocator.Register(_state);

            _road = new RoadSystem();
            _chest = new ChestSystem();
            _survivor = new SurvivorSystem();

            var loop = FindObjectOfType<GameLoop>();
            if (loop != null)
                GameLoop.OnTick += TickAll;

            ConfigureRenderAndFrameRate();
        }

        /// <summary>新开局初始化：确定性种子 + 初始物资 + 首个里程宝箱阈值。</summary>
        private void InitNewGame(GameState s)
        {
            // 首次种子用时间派生；之后全程由 rngSeed 驱动，读档可复现（SPEC 验收 10）。
            s.rngSeed = (int)((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() & 0x7FFFFFFF) | 1);

            var cfg = GameConfig.Instance;
            if (cfg != null && cfg.resources != null)
            {
                s.food = cfg.resources.initialFood;
                s.water = cfg.resources.initialWater;
                s.medicine = cfg.resources.initialMedicine;
                s.fuel = cfg.resources.initialFuel;
                s.parts = cfg.resources.initialParts;
            }
            else
            {
                // 兜底初始物资，保证首局可长时间维持 Normal（后续可在 GameConfig 调整）。
                s.food = 50; s.water = 50; s.medicine = 30; s.fuel = 100; s.parts = 20;
            }
            s.nextChestThresholdKm = 2f;
        }

        /// <summary>SPEC 2.2 渲染/帧率前置设置（代码 API，Unity 设置面板无对应项）。</summary>
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

        /// <summary>核心循环（SPEC 第 1 节）：公路 → 宝箱 → 小人需求。库存由系统内部调用。</summary>
        private void TickAll(float dt)
        {
            _state.playTimeSeconds += dt;
            _road.Tick(_state, dt);
            _chest.Tick(_state, dt);
            _survivor.Tick(_state, dt);
        }

        private void OnApplicationQuit()
        {
            if (_save != null && _state != null)
                _save.Save(_state);
        }
    }
}
