using System.Collections.Generic;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;

namespace DesktopDoomsday.Systems
{
    /// <summary>
    /// Batch 1 接口空壳。方法签名严格取自 SPEC 5.x，实现在对应 Batch 完成。
    /// 所有系统只接受 GameState 作为参数，不持有 UI 引用。
    /// </summary>

    public interface IRoadSystem
    {
        float GetCurrentSpeed(GameState state, WeatherId weather, VehicleId vehicle);
        void Tick(GameState state, float deltaSeconds);
        float GetZoneProgress(GameState state);
        bool TryUnlockNextZone(GameState state);
    }

    public interface IChestSystem
    {
        void Tick(GameState state, float deltaSeconds);
        ChestOpenResult Open(ChestType type, GameState state);
    }

    public interface IInventorySystem
    {
        void Add(GameState state, string resource, int amount);
        bool TryConsume(GameState state, string resource, int amount);
    }

    public interface ISurvivorSystem
    {
        void Tick(GameState state, float deltaSeconds);
        bool IsWeakened(GameState state);
    }

    public interface IWeatherSystem
    {
        void Tick(GameState state, float deltaSeconds);
        void ApplyToZone(GameState state, ZoneId zone);
    }

    public interface IVehicleSystem
    {
        void SelectVehicle(GameState state, VehicleId vehicle);
        bool TryUpgrade(GameState state, VehicleId vehicle, int trackIndex);
    }

    public interface IDungeonSystem
    {
        bool TryTrigger(GameState state);
        DungeonResult Complete(DungeonId id, int chosenBranchIndex, GameState state);
        void IgnorePending(GameState state);
    }

    public interface IAchievementSystem
    {
        void Tick(GameState state, float deltaSeconds);
        void Check(GameState state);
    }
}
