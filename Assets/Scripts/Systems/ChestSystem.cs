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
        // 收紧：2km → 4km，降低物资获取速度（Batch 8 反馈）。
        private const float DefaultMileageBase = 3f;

        // 遇怪伤害区间（三需求各扣 10–20 点，钳制 ≥1 不致死）
        private const float MonsterDamageMin = 10f;
        private const float MonsterDamageMax = 20f;

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

            int count = 1 + rng.NextInt(2); // 1..2 种（收紧：原来 1..3）
            var result = new ChestOpenResult
            {
                type = type,
                rewards = new List<ResourceDelta>(),
                blueprintFragments = 0,
                monsterEncounter = false,
            };

            for (int i = 0; i < count; i++)
            {
                string res = PickWeightedResource(rng, cfg);
                int amt = 1 + rng.NextInt(2); // 1..2 单位（收紧：原来 1..4）
                _inventory.Add(state, res, amt);
                result.rewards.Add(new ResourceDelta { resource = res, delta = amt });
            }

            if (type == ChestType.Dungeon)
                result.blueprintFragments = 1 + rng.NextInt(3);

            // 副本宝箱掉落的蓝图碎片需持久化到 GameState，供 VehicleSystem 改装消耗（SPEC 5.7）
            if (type == ChestType.Dungeon)
                state.blueprintFragments += result.blueprintFragments;

            // 开箱遇怪：区域基础概率 × 天气系数；扣三需求，钳制 ≥1（硬约束：不致死，只会进虚弱）
            float monsterChance = MonsterChance(state, cfg);
            if (rng.NextFloat() < monsterChance)
            {
                result.monsterEncounter = true;
                state.hunger = System.Math.Max(1f, state.hunger - (MonsterDamageMin + rng.NextFloat() * (MonsterDamageMax - MonsterDamageMin)));
                state.thirst = System.Math.Max(1f, state.thirst - (MonsterDamageMin + rng.NextFloat() * (MonsterDamageMax - MonsterDamageMin)));
                state.health = System.Math.Max(1f, state.health - (MonsterDamageMin + rng.NextFloat() * (MonsterDamageMax - MonsterDamageMin)));
            }

            state.rngSeed = rng.State; // 持久化随机状态
            GameEvents.RaiseChestOpened(result);
            return result;
        }

        /// <summary>开箱遇怪概率 = 当前区域基础概率 × 当前天气系数（旧配置资产缺字段时按 0 处理，即不遇怪）。</summary>
        private float MonsterChance(GameState state, GameConfig cfg)
        {
            if (cfg == null) return 0f;
            int idx = state.currentZoneIndex;
            ZoneConfig zone = (idx >= 0 && idx < cfg.zones.Count) ? cfg.zones[idx] : null;
            if (zone == null) return 0f;
            var wc = cfg.weathers.FirstOrDefault(w => w.id == state.currentWeather);
            float weatherMult = (wc != null) ? wc.monsterChanceMultiplier : 1f;
            return zone.monsterChancePerChest * weatherMult;
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
