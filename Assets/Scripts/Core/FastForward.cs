using UnityEngine;

namespace DesktopDoomsday.Core
{
    /// <summary>
    /// 开发用快进（SPEC 之外、纯测试便利）：解决"等数值变化无聊"的问题。
    /// 长按 F：tick 加速 20 倍（松手恢复）；单击 G：直接推进 60s；单击 J：直接推进 300s。
    /// 只影响时间推进速度，不改变任何游戏规则。
    /// </summary>
    public class FastForward : MonoBehaviour
    {
        public float holdMultiplier = 20f;
        public float jumpShortSeconds = 60f;
        public float jumpLongSeconds = 300f;

        private void Update()
        {
            bool hold = Input.GetKey(KeyCode.F);
            GameLoop.TimeScale = hold ? holdMultiplier : 1f;

            if (Input.GetKeyDown(KeyCode.G)) Jump(jumpShortSeconds);
            if (Input.GetKeyDown(KeyCode.J)) Jump(jumpLongSeconds);
        }

        private void Jump(float seconds)
        {
            var loop = FindObjectOfType<GameLoop>();
            if (loop != null) loop.JumpSeconds(seconds);
            Debug.Log($"[FastForward] 推进 {seconds}s");
        }

        private void OnDisable() { GameLoop.TimeScale = 1f; }
    }
}
