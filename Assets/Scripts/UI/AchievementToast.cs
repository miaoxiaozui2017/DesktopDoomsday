using UnityEngine;
using System.Collections.Generic;
using DesktopDoomsday.Core;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 成就解锁提示（Batch 7，IMGUI 版）。顶部居中轻量吐司，3.5s 后自动消失。
    /// 非交互（不切穿透，保持桌面宠物态）。成就中文名由内置映射表提供，无需改数据类。
    /// </summary>
    public class AchievementToast : MonoBehaviour
    {
        private GUIStyle _title, _label;
        private bool _inited;
        private readonly Queue<string> _queue = new Queue<string>();
        private float _showUntil;
        private string _current;

        private void EnsureStyles()
        {
            if (_inited) return;
            _title = UIHelper.NewLabelStyle(14, UIHelper.C_Amber, TextAnchor.MiddleCenter, true);
            _label = UIHelper.NewLabelStyle(13, UIHelper.C_Cyan, TextAnchor.MiddleCenter);
            _inited = true;
        }

        private void OnEnable() => GameEvents.OnAchievementUnlocked += OnUnlock;
        private void OnDisable() => GameEvents.OnAchievementUnlocked -= OnUnlock;

        private void OnUnlock(string id)
        {
            string label = ResolveName(id);
            if (string.IsNullOrEmpty(label)) label = id;
            _queue.Enqueue(label);
        }

        private static string ResolveName(string id)
        {
            var cfg = GameConfig.Instance;
            if (cfg != null && cfg.achievements != null)
            {
                var a = cfg.achievements.Find(x => x.id == id);
                if (a != null && !string.IsNullOrEmpty(a.displayName)) return a.displayName;
            }
            Names.TryGetValue(id, out var fallback);
            return fallback;
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (string.IsNullOrEmpty(_current) && _queue.Count > 0 && Time.time >= _showUntil)
            {
                _current = _queue.Dequeue();
                _showUntil = Time.time + 3.5f;
            }
            if (string.IsNullOrEmpty(_current)) return;
            if (Time.time >= _showUntil) { _current = null; return; }

            float w = 300f, h = 52f;
            float x = (Screen.width - w) / 2f;
            float y = 24f; // 顶部居中
            UIHelper.DrawRect(new Rect(x, y, w, h), UIHelper.C_Panel);
            UIHelper.DrawBorder(x, y, w, h, UIHelper.C_Amber);
            GUI.Label(new Rect(x, y + 6f, w, 20f), "★ 成就解锁", _title);
            GUI.Label(new Rect(x, y + 28f, w, 20f), _current, _label);
        }

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
    }
}
