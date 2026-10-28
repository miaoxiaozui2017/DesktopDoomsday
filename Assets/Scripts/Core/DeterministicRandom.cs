using System;

namespace DesktopDoomsday.Core
{
    /// <summary>
    /// 确定性随机数发生器（xorshift32）。
    /// 完整内部状态仅为 1 个 uint，可直接存入 GameState.rngSeed 序列化，
    /// 保证读档后随机序列完全可复现（SPEC 验收项 10：同一存档反复读档宝箱掉落序列一致）。
    /// 不依赖 UnityEngine.Random，行为与平台/帧率无关。
    /// </summary>
    public class DeterministicRandom
    {
        private uint _state;

        public DeterministicRandom(int seed)
        {
            _state = (uint)seed;
            if (_state == 0) _state = 0x9E3779B9; // 避免 0 状态导致恒定输出
        }

        /// <summary>当前内部状态，调用方需写回 GameState.rngSeed 以持久化。</summary>
        public int State => (int)_state;

        /// <summary>推进并返回 [0,1) 浮点。</summary>
        public float NextFloat()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            // 取高 24 位映射到 [0,1)
            return ((x >> 8) & 0xFFFFFF) / (float)(1 << 24);
        }

        /// <summary>[0, maxExclusive) 整数。</summary>
        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0) return 0;
            return (int)(NextFloat() * maxExclusive);
        }

        /// <summary>[minInclusive, maxInclusive] 整数。</summary>
        public int Range(int minInclusive, int maxInclusive)
        {
            if (maxInclusive < minInclusive) return minInclusive;
            return minInclusive + NextInt(maxInclusive - minInclusive + 1);
        }
    }
}
