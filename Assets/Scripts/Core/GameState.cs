using System.Collections.Generic;
using DesktopDoomsday.Data;

namespace DesktopDoomsday.Core
{
    /// <summary>
    /// 单一运行时数据源（单例）。所有系统读写此对象，UI 只订阅事件。
    /// 字段严格对应 SPEC 4.1。
    /// </summary>
    [System.Serializable]
    public class GameState
    {
        // 公路
        public float totalDistanceKm;
        public float currentSpeedKmPerTick;
        public int currentZoneIndex;       // 0=城郊, 1=荒漠, 2=冻原

        // 车辆（upgradeLevels 布局：index = vehicleIndex * TrackCount + trackIndex，TrackCount=2）
        public VehicleId currentVehicle;
        public int[] upgradeLevels;

        // 物资
        public int food, water, medicine, fuel, parts;
        public int blueprintFragments; // 蓝图碎片，用于车辆改装（SPEC 5.7）

        // 小人
        public float hunger;               // 0-100
        public float thirst;
        public float health;
        public SurvivorState survivorState; // Normal / Weakened

        // 天气
        public WeatherId currentWeather;
        public float weatherRemainingSeconds;

        // 宝箱
        public float distanceSinceLastChest;
        public float nextChestThresholdKm;

        // 副本
        public float dungeonCooldownRemaining;
        public DungeonId? pendingDungeon;
        public float pendingDungeonTimeout;

        // 副本设置项（SPEC 5.6，Batch 6 接 UI；默认 false）
        public bool autoIgnoreDungeons;       // 开启后副本提示不出现，冷却照常
        public bool pauseRoadDuringDungeon;   // 开启后打开副本面板时暂停公路 tick

        // 时间
        public long lastSaveUnixMs;
        public float playTimeSeconds;

        // 成就
        public HashSet<string> unlockedAchievementIds = new HashSet<string>();

        // 确定性随机种子（存入存档，保证读档后随机序列可复现）
        public int rngSeed;

        public GameState()
        {
            const int vehicleCount = 3;
            const int trackCount = 2;
            upgradeLevels = new int[vehicleCount * trackCount];
            hunger = thirst = health = 100f;
            survivorState = SurvivorState.Normal;
            currentZoneIndex = 0;
            currentWeather = WeatherId.Sunny;
            rngSeed = 12345; // 默认值，首次开局可由 Bootstrapper 重新随机
            pendingDungeonTimeout = 20f;
            unlockedAchievementIds = new HashSet<string>();
            blueprintFragments = 0;
        }
    }
}
