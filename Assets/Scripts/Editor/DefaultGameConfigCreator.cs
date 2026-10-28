#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DesktopDoomsday.Data;

namespace DesktopDoomsday.EditorTools
{
    /// <summary>
    /// 一键生成默认 GameConfig 资产（Assets/Resources/GameConfig.asset）。
    /// 提供 SPEC 4.2 的基线数值：3 车 / 3 区 / 4 天气 / 物资 / 小人 / 2 副本 / 10 成就。
    /// 菜单：DesktopDoomsday → 创建默认 GameConfig。之后可在 Inspector 里随意改，不影响本脚本。
    /// </summary>
    public static class DefaultGameConfigCreator
    {
        private const string Path = "Assets/Resources/GameConfig.asset";

        [MenuItem("DesktopDoomsday/创建默认 GameConfig")]
        public static void CreateDefault()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            var existing = AssetDatabase.LoadAssetAtPath<GameConfig>(Path);
            if (existing != null &&
                !EditorUtility.DisplayDialog("已存在", "GameConfig.asset 已存在，是否覆盖？", "覆盖", "取消"))
                return;

            var cfg = ScriptableObject.CreateInstance<GameConfig>();

            cfg.vehicles = new List<VehicleConfig>
            {
                new VehicleConfig
                {
                    id = VehicleId.Pickup, displayName = "皮卡", baseSpeed = 0.05f, fuelPerKm = 0.6f,
                    cargoCapacity = 60f, chestChanceModifier = 1.05f, dungeonRewardModifier = 1f,
                    upgradeTracks = new List<UpgradeTrack>
                    {
                        new UpgradeTrack { trackName = "引擎", maxLevel = 2, speedBonusPerLevel = new[] { 0.010f, 0.020f } },
                        new UpgradeTrack { trackName = "货箱", maxLevel = 2, capacityBonusPerLevel = new[] { 10f, 20f } },
                    }
                },
                new VehicleConfig
                {
                    id = VehicleId.Van, displayName = "厢式货车", baseSpeed = 0.04f, fuelPerKm = 0.9f,
                    cargoCapacity = 100f, chestChanceModifier = 1f, dungeonRewardModifier = 1.1f,
                    upgradeTracks = new List<UpgradeTrack>
                    {
                        new UpgradeTrack { trackName = "引擎", maxLevel = 2, speedBonusPerLevel = new[] { 0.008f, 0.016f } },
                        new UpgradeTrack { trackName = "货箱", maxLevel = 2, capacityBonusPerLevel = new[] { 15f, 30f } },
                    }
                },
                new VehicleConfig
                {
                    id = VehicleId.Motorcycle, displayName = "摩托", baseSpeed = 0.07f, fuelPerKm = 0.4f,
                    cargoCapacity = 35f, chestChanceModifier = 1f, dungeonRewardModifier = 1f,
                    upgradeTracks = new List<UpgradeTrack>
                    {
                        new UpgradeTrack { trackName = "引擎", maxLevel = 2, speedBonusPerLevel = new[] { 0.015f, 0.030f } },
                        new UpgradeTrack { trackName = "油箱", maxLevel = 2, speedBonusPerLevel = new[] { 0.005f, 0.010f } },
                    }
                },
            };

            cfg.zones = new List<ZoneConfig>
            {
                new ZoneConfig
                {
                    id = ZoneId.Suburb, name = "城郊", unlockDistanceKm = 0f,
                    weatherWeightTable = new List<ZoneWeatherWeight>
                    {
                        new ZoneWeatherWeight { weather = WeatherId.Sunny, weight = 60f },
                        new ZoneWeatherWeight { weather = WeatherId.Rainstorm, weight = 30f },
                        new ZoneWeatherWeight { weather = WeatherId.Sandstorm, weight = 10f },
                    },
                    dungeonPool = new List<DungeonId> { DungeonId.WarehouseRaid }
                },
                new ZoneConfig
                {
                    id = ZoneId.Desert, name = "荒漠", unlockDistanceKm = 30f,
                    weatherWeightTable = new List<ZoneWeatherWeight>
                    {
                        new ZoneWeatherWeight { weather = WeatherId.Sunny, weight = 40f },
                        new ZoneWeatherWeight { weather = WeatherId.Sandstorm, weight = 50f },
                        new ZoneWeatherWeight { weather = WeatherId.Rainstorm, weight = 10f },
                    },
                    dungeonPool = new List<DungeonId> { DungeonId.ConvoyChase }
                },
                new ZoneConfig
                {
                    id = ZoneId.Tundra, name = "冻原", unlockDistanceKm = 120f,
                    weatherWeightTable = new List<ZoneWeatherWeight>
                    {
                        new ZoneWeatherWeight { weather = WeatherId.Sunny, weight = 35f },
                        new ZoneWeatherWeight { weather = WeatherId.ExtremeCold, weight = 50f },
                        new ZoneWeatherWeight { weather = WeatherId.Rainstorm, weight = 15f },
                    },
                    dungeonPool = new List<DungeonId> { DungeonId.WarehouseRaid, DungeonId.ConvoyChase }
                },
            };

