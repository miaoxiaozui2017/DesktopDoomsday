using System.Collections.Generic;
using DesktopDoomsday.Data;
using DesktopDoomsday.Systems;

namespace DesktopDoomsday.Core
{
    /// <summary>
    /// 离线收益结算（SPEC 5.8）：按基础速度推进里程，小人消耗以 0.5 倍速率模拟，不生成宝箱、不触发副本。
    /// 离线秒数由 SaveSystem.ComputeOfflineSeconds 计算（上限 8 小时，时间回拨归零）。
    /// 为避免事件洪泛，里程推进不逐 tick 触发 OnDistanceAdvanced（仅区域解锁仍触发 OnZoneChanged）；
    /// 里程/区域类成就在 Bootstrapper 调用 AchievementSystem.Check 时统一补判。
    /// </summary>
    public static class OfflineSimulator
    {
        /// <summary>最新离线报告，供 OfflineReportUI 在 Start 时读取展示；读取后由 UI 清空。</summary>
        public static OfflineReport Pending;

        public static OfflineReport Simulate(GameState state, float seconds)
        {
            var report = new OfflineReport
            {
                offlineSeconds = seconds,
                resourceChanges = new List<ResourceDelta>(),
                survivorState = state.survivorState,
                distanceGain = 0f
            };
            if (seconds <= 0f) return report;

            var road = new RoadSystem();
            var survivor = new SurvivorSystem();

            float startDist = state.totalDistanceKm;
            var startRes = Snapshot(state);

            // 以 5s 为步长模拟；小人消耗按 0.5 倍速率（SPEC 5.8）。
            float step = 5f;
            int steps = UnityEngine.Mathf.CeilToInt(seconds / step);
            float actual = seconds / steps;
            for (int i = 0; i < steps; i++)
            {
                float speed = road.GetCurrentSpeed(state, state.currentWeather, state.currentVehicle);
                state.currentSpeedKmPerTick = speed;
                float adv = speed * actual;
                state.totalDistanceKm += adv;
                state.distanceSinceLastChest += adv;
                survivor.Tick(state, actual * 0.5f); // 离线消耗 0.5x
            }

            // 区域解锁（沿路自然推进，触发 OnZoneChanged 供成就补判）
            int guard = 0;
            while (road.TryUnlockNextZone(state) && guard++ < 16) { }

            report.distanceGain = state.totalDistanceKm - startDist;
            var endRes = Snapshot(state);
            foreach (var kv in endRes)
                report.resourceChanges.Add(new ResourceDelta { resource = kv.Key, delta = kv.Value - startRes[kv.Key] });
            report.survivorState = state.survivorState;
            return report;
        }

        private static Dictionary<string, int> Snapshot(GameState s)
        {
            return new Dictionary<string, int>
            {
                { "food", s.food },
                { "water", s.water },
                { "medicine", s.medicine },
                { "fuel", s.fuel },
                { "parts", s.parts },
                { "blueprintFragments", s.blueprintFragments }
            };
        }
    }
}
