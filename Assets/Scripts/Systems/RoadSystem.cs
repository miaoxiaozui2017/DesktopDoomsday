using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;

namespace DesktopDoomsday.Systems
{
    /// <summary>
    /// 公路推进系统（SPEC 5.2）。
    /// 每 tick：距离 = 速度 × 天气修正 × 小人修正 × deltaSeconds。
    /// 速度仅由配置与状态推导，系统本身不持有可变运行时数据（除写回 GameState）。
    /// </summary>
    public class RoadSystem : IRoadSystem
    {
        // 兜底默认基础速度（km/s）：当 GameConfig 未配置时使用，保证可运行与可测。
        private static readonly Dictionary<VehicleId, float> DefaultBaseSpeed = new Dictionary<VehicleId, float>
        {
            { VehicleId.Pickup, 0.05f },
            { VehicleId.Van, 0.04f },
            { VehicleId.Motorcycle, 0.07f },
        };

        public float GetCurrentSpeed(GameState state, WeatherId weather, VehicleId vehicle)
        {
            float baseSpeed = DefaultBaseSpeed.TryGetValue(vehicle, out var b) ? b : 0.05f;
            float speedBonus = 0f;

            var cfg = GameConfig.Instance;
            if (cfg != null)
            {
                var vc = cfg.vehicles.FirstOrDefault(v => v.id == vehicle);
                if (vc != null)
                {
                    baseSpeed = vc.baseSpeed;
                    int vIdx = (int)vehicle;
                    for (int t = 0; t < vc.upgradeTracks.Count && t < 2; t++)
                    {
                        int lvl = state.upgradeLevels[vIdx * 2 + t];
                        var bonuses = vc.upgradeTracks[t].speedBonusPerLevel;
                        if (lvl > 0 && bonuses != null && (lvl - 1) < bonuses.Length)
                            speedBonus += bonuses[lvl - 1];
                    }
                }
                var wc = cfg.weathers.FirstOrDefault(w => w.id == weather);
                if (wc != null) baseSpeed *= wc.speedMultiplier;
            }

            float speed = baseSpeed + speedBonus;
            speed *= SurvivorSpeedFactor(state); // 小人虚弱降速，不降为 0
            return speed;
        }

        public void Tick(GameState state, float deltaSeconds)
        {
            float speed = GetCurrentSpeed(state, state.currentWeather, state.currentVehicle);
            state.currentSpeedKmPerTick = speed;
            float advance = speed * deltaSeconds;
            state.totalDistanceKm += advance;
            state.distanceSinceLastChest += advance;
            GameEvents.RaiseDistanceAdvanced(state.totalDistanceKm);
            TryUnlockNextZone(state);
        }

        public float GetZoneProgress(GameState state)
        {
            var cfg = GameConfig.Instance;
            if (cfg == null || cfg.zones.Count == 0) return 0f;
            int idx = Mathf.Clamp(state.currentZoneIndex, 0, cfg.zones.Count - 1);
            float start = idx == 0 ? 0f : cfg.zones[idx - 1].unlockDistanceKm;
            float end = cfg.zones[idx].unlockDistanceKm;
            if (end <= start) return 0f;
            return Mathf.Clamp01((state.totalDistanceKm - start) / (end - start));
        }

        public bool TryUnlockNextZone(GameState state)
        {
            var cfg = GameConfig.Instance;
            if (cfg == null || cfg.zones.Count == 0) return false;
            if (state.currentZoneIndex >= cfg.zones.Count - 1) return false; // 已最后区：无限循环，不回退
            float next = cfg.zones[state.currentZoneIndex + 1].unlockDistanceKm;
            if (state.totalDistanceKm >= next)
            {
                state.currentZoneIndex++;
                GameEvents.RaiseZoneChanged(state.currentZoneIndex);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 小人速度修正：Normal = 1.0；Weakened 时按虚弱程度在 0.7–0.9 插值（不降为 0）。
        /// 虚弱程度由三需求均值决定：越枯竭越接近 0.7。
        /// </summary>
        private float SurvivorSpeedFactor(GameState state)
        {
            if (state.survivorState != SurvivorState.Weakened) return 1f;
            float avg = (state.hunger + state.thirst + state.health) / 3f;
            float t = Mathf.Clamp01(avg / 60f);
            return 0.7f + 0.2f * t;
        }
    }
}