            cfg.weathers = new List<WeatherConfig>
            {
                new WeatherConfig { id = WeatherId.Sunny, displayName = "晴天", speedMultiplier = 1.0f, fuelMultiplier = 1.0f, chestChanceBonus = 0f, survivorDrainMultiplier = 1.0f, durationRange = new Vector2(60f, 120f) },
                new WeatherConfig { id = WeatherId.Rainstorm, displayName = "暴雨", speedMultiplier = 0.85f, fuelMultiplier = 1.0f, chestChanceBonus = 0.10f, survivorDrainMultiplier = 1.0f, durationRange = new Vector2(30f, 60f) },
                new WeatherConfig { id = WeatherId.Sandstorm, displayName = "沙暴", speedMultiplier = 0.90f, fuelMultiplier = 1.25f, chestChanceBonus = 0.05f, survivorDrainMultiplier = 1.0f, durationRange = new Vector2(30f, 60f) },
                new WeatherConfig { id = WeatherId.ExtremeCold, displayName = "极寒", speedMultiplier = 0.90f, fuelMultiplier = 1.0f, chestChanceBonus = 0f, survivorDrainMultiplier = 1.3f, durationRange = new Vector2(40f, 80f) },
            };

            cfg.resources = new ResourceConfig
            {
                initialFood = 50, initialWater = 50, initialMedicine = 30, initialFuel = 100, initialParts = 20,
                baseChestChance = 0.02f,
                dropWeights = new List<ResourceDropWeight>
                {
                    new ResourceDropWeight { resource = "food", weight = 30f },
                    new ResourceDropWeight { resource = "water", weight = 30f },
                    new ResourceDropWeight { resource = "medicine", weight = 15f },
                    new ResourceDropWeight { resource = "fuel", weight = 15f },
                    new ResourceDropWeight { resource = "parts", weight = 10f },
                }
            };

            cfg.survivor = new SurvivorConfig
            {
                hungerDrainPerSec = 0.15f, thirstDrainPerSec = 0.20f, healthDrainPerSec = 0.05f,
                weakenThreshold = 0f, weakenedDrainMultiplier = 1.3f,
                recoveryPerSec = 1.0f, recoveryDurationSec = 60f
            };

            cfg.dungeons = new List<DungeonConfig>
            {
                new DungeonConfig
                {
                    id = DungeonId.WarehouseRaid, displayName = "废弃仓库", durationSeconds = 75f,
                    triggerCooldownSeconds = 180f, triggerChancePerTick = 0.01f,
                    branches = new List<DungeonBranch>
                    {
                        new DungeonBranch { branchName = "搜刮仓库", rewardMultiplier = 2.5f, blueprintFragmentReward = 2 },
                        new DungeonBranch { branchName = "检查加油站", rewardMultiplier = 2.0f, blueprintFragmentReward = 1 },
                    }
                },
                new DungeonConfig
                {
                    id = DungeonId.ConvoyChase, displayName = "追击车队", durationSeconds = 70f,
                    triggerCooldownSeconds = 240f, triggerChancePerTick = 0.008f,
                    branches = new List<DungeonBranch>
                    {
                        new DungeonBranch { branchName = "追击车队", rewardMultiplier = 3.0f, blueprintFragmentReward = 3 },
                        new DungeonBranch { branchName = "绕路避险", rewardMultiplier = 1.8f, blueprintFragmentReward = 1 },
                    }
                },
            };

            cfg.achievements = new List<AchievementConfig>
            {
                new AchievementConfig { id = "dist_10", conditionType = "distance", parameter = 10f },
                new AchievementConfig { id = "dist_100", conditionType = "distance", parameter = 100f },
                new AchievementConfig { id = "dist_1000", conditionType = "distance", parameter = 1000f },
                new AchievementConfig { id = "chest_1", conditionType = "chestsOpened", parameter = 1f },
                new AchievementConfig { id = "chest_50", conditionType = "chestsOpened", parameter = 50f },
                new AchievementConfig { id = "chest_200", conditionType = "chestsOpened", parameter = 200f },
                new AchievementConfig { id = "dungeon_1", conditionType = "dungeons", parameter = 1f },
                new AchievementConfig { id = "dungeon_10", conditionType = "dungeons", parameter = 10f },
                new AchievementConfig { id = "zone_2", conditionType = "zones", parameter = 2f },
                new AchievementConfig { id = "zone_3", conditionType = "zones", parameter = 3f },
            };

            AssetDatabase.CreateAsset(cfg, Path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = cfg;
            EditorGUIUtility.PingObject(cfg);
            Debug.Log($"[DefaultGameConfig] 已创建 {Path}");
        }
    }
}
#endif
