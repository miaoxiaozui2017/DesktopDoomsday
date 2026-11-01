using UnityEngine;
using UnityEngine.UI;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 正式科技感 HUD（SPEC 9.4，替代 IMGUI 调试层的外观，但调试层仍保留供开发）。
    /// 暗玻璃面板 + 霓虹青描边 + 等宽文本 + 霓虹条形进度；只读 GameState，订阅事件 + 0.5s 节流刷新连续值。
    /// 位置：屏幕左下，不挡桌面宠物主体。
    /// </summary>
    public class MainHud : MonoBehaviour
    {
        private Text _title, _status, _res, _blue, _ach;
        private Image _barHunger, _barThirst, _barHealth;
        private float _innerHunger, _innerThirst, _innerHealth;
        private GameObject _root;

        private void OnEnable() { Build(); }
        private void OnDisable() { if (_root != null) Destroy(_root); _root = null; }

        private void Start()
        {
            InvokeRepeating(nameof(Refresh), 0.1f, 0.5f);
        }

        private void Build()
        {
            if (_root != null) return;
            _root = UIHelper.Panel("MainHud", 16f, 16f, 300f, 196f, UIHelper.Palette.Panel, UIHelper.Palette.Cyan);

            float x = 12f, w = 276f; float y = 8f;
            _title = UIHelper.Label(_root, "DESKTOP DOOMSDAY", x, y, w, 18f, 14, UIHelper.Palette.Cyan, TextAnchor.MiddleLeft);
            y += 22f;
            // 左上角琥珀强调条
            var accent = UIHelper.Label(_root, "", x, y - 2f, 60f, 2f, 1, UIHelper.Palette.Amber);
            _status = UIHelper.Label(_root, "", x, y, w, 18f, 12, UIHelper.Palette.Text);
            y += 22f;

            // 三条需求霓虹条
            UIHelper.Label(_root, "饥饿", x, y - 2f, 44f, 14f, 11, UIHelper.Palette.Sub);
            _barHunger = UIHelper.NeonBar(_root, x + 46f, y, w - 46f, 12f, out _innerHunger);
            y += 18f;
            UIHelper.Label(_root, "口渴", x, y - 2f, 44f, 14f, 11, UIHelper.Palette.Sub);
            _barThirst = UIHelper.NeonBar(_root, x + 46f, y, w - 46f, 12f, out _innerThirst);
            y += 18f;
            UIHelper.Label(_root, "健康", x, y - 2f, 44f, 14f, 11, UIHelper.Palette.Sub);
            _barHealth = UIHelper.NeonBar(_root, x + 46f, y, w - 46f, 12f, out _innerHealth);
            y += 20f;

            _res = UIHelper.Label(_root, "", x, y, w, 16f, 11, UIHelper.Palette.Text);
            y += 17f;
            _blue = UIHelper.Label(_root, "", x, y, w, 16f, 11, UIHelper.Palette.Amber);
            y += 17f;
            _ach = UIHelper.Label(_root, "", x, y, w, 14f, 10, UIHelper.Palette.Sub);

            GameEvents.OnDistanceAdvanced += _ => Refresh();
            GameEvents.OnWeatherChanged += _ => Refresh();
            GameEvents.OnZoneChanged += _ => Refresh();
            GameEvents.OnSurvivorStateChanged += _ => Refresh();
            GameEvents.OnInventoryChanged += _ => Refresh();
            GameEvents.OnAchievementUnlocked += _ => Refresh();

            Refresh();
        }

        private void Refresh()
        {
            var s = ServiceLocator.Get<GameState>();
            if (s == null || _root == null) return;

            _status.text = $"里程 {s.totalDistanceKm:F2}km  区域 {ZoneName(s.currentZoneIndex)}  {WeatherName(s.currentWeather)}";
            _res.text = $"食{s.food} 水{s.water} 药{s.medicine} 燃{s.fuel} 件{s.parts}";
            _blue.text = $"蓝图碎片 {s.blueprintFragments}";
            _ach.text = $"成就 {s.unlockedAchievementIds.Count}/{(GameConfig.Instance != null ? GameConfig.Instance.achievements.Count : 0)}";

            SetBar(_barHunger, _innerHunger, s.hunger);
            SetBar(_barThirst, _innerThirst, s.thirst);
            SetBar(_barHealth, _innerHealth, s.health);
        }

        private static void SetBar(Image fill, float innerW, float value)
        {
            float v = Mathf.Clamp01(value / 100f);
            fill.rectTransform.sizeDelta = new Vector2(innerW * v, fill.rectTransform.sizeDelta.y);
            fill.color = v > 0.6f ? UIHelper.Palette.Green : v > 0.3f ? UIHelper.Palette.Amber : UIHelper.Palette.Red;
        }

        private static string ZoneName(int idx)
        {
            switch (idx) { case 1: return "荒漠"; case 2: return "冻原"; default: return "城郊"; }
        }

        private static string WeatherName(WeatherId w)
        {
            switch (w)
            {
                case WeatherId.Rainstorm: return "暴雨";
                case WeatherId.Sandstorm: return "沙暴";
                case WeatherId.ExtremeCold: return "极寒";
                default: return "晴天";
            }
        }
    }
}
