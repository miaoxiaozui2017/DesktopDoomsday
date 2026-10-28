using System.Collections.Generic;
using System.Linq;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;

namespace DesktopDoomsday.Systems
{
    /// <summary>
    /// 宝箱系统（SPEC 5.3）。
    /// 里程宝箱：distanceSinceLastChest 超过阈值自动开；随机宝箱：每 tick 概率判定。
    /// 掉落按 ResourceConfig 权重，1–3 种物资；副本宝箱额外掉蓝图碎片。
    /// 所有随机走确定性 RNG（GameState.rngSeed），读档可复现（验收项 10）。
    /// </summary>
    public class ChestSystem : IChestSystem
    {
        private readonly InventorySystem _inventory = new InventorySystem();

        // 兜底：里程宝箱基础阈值（km）；车辆/区域可在配置中覆盖（Batch 3 接入区域表）。
        private const float DefaultMileageBase = 2f;

        public void Tick(GameState state, float deltaSeconds)
        {
            if (state.nextChestThresholdKm <= 0f)
                state.nextChestThresholdKm = PickMileageThreshold(state);

            // 里程宝箱
            if (state.distanceSinceLastChest >= state.nextChestThresholdKm)
            {
                Open(ChestType.Mileage, state);
                state.distanceSinceLastChest = 0f;
                state.nextChestThresholdKm = PickMileageThreshold(state);
            }

            // 随机宝箱：baseChance × (1 + 天气加成) × (1 + 车辆加成)
            float chance = BaseChestChance(state) * (1f + WeatherChestBonus(state)) * (1f + VehicleChestModifier(state));
            var rng = new DeterministicRandom(state.rngSeed);
            if (rng.NextFloat() < chance)
                Open(ChestType.Random, state);
            state.rngSeed = rng.State;
        }

        public ChestOpenResult Open(ChestType type, GameState state)
        {
            var cfg = GameConfig.Instance;
            var rng = new DeterministicRandom(state.rngSeed);

            int count = 1 + rng.NextInt(3); // 1..3 种
            var result = new ChestOpenResult
            {
                type = type,
                rewards = new List<ResourceDelta>(),
                blueprintFragments = 0,
            };

            for (int i = 0; i < count; i++)
            {
                string res = PickWeightedResource(rng, cfg);
                int amt = 1 + rng.NextInt(4); // 1..4
                _inventory.Add(state, res, amt);
                result.rewards.Add(new ResourceDelta { resource = res, delta = amt });
            }

            if (type == ChestType.Dungeon)
                result.blueprintFragments = 1 + rng.NextInt(3);

            state.rngSeed = rng.State; // 持久化随机状态
            GameEvents.RaiseChestOpened(result);
            return result;
        }

        private float BaseChestChance(GameState state)
        {
            var cfg = GameConfig.Instance;
            return (cfg != null && cfg.resources != null) ? cfg.resources.baseChestChance : 0.05f;
        }

        private float WeatherChestBonus(GameState state)
        {
            var cfg = GameConfig.Instance;
            if (cfg == null) return 0f;
            var wc = cfg.weathers.FirstOrDefault(w => w.id == state.currentWeather);
            return wc != null ? wc.chestChanceBonus : 0f;
        }

        private float VehicleChestModifier(GameState state)
        {
            var cfg = GameConfig.Instance;
            if (cfg == null) return 0f;
            var vc = cfg.vehicles.FirstOrDefault(v => v.id == state.currentVehicle);
            return vc != null ? vc.chestChanceModifier : 0f;
        }

        private float PickMileageThreshold(GameState state)
        {
            float baseTh = DefaultMileageBase;
            // 摩托特性：里程宝箱阈值 -10%（SPEC 5.7）
            if (state.currentVehicle == VehicleId.Motorcycle)
                baseTh *= 0.9f;

            var rng = new DeterministicRandom(state.rngSeed);
            float jitter = 0.8f + rng.NextFloat() * 0.4f; // 0.8–1.2
            state.rngSeed = rng.State;
            return baseTh * jitter;
        }

        private string PickWeightedResource(DeterministicRandom rng, GameConfig cfg)
        {
            var weights = cfg?.resources?.dropWeights;
            if (weights == null || weights.Count == 0)
            {
                // 兜底：五种等权
                var all = new[] { "food", "water", "medicine", "fuel", "parts" };
                return all[rng.NextInt(all.Length)];
            }
            float total = 0f;
            foreach (var w in weights) total += w.weight;
            float roll = rng.NextFloat() * total;
            foreach (var w in weights)
            {
                roll -= w.weight;
                if (roll <= 0f) return w.resource;
            }
            return weights[weights.Count - 1].resource;
        }
    }
}
