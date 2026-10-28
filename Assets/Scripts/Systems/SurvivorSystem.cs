using DesktopDoomsday.Core;
using DesktopDoomsday.Data;

namespace DesktopDoomsday.Systems
{
    /// <summary>
    /// 小人需求系统（SPEC 5.4）。不实现死亡（硬约束）。
    /// 每 tick 三需求（hunger/thirst/health）按速率下降；任一值 ≤ 0 时尝试用对应物资补货，
    /// 库存不足则保持 0 并进入 Weakened（仅降速惩罚，不致死）。
    /// Weakened 时下降速率 ×1.3。三值全部 > 60 时解除 Weakened。
    /// 天气消耗修正（survivorDrainMultiplier）在 Batch 3 接入 WeatherSystem 后由当前天气配置驱动。
    /// </summary>
    public class SurvivorSystem : ISurvivorSystem
    {
        private readonly InventorySystem _inventory = new InventorySystem();

        // 兜底默认消耗速率（每秒）；GameConfig.survivor 优先。
        private const float DefaultHungerDrain = 0.20f;
        private const float DefaultThirstDrain = 0.25f;
        private const float DefaultHealthDrain = 0.10f;
        private const float RestorePerConsume = 30f; // 消耗 1 单位物资恢复的需求量

        public void Tick(GameState state, float deltaSeconds)
        {
            var cfg = GameConfig.Instance;
            float hDrain = DefaultHungerDrain, tDrain = DefaultThirstDrain, hpDrain = DefaultHealthDrain;
            float weakMult = 1f;

            if (cfg != null && cfg.survivor != null)
            {
                hDrain = cfg.survivor.hungerDrainPerSec;
                tDrain = cfg.survivor.thirstDrainPerSec;
                hpDrain = cfg.survivor.healthDrainPerSec;
                weakMult = state.survivorState == SurvivorState.Weakened ? cfg.survivor.weakenedDrainMultiplier : 1f;
            }

            float weatherMult = 1f;
            if (cfg != null)
            {
                var wc = cfg.weathers.Find(w => w.id == state.currentWeather);
                if (wc != null) weatherMult = wc.survivorDrainMultiplier;
            }

            Drain(ref state.hunger, hDrain * weakMult * weatherMult * deltaSeconds);
            Drain(ref state.thirst, tDrain * weakMult * weatherMult * deltaSeconds);
            Drain(ref state.health, hpDrain * weakMult * weatherMult * deltaSeconds);

            // 补货：任一需求 ≤ 0 时尝试消耗对应物资（food→饥饿, water→口渴, medicine→生命）
            TryRefill(ref state.hunger, "food", state);
            TryRefill(ref state.thirst, "water", state);
            TryRefill(ref state.health, "medicine", state);

            // 状态机
            bool starved = state.hunger <= 0f || state.thirst <= 0f || state.health <= 0f;
            SurvivorState next;
            if (starved)
                next = SurvivorState.Weakened;
            else if (state.survivorState == SurvivorState.Weakened &&
                     state.hunger > 60f && state.thirst > 60f && state.health > 60f)
                next = SurvivorState.Normal;
            else
                next = state.survivorState;

            if (next != state.survivorState)
            {
                state.survivorState = next;
                GameEvents.RaiseSurvivorStateChanged(next);
            }
        }

        public bool IsWeakened(GameState state) => state.survivorState == SurvivorState.Weakened;

        private void Drain(ref float value, float amount)
        {
            value -= amount;
            if (value < 0f) value = 0f;
        }

        private void TryRefill(ref float need, string resource, GameState state)
        {
            if (need > 0f) return;
            if (_inventory.TryConsume(state, resource, 1))
                need = RestorePerConsume;
        }
    }
}
