using System.Collections.Generic;
using DesktopDoomsday.Data;
using DesktopDoomsday.Systems;

namespace DesktopDoomsday.Core
{
    /// <summary>
    /// 离线收益结算（SPEC 5.8）：按基础速度推进里程，小人消耗以 0.3 倍速率模拟，不生成宝箱、不触发副本。
    /// 离线期间有「路边拾荒」机制：每整小时 +4 食/+5 水/+2 药，抵消 0.3× 消耗后有少量正收益，离线回来物资不缺。
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

            // 以 5s 为步长模拟；小人消耗按 0.3 倍速率（SPEC 5.8，离线更轻松）。
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
                survivor.Tick(state, actual * 0.3f); // 离线消耗 0.3x
            }

            // 区域解锁（沿路自然推进，触发 OnZoneChanged 供成就补判）
            int guard = 0;
            while (road.TryUnlockNextZone(state) && guard++ < 16) { }

            // 离线零件涓流（Batch 8 反馈）：每 2 小时 +1、封顶 4，蓝图零产出——
            // 让升级改装主要依赖在线打副本，而不是挂离线白拿。Snapshot diff 会自动带进报告。
            int partsTrickle = (int)System.Math.Min(4f, seconds / 7200f);
            if (partsTrickle > 0) state.parts += partsTrickle;

            // 路边拾荒（Batch 8 反馈）：每整小时 +4 食/+5 水/+2 药，
            // 抵消 0.3× 离线消耗后有少量正收益，离线回来物资不缺。Snapshot diff 会自动带进报告。
            int hoursOffline = (int)System.Math.Floor(seconds / 3600f);
            if (hoursOffline > 0)
            {
                state.food += hoursOffline * 4;
                state.water += hoursOffline * 5;
                state.medicine += hoursOffline * 2;
            }

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
