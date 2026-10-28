using UnityEngine;

namespace DesktopDoomsday.Core
{
    /// <summary>
    /// 固定 tick 调度器。所有 Systems 订阅 OnTick，避免各自 Update。
    /// 帧率与 tick 解耦：tick 用 unscaledDeltaTime 累加真实时间，保证后台/最小化时收益一致。
    /// </summary>
    public class GameLoop : MonoBehaviour
    {
        public float baseTickInterval = 1.0f;
        public float backgroundTickInterval = 5.0f;

        public static event System.Action<float> OnTick;

        /// <summary>开发用时间加速倍数（FastForward 长按 F）。1 = 正常。</summary>
        public static float TimeScale = 1f;

        private float _accumulator;
        private bool _background;

        /// <summary>
        /// 由 TrayManager / ClickThroughManager 在最小化或穿透时调用。
        /// 后台时降低 tick 频率，但 OnTick 传入的是真实经过时间，逻辑按真实时间补齐。
        /// </summary>
        public void SetBackground(bool background) => _background = background;

        private void Update()
        {
            float interval = _background ? backgroundTickInterval : baseTickInterval;
            _accumulator += Time.unscaledDeltaTime * (TimeScale > 0f ? TimeScale : 1f);
            if (_accumulator >= interval)
            {
                float delta = _accumulator; // 真实经过秒数（含后台补足）
                _accumulator = 0f;
                OnTick?.Invoke(delta);
            }
        }

        /// <summary>一次性推进指定秒数（FastForward 单击跳秒）。</summary>
        public void JumpSeconds(float seconds)
        {
            if (seconds > 0f) OnTick?.Invoke(seconds);
        }
    }
}
