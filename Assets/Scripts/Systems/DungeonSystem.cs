using System.Collections.Generic;
using System.Linq;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;

namespace DesktopDoomsday.Systems
{
    /// <summary>
    /// 副本系统（SPEC 5.6）。Batch 4：先实现数据层——触发、超时忽略、分支结算。面板 UI 留 Batch 6。
    /// 生命周期：
    ///   1) 冷却中 dungeonCooldownRemaining>0 → 每 tick 按真实 dt 递减；
    ///   2) 冷却结束 → 每 tick 按 triggerChancePerTick 概率生成待处理副本（从当前区域 dungeonPool 等权抽取）；
    ///   3) 待处理 pendingDungeon 存在 → 倒计时 pendingDungeonTimeout（默认 20s），超时自动 IgnorePending；
    ///   4) 玩家 Complete（选分支）或 IgnorePending（超时/主动）→ 重置冷却，公路照常。
    /// 不暂停公路（PauseRoadDuringDungeon 为 Batch 6 设置项，默认 false），不夺取焦点。
    /// 所有随机走确定性 RNG（GameState.rngSeed），读档可复现（SPEC 验收 10）。
    /// </summary>
    public class DungeonSystem : IDungeonSystem
    {
        private readonly InventorySystem _inventory = new InventorySystem();
        private const float DefaultPendingTimeout = 20f;
        private const float BaseTickSeconds = 1f;

        /// <summary>每 tick 驱动：推进冷却/超时，并按概率触发。替代 SPEC 接口中 TryTrigger 的计时职责。</summary>
        public void Tick(GameState state, float deltaSeconds)
        {
            if (deltaSeconds <= 0f) return;

            // 待处理副本：倒计时超时自动忽略（视为忽略，公路照常）
            if (state.pendingDungeon.HasValue)
            {
                state.pendingDungeonTimeout -= deltaSeconds;
                if (state.pendingDungeonTimeout <= 0f)
                    IgnorePending(state);
                return;
            }

            // 冷却中：按真实时间递减（后台 5s tick 也按真实秒数，保证超时窗口准确）
            if (state.dungeonCooldownRemaining > 0f)
            {
                state.dungeonCooldownRemaining -= deltaSeconds;
                if (state.dungeonCooldownRemaining < 0f) state.dungeonCooldownRemaining = 0f;
                return;
            }

            // 冷却结束：尝试触发
            TryTrigger(state);
        }

        /// <summary>SPEC 接口。在冷却结束且无非待处理副本时，按概率生成待处理副本。返回是否新触发。</summary>
        public bool TryTrigger(GameState state)
        {
            if (state.pendingDungeon.HasValue) return false;
            if (state.dungeonCooldownRemaining > 0f) return false;

            var cfg = GameConfig.Instance;
            if (cfg == null) return false;
            var pool = CurrentZonePool(state, cfg);
            if (pool.Count == 0) return false;

            // AutoIgnore：保持冷却循环，不生成提示（Batch 6 设置项，默认 false）
            if (state.autoIgnoreDungeons)
            {
                state.dungeonCooldownRemaining = FirstPoolDef(state, cfg)?.triggerCooldownSeconds ?? 120f;
                return false;
            }

            var firstDef = FirstPoolDef(state, cfg);
            if (firstDef == null) return false;

            var rng = new DeterministicRandom(state.rngSeed);
            bool hit = rng.NextFloat() < firstDef.triggerChancePerTick;
            if (!hit) { state.rngSeed = rng.State; return false; }

            var id = pool[rng.NextInt(pool.Count)];
            state.rngSeed = rng.State;

            state.pendingDungeon = id;
            state.pendingDungeonTimeout = DefaultPendingTimeout;
            GameEvents.RaiseDungeonAvailable(id);
            return true;
        }

        /// <summary>结算分支奖励。校验 pending 必须匹配 id；不匹配返回空结果（不发放，不重置）。</summary>
        public DungeonResult Complete(DungeonId id, int chosenBranchIndex, GameState state)
        {
            var empty = new DungeonResult { id = id, rewards = new List<ResourceDelta>(), blueprintFragments = 0 };
            if (!state.pendingDungeon.HasValue || state.pendingDungeon.Value != id)
                return empty;

            var cfg = GameConfig.Instance;
            var def = cfg?.dungeons.FirstOrDefault(d => d.id == id);
            if (def == null || def.branches.Count == 0)
            {
                ResetAfterResolve(state, def);
                return empty;
            }

            int branchIndex = (chosenBranchIndex >= 0 && chosenBranchIndex < def.branches.Count) ? chosenBranchIndex : 0;
            var branch = def.branches[branchIndex];

            var result = new DungeonResult { id = id, rewards = new List<ResourceDelta>(), blueprintFragments = 0 };
            int totalUnits = ComputeRewardUnits(state, def, branch);
            DistributeRewards(state, cfg, totalUnits, result);

            if (branch.blueprintFragmentReward > 0)
            {
                state.blueprintFragments += branch.blueprintFragmentReward;
                result.blueprintFragments = branch.blueprintFragmentReward;
            }

            ResetAfterResolve(state, def);
            GameEvents.RaiseDungeonCompleted(result);
            return result;
        }

