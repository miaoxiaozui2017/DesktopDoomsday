using System.Collections.Generic;
using UnityEngine;

namespace DesktopDoomsday.Data
{
    [System.Serializable]
    public class UpgradeTrack
    {
        public string trackName;
        public int maxLevel = 2;
        public float[] speedBonusPerLevel;   // 每级叠加到 baseSpeed
        public float[] fuelBonusPerLevel;
        public float[] capacityBonusPerLevel;
    }

    [System.Serializable]
    public class VehicleConfig
    {
        public VehicleId id;
        public string displayName;
        public float baseSpeed;
        public float fuelPerKm;
        public float cargoCapacity;
        public float chestChanceModifier;
        public float dungeonRewardModifier;  // 副本奖励修正（如厢式货车 +10%）
        public List<UpgradeTrack> upgradeTracks = new List<UpgradeTrack>();
    }

    [System.Serializable]
    public class ZoneWeatherWeight
    {
        public WeatherId weather;
        public float weight;
    }

    [System.Serializable]
    public class ZoneConfig
    {
        public ZoneId id;
        public string name;
        public float unlockDistanceKm;
        public List<ZoneWeatherWeight> weatherWeightTable = new List<ZoneWeatherWeight>();
        public List<DungeonId> dungeonPool = new List<DungeonId>();
    }

    [System.Serializable]
    public class WeatherConfig
    {
        public WeatherId id;
        public string displayName;
        public float speedMultiplier = 1f;
        public float fuelMultiplier = 1f;
        public float chestChanceBonus;        // 相对基准的加成（如 +0.1）
        public float survivorDrainMultiplier = 1f;
        public Vector2 durationRange = new Vector2(30f, 90f);
    }

    [System.Serializable]
    public class ResourceDropWeight
    {
        public string resource;               // "food","water","medicine","fuel","parts"
        public float weight;
    }

    [System.Serializable]
    public class ResourceConfig
    {
        public int initialFood, initialWater, initialMedicine, initialFuel, initialParts;
        public float baseChestChance = 0.05f; // 随机宝箱每 tick 基础概率
        public List<ResourceDropWeight> dropWeights = new List<ResourceDropWeight>();
    }

    [System.Serializable]
    public class SurvivorConfig
    {
        public float hungerDrainPerSec, thirstDrainPerSec, healthDrainPerSec;
        public float weakenThreshold = 0f;     // 任一需求 <= 此值则尝试补货，不足则 Weakened
        public float weakenedDrainMultiplier = 1.3f;
        public float recoveryPerSec;
        public float recoveryDurationSec = 60f;
    }

    [System.Serializable]
    public class DungeonBranch
    {
        public string branchName;              // 如 "搜刮仓库"
        public float rewardMultiplier;         // 相对基础挂机收益的倍数
        public int blueprintFragmentReward;
    }

    [System.Serializable]
    public class DungeonConfig
    {
        public DungeonId id;
        public string displayName;
        public float durationSeconds;          // 面板时长 60-90s
        public float triggerCooldownSeconds;   // 冷却
        public float triggerChancePerTick;     // 冷却结束后每 tick 触发概率
        public List<DungeonBranch> branches = new List<DungeonBranch>();
    }

    [System.Serializable]
    public class AchievementConfig
    {
        public string id;
        public string conditionType;           // "distance","chestsOpened","dungeons","zones","resources"
        public float parameter;
    }

    [CreateAssetMenu(fileName = "GameConfig", menuName = "DesktopDoomsday/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        public List<VehicleConfig> vehicles = new List<VehicleConfig>();
        public List<ZoneConfig> zones = new List<ZoneConfig>();
        public List<WeatherConfig> weathers = new List<WeatherConfig>();
        public ResourceConfig resources;
        public SurvivorConfig survivor;
        public List<DungeonConfig> dungeons = new List<DungeonConfig>();
        public List<AchievementConfig> achievements = new List<AchievementConfig>();

        private static GameConfig _instance;
        public static GameConfig Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<GameConfig>("GameConfig");
                return _instance;
            }
        }
    }
}
