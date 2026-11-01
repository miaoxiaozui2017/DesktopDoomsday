using System.Collections.Generic;
using UnityEngine;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;

namespace DesktopDoomsday.Systems
{
    /// <summary>
    /// 成就系统（SPEC Batch 7）。订阅 GameEvents 统计里程/宝箱/副本/区域/物资，对照 GameConfig.achievements
    /// 解锁并写入 GameState.unlockedAchievementIds（随存档持久化）。解锁即触发 OnAchievementUnlocked 供 UI 提示。
    /// 纯行为系统，不持有 UI；确定性由 GameState 保证。
    /// </summary>
    public class AchievementSystem : IAchievementSystem
    {
        private GameState _state;
        private int _chestsOpened;
        private int _dungeonsCompleted;

        /// <summary>订阅事件。必须在 GameState 加载完成后调用一次。</summary>
        public void Init(GameState state)
        {
            _state = state;
            GameEvents.OnDistanceAdvanced += _ => Check(true);
            GameEvents.OnChestOpened += _ => { _chestsOpened++; Check(true); };
            GameEvents.OnDungeonCompleted += _ => { _dungeonsCompleted++; Check(true); };
            GameEvents.OnZoneChanged += _ => Check(true);
            GameEvents.OnInventoryChanged += _ => Check(true);

            // 加载旧档时补判已满足但此前未记录的成就（不弹提示，避免首启轰炸）。
            Check(announce: false);
        }

        /// <summary>离线结算后由 Bootstrapper 调用，补判里程/区域跳跃带来的解锁。</summary>
        public void Check(GameState state) => Check(announce: true);

        public void Tick(GameState state, float deltaSeconds) { /* 成就为事件驱动，无需每 tick */ }

        private void Check(bool announce)
        {
            var s = _state;
            if (s == null) return;
            var cfg = GameConfig.Instance;
            if (cfg == null || cfg.achievements == null) return;

            foreach (var a in cfg.achievements)
            {
                if (s.unlockedAchievementIds.Contains(a.id)) continue;
                if (!IsMet(a, s)) continue;
                s.unlockedAchievementIds.Add(a.id);
                GameEvents.RaiseAchievementUnlocked(a.id);
                if (announce)
                    UnityEngine.Debug.Log($"[Achievement] 解锁：{a.id}");
            }
        }

        private bool IsMet(AchievementConfig a, GameState s)
        {
            switch (a.conditionType)
            {
                case "distance":
                    return s.totalDistanceKm >= a.parameter;
                case "chestsOpened":
                    return _chestsOpened >= a.parameter;
                case "dungeons":
                    return _dungeonsCompleted >= a.parameter;
                case "zones":
                    return (s.currentZoneIndex + 1) >= a.parameter;
                case "resources":
                    return (s.food + s.water + s.medicine + s.fuel + s.parts) >= a.parameter;
                default:
                    return false;
            }
        }
    }
}
