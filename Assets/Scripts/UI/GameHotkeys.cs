using UnityEngine;
using DesktopDoomsday.Core;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 正式玩法快捷键路由（Batch 8）。与调试键（DebugHud）分离，始终生效：
    /// I 背包 / A 成就图鉴 / H 帮助。面板开关由各自组件维护并自动管理点击穿透。
    /// </summary>
    public class GameHotkeys : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I)) InventoryPanel.Toggle();
            if (Input.GetKeyDown(KeyCode.A)) AchievementGallery.Toggle();
            if (Input.GetKeyDown(KeyCode.H)) HelpPanel.Toggle();
        }
    }
}
