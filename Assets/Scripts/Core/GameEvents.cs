using System;
using System.Collections.Generic;
using DesktopDoomsday.Data;

namespace DesktopDoomsday.Core
{
    public struct ResourceDelta
    {
        public string resource; // "food","water","medicine","fuel","parts"
        public int delta;
    }

    public struct ChestOpenResult
    {
        public ChestType type;
        public List<ResourceDelta> rewards;
        public int blueprintFragments;
    }

    public struct DungeonResult
    {
        public DungeonId id;
        public List<ResourceDelta> rewards;
        public int blueprintFragments;
    }

    public struct OfflineReport
    {
        public float distanceGain;
        public List<ResourceDelta> resourceChanges;
        public SurvivorState survivorState;
        public float offlineSeconds;
    }

    /// <summary>
    /// 跨系统通信总线。UI 订阅，Systems 通过 Raise* 触发。
    /// 规则：仅状态真正变化时触发；回调内禁止直接改其他系统状态。
    /// </summary>
    public static class GameEvents
    {
        public static event Action<float> OnDistanceAdvanced;
        public static event Action<ChestOpenResult> OnChestOpened;
        public static event Action<ResourceDelta> OnInventoryChanged;
        public static event Action<SurvivorState> OnSurvivorStateChanged;
        public static event Action<WeatherId> OnWeatherChanged;
        public static event Action<int> OnZoneChanged;
        public static event Action<DungeonId> OnDungeonAvailable;
        public static event Action<DungeonResult> OnDungeonCompleted;
        public static event Action<OfflineReport> OnOfflineReportReady;
        public static event Action<string> OnAchievementUnlocked;

        // null-safe 触发器
        public static void RaiseDistanceAdvanced(float d) => OnDistanceAdvanced?.Invoke(d);
        public static void RaiseChestOpened(ChestOpenResult r) => OnChestOpened?.Invoke(r);
        public static void RaiseInventoryChanged(ResourceDelta d) => OnInventoryChanged?.Invoke(d);
        public static void RaiseSurvivorStateChanged(SurvivorState s) => OnSurvivorStateChanged?.Invoke(s);
        public static void RaiseWeatherChanged(WeatherId w) => OnWeatherChanged?.Invoke(w);
        public static void RaiseZoneChanged(int z) => OnZoneChanged?.Invoke(z);
        public static void RaiseDungeonAvailable(DungeonId d) => OnDungeonAvailable?.Invoke(d);
        public static void RaiseDungeonCompleted(DungeonResult r) => OnDungeonCompleted?.Invoke(r);
        public static void RaiseOfflineReportReady(OfflineReport r) => OnOfflineReportReady?.Invoke(r);
        public static void RaiseAchievementUnlocked(string id) => OnAchievementUnlocked?.Invoke(id);
    }
}
