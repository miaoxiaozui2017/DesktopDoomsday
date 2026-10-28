using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;

namespace DesktopDoomsday.Systems
{
    /// <summary>
    /// 天气系统（SPEC 5.5）。
    /// 按当前区域权重表随机抽取天气，持续 durationRange 内随机秒数；仅触发 OnWeatherChanged 事件（不弹模态窗）。
    /// 天气的数值修正（速度/油耗/宝箱概率/小人消耗）由 RoadSystem / ChestSystem / SurvivorSystem 读取 GameConfig 应用。
    /// 极端天气（沙暴/极寒）首次出现时日志提示一次（托盘气泡见 Batch 5）。
    /// 随机走确定性 RNG（GameState.rngSeed），读档可复现（验收项 10）。
    /// </summary>
    public class WeatherSystem : IWeatherSystem
    {
        // 会话内已提示过的极端天气（避免重复刷屏；按 SPEC「同一天不再重复」的近似实现，完整托盘在 Batch 5）
        private static readonly HashSet<WeatherId> ExtremeNotifiedThisSession = new HashSet<WeatherId>();
        private static readonly HashSet<WeatherId> ExtremeWeathers = new HashSet<WeatherId>
        {
            WeatherId.Sandstorm, WeatherId.ExtremeCold
        };

        public void Tick(GameState state, float deltaSeconds)
        {
            state.weatherRemainingSeconds -= deltaSeconds;
            if (state.weatherRemainingSeconds <= 0f)
                PickNewWeather(state);
        }

        /// <summary>新开局初始化首个天气与持续时间（由 Bootstrapper 调用）。</summary>
        public void Initialize(GameState state)
        {
            PickNewWeather(state, forceFromZone: true);
        }

        /// <summary>区域切换时按新区域权重重抽天气（SPEC：区域解锁 → 新天气权重更新）。</summary>
        public void ApplyToZone(GameState state, ZoneId zone)
        {
            PickNewWeather(state, forceFromZone: true);
        }

        private void PickNewWeather(GameState state, bool forceFromZone = false)
        {
            var cfg = GameConfig.Instance;
            WeatherId newWeather = state.currentWeather;
            float duration = 60f;

            if (cfg != null && cfg.weathers != null && cfg.weathers.Count > 0)
            {
                // 选权重表：优先当前区域 weatherWeightTable，否则退化为全局各天气等权
                List<ZoneWeatherWeight> weights = null;
                if (cfg.zones != null && cfg.zones.Count > 0)
                {
                    int idx = Mathf.Clamp(state.currentZoneIndex, 0, cfg.zones.Count - 1);
                    weights = cfg.zones[idx].weatherWeightTable;
                }

                var rng = new DeterministicRandom(state.rngSeed);
                newWeather = PickWeather(rng, cfg, weights);
                var wc = cfg.weathers.FirstOrDefault(w => w.id == newWeather);
                if (wc != null)
                    duration = rng.Range((int)wc.durationRange.x, (int)wc.durationRange.y);
                state.rngSeed = rng.State;
            }

            bool changed = newWeather != state.currentWeather || forceFromZone;
            state.currentWeather = newWeather;
            state.weatherRemainingSeconds = duration;

            if (changed)
            {
                GameEvents.RaiseWeatherChanged(newWeather);
                if (ExtremeWeathers.Contains(newWeather) && !ExtremeNotifiedThisSession.Contains(newWeather))
                {
                    ExtremeNotifiedThisSession.Add(newWeather);
                    Debug.Log($"[WeatherSystem] 极端天气出现：{newWeather}（首次提示；托盘气泡见 Batch 5）");
                }
            }
        }

        private WeatherId PickWeather(DeterministicRandom rng, GameConfig cfg, List<ZoneWeatherWeight> zoneWeights)
        {
            if (zoneWeights != null && zoneWeights.Count > 0)
            {
                float total = 0f;
                foreach (var w in zoneWeights) total += w.weight;
                if (total > 0f)
                {
                    float roll = rng.NextFloat() * total;
                    foreach (var w in zoneWeights)
                    {
                        roll -= w.weight;
                        if (roll <= 0f) return w.weather;
                    }
                    return zoneWeights[zoneWeights.Count - 1].weather;
                }
            }
            // 退化：全局等权
            var all = cfg.weathers;
            return all[rng.NextInt(all.Count)].id;
        }
    }
}
