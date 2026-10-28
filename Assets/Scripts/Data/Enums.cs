namespace DesktopDoomsday.Data
{
    // 车辆
    public enum VehicleId { Pickup, Van, Motorcycle }

    // 天气
    public enum WeatherId { Sunny, Rainstorm, Sandstorm, ExtremeCold }

    // 副本
    public enum DungeonId { WarehouseRaid, ConvoyChase }

    // 宝箱类型
    public enum ChestType { Mileage, Random, Dungeon }

    // 小人状态
    public enum SurvivorState { Normal, Weakened }

    // 区域（GameState.currentZoneIndex 用 int 同步：0=城郊,1=荒漠,2=冻原）
    public enum ZoneId { Suburb, Desert, Tundra }
}