        /// <summary>主动/超时忽略：清除待处理并重置冷却。</summary>
        public void IgnorePending(GameState state)
        {
            var def = CurrentPendingDef(state);
            state.pendingDungeon = null;
            state.pendingDungeonTimeout = 0f;
            ResetAfterResolve(state, def);
        }

        // ---- 调试用：强制生成当前区域一个待处理副本（忽略概率/冷却），便于无面板时验收（DebugHud 调用）----
        public bool ForceTrigger(GameState state)
        {
            if (state.pendingDungeon.HasValue) return false;
            var cfg = GameConfig.Instance;
            if (cfg == null) return false;
            var pool = CurrentZonePool(state, cfg);
            if (pool.Count == 0) return false;
            var rng = new DeterministicRandom(state.rngSeed);
            var id = pool[rng.NextInt(pool.Count)];
            state.rngSeed = rng.State;
            state.pendingDungeon = id;
            state.pendingDungeonTimeout = DefaultPendingTimeout;
            GameEvents.RaiseDungeonAvailable(id);
            return true;
        }

        // ---------- 内部辅助 ----------

        private List<DungeonId> CurrentZonePool(GameState state, GameConfig cfg)
        {
            int idx = state.currentZoneIndex;
            if (idx < 0) idx = 0;
            ZoneConfig zone = (idx < cfg.zones.Count) ? cfg.zones[idx] : cfg.zones[cfg.zones.Count - 1];
            return (zone != null && zone.dungeonPool != null) ? zone.dungeonPool : new List<DungeonId>();
        }

        private DungeonConfig FirstPoolDef(GameState state, GameConfig cfg)
        {
            var pool = CurrentZonePool(state, cfg);
            return pool.Count > 0 ? cfg.dungeons.FirstOrDefault(d => d.id == pool[0]) : null;
        }

        private DungeonConfig CurrentPendingDef(GameState state)
        {
            if (!state.pendingDungeon.HasValue) return null;
            var cfg = GameConfig.Instance;
            return cfg?.dungeons.FirstOrDefault(d => d.id == state.pendingDungeon.Value);
        }

        private void ResetAfterResolve(GameState state, DungeonConfig def)
        {
            state.pendingDungeon = null;
            state.pendingDungeonTimeout = 0f;
            state.dungeonCooldownRemaining = def != null ? def.triggerCooldownSeconds : 120f;
        }

        /// <summary>基础挂机每 tick 期望资源单位数（宝箱概率 × 单次平均掉落 2 单位），对齐 ChestSystem 的概率模型。</summary>
        private float BaseChestUnitsPerTick(GameState state)
        {
            var cfg = GameConfig.Instance;
            if (cfg == null || cfg.resources == null) return 0.1f;
            float chance = cfg.resources.baseChestChance
                * (1f + WeatherChestBonus(state))
                * (1f + VehicleChestModifier(state));
            return chance * 2f; // 单次宝箱期望掉落 1–3，均值 2
        }

        private int ComputeRewardUnits(GameState state, DungeonConfig def, DungeonBranch branch)
        {
            float idleUnits = BaseChestUnitsPerTick(state) * (def.durationSeconds / BaseTickSeconds);
            float total = idleUnits * branch.rewardMultiplier;
            int units = (int)(total + 0.5f);
            return units < 1 ? 1 : units;
        }

        private void DistributeRewards(GameState state, GameConfig cfg, int totalUnits, DungeonResult result)
        {
            if (totalUnits <= 0 || cfg == null) return;
            var rng = new DeterministicRandom(state.rngSeed);
            var counts = new Dictionary<string, int>();
            for (int i = 0; i < totalUnits; i++)
            {
                string res = PickWeightedResource(rng, cfg);
                if (!counts.ContainsKey(res)) counts[res] = 0;
                counts[res]++;
            }
            state.rngSeed = rng.State;

            foreach (var kv in counts)
            {
                _inventory.Add(state, kv.Key, kv.Value);
                result.rewards.Add(new ResourceDelta { resource = kv.Key, delta = kv.Value });
            }
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

        private string PickWeightedResource(DeterministicRandom rng, GameConfig cfg)
        {
            var weights = cfg?.resources?.dropWeights;
            if (weights == null || weights.Count == 0)
            {
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
