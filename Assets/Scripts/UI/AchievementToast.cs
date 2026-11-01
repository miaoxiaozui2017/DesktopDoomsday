using System.Collections.Generic;
using DesktopDoomsday.Core;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 成就解锁提示（Batch 7）。顶部居中轻量吐司，3.5s 后自动消失。非交互（不切穿透，保持桌面宠物态）。
    /// </summary>
    public class AchievementToast : MonoBehaviour
    {
        private readonly List<GameObject> _active = new List<GameObject>();

        private static readonly Dictionary<string, string> Names = new Dictionary<string, string>
        {
            { "dist_10", "初出城郊 · 行驶 10km" },
            { "dist_100", "长途跋涉 · 行驶 100km" },
            { "dist_1000", "千里走单骑 · 行驶 1000km" },
            { "chest_1", "初见宝箱" },
            { "chest_50", "拾荒老手 · 50 宝箱" },
            { "chest_200", "宝箱猎人 · 200 宝箱" },
            { "dungeon_1", "初探副本" },
            { "dungeon_10", "副本常客 · 10 副本" },
            { "zone_2", "穿越荒漠" },
            { "zone_3", "极寒来客" },
        };

        private void OnEnable() => GameEvents.OnAchievementUnlocked += Show;
        private void OnDisable()
        {
            GameEvents.OnAchievementUnlocked -= Show;
            foreach (var g in _active) if (g != null) Destroy(g);
            _active.Clear();
        }

        private void Show(string id)
        {
            Names.TryGetValue(id, out var label);
            if (string.IsNullOrEmpty(label)) label = id;

            float w = 280f, h = 40f;
            float x = (Screen.width - w) / 2f;
            float y = 24f + _active.Count * (h + 8f);
            var go = UIHelper.Panel("AchToast", x, y, w, h, UIHelper.Palette.Panel, UIHelper.Palette.Cyan);
            UIHelper.Label(go, "★ " + label, 10f, 0f, w - 20f, h, 13, UIHelper.Palette.Cyan, TextAnchor.MiddleLeft);
            _active.Add(go);
            int idx = _active.Count - 1;
            Destroy(go, 3.5f);
            Invoke(nameof(Cleanup), 3.6f);
        }

        private void Cleanup()
        {
            _active.RemoveAll(g => g == null);
        }
    }
}
