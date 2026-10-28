using System.Linq;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;

namespace DesktopDoomsday.Systems
{
    /// <summary>
    /// 车辆系统（SPEC 5.7）。
    /// - 开局选择：SelectVehicle 写入 currentVehicle，之后不可更换（选定由 Batch 6 的选车面板调用）。
    /// - 改装：每条改装线 2 级，消耗 parts + 蓝图碎片；升级等级存 GameState.upgradeLevels，
    ///   效果由 RoadSystem 读取应用（直接读 ScriptableObject 原始数据 + 等级，不改写原始资产）。
    /// </summary>
    public class VehicleSystem : IVehicleSystem
    {
        private readonly InventorySystem _inventory = new InventorySystem();

        // 改装成本（MVP 可调；后续可移入 UpgradeTrack 配置字段）
        private const int PartsBaseCost = 15;        // 每级 parts 成本 = PartsBaseCost * (当前等级+1)
        private const int FragmentCostPerLevel = 1;  // 每级蓝图碎片成本 = FragmentCostPerLevel * (当前等级+1)

        public void SelectVehicle(GameState state, VehicleId vehicle)
        {
            state.currentVehicle = vehicle;
        }

        public bool TryUpgrade(GameState state, VehicleId vehicle, int trackIndex)
        {
            var cfg = GameConfig.Instance;
            var vc = cfg?.vehicles?.FirstOrDefault(v => v.id == vehicle);
            if (vc == null || trackIndex < 0 || trackIndex >= vc.upgradeTracks.Count) return false;

            int vIdx = (int)vehicle;
            int flat = vIdx * 2 + trackIndex; // 与 GameState.upgradeLevels 布局一致（TrackCount=2）
            int lvl = state.upgradeLevels[flat];
            var track = vc.upgradeTracks[trackIndex];
            if (lvl >= track.maxLevel) return false; // 已满级

            int partsCost = PartsBaseCost * (lvl + 1);
            int fragCost = FragmentCostPerLevel * (lvl + 1);

            // 先扣 parts，再扣蓝图碎片；碎片不足则回滚 parts，保证原子性
            if (!_inventory.TryConsume(state, "parts", partsCost))
                return false;
            if (state.blueprintFragments < fragCost)
            {
                _inventory.Add(state, "parts", partsCost);
                return false;
            }
            state.blueprintFragments -= fragCost;
            state.upgradeLevels[flat] = lvl + 1;
            return true;
        }

        /// <summary>
        /// 查询指定改装线「下一级」所需成本（用于 HUD / 属性页展示）。
        /// 返回 false 表示无法升级：cfg 缺失、trackIndex 越界、或已满级（此时 maxed=true）。
        /// 成本公式：parts = PartsBaseCost*(当前等级+1)，fragments = FragmentCostPerLevel*(当前等级+1)。
        /// </summary>
        public bool GetUpgradeCost(GameState state, VehicleId vehicle, int trackIndex,
            out int partsCost, out int fragCost, out bool maxed)
        {
            partsCost = 0; fragCost = 0; maxed = false;
            var cfg = GameConfig.Instance;
            var vc = cfg?.vehicles?.FirstOrDefault(v => v.id == vehicle);
            if (vc == null || trackIndex < 0 || trackIndex >= vc.upgradeTracks.Count) return false;

            int flat = (int)vehicle * 2 + trackIndex;
            int lvl = state.upgradeLevels[flat];
            var track = vc.upgradeTracks[trackIndex];
            if (lvl >= track.maxLevel) { maxed = true; return false; }

            partsCost = PartsBaseCost * (lvl + 1);
            fragCost = FragmentCostPerLevel * (lvl + 1);
            return true;
        }

        /// <summary>当前车辆某改装线已叠加的速度加成（各等级 speedBonusPerLevel 之和）。</summary>
        public float GetSpeedBonus(GameState state, VehicleId vehicle, int trackIndex)
        {
            var cfg = GameConfig.Instance;
            var vc = cfg?.vehicles?.FirstOrDefault(v => v.id == vehicle);
            if (vc == null || trackIndex < 0 || trackIndex >= vc.upgradeTracks.Count) return 0f;

            int flat = (int)vehicle * 2 + trackIndex;
            int lvl = state.upgradeLevels[flat];
            var arr = vc.upgradeTracks[trackIndex].speedBonusPerLevel;
            if (arr == null) return 0f;
            float sum = 0f;
            for (int i = 0; i < lvl && i < arr.Length; i++) sum += arr[i];
            return sum;
        }
    }
}
