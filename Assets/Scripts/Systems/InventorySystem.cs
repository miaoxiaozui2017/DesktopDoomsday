using DesktopDoomsday.Core;
using DesktopDoomsday.Data;

namespace DesktopDoomsday.Systems
{
    /// <summary>
    /// 物资库存系统（SPEC 4.1 / 5.x）。库存即 GameState 的五个 int 字段。
    /// 提供 Add / TryConsume / GetAmount，并在变化时触发 OnInventoryChanged。
    /// 被 ChestSystem（入库）与 SurvivorSystem（消耗）复用。
    /// </summary>
    public class InventorySystem : IInventorySystem
    {
        public void Add(GameState state, string resource, int amount)
        {
            if (amount == 0) return;
            SetAmount(state, resource, GetAmount(state, resource) + amount);
            GameEvents.RaiseInventoryChanged(new ResourceDelta { resource = resource, delta = amount });
        }

        public bool TryConsume(GameState state, string resource, int amount)
        {
            if (amount <= 0) return true;
            int have = GetAmount(state, resource);
            if (have < amount) return false;
            SetAmount(state, resource, have - amount);
            GameEvents.RaiseInventoryChanged(new ResourceDelta { resource = resource, delta = -amount });
            return true;
        }

        public int GetAmount(GameState state, string resource)
        {
            switch (resource)
            {
                case "food": return state.food;
                case "water": return state.water;
                case "medicine": return state.medicine;
                case "fuel": return state.fuel;
                case "parts": return state.parts;
                default: return 0;
            }
        }

        private void SetAmount(GameState state, string resource, int value)
        {
            int v = value < 0 ? 0 : value;
            switch (resource)
            {
                case "food": state.food = v; break;
                case "water": state.water = v; break;
                case "medicine": state.medicine = v; break;
                case "fuel": state.fuel = v; break;
                case "parts": state.parts = v; break;
            }
        }
    }
}
