using System.Linq;
using UnityEngine;
using DesktopDoomsday.Core;
using DesktopDoomsday.Data;
using DesktopDoomsday.Systems;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// 车辆独立属性页（按 V 单独调取）。
    /// 展示当前车辆肖像、改装线等级、下一级所需 P/B 成本、已叠加速度加成、蓝图/零件存量。
    /// 仅读取 GameState / VehicleSystem / GameConfig，不反写。
    /// </summary>
    public class VehicleInfoPanel : MonoBehaviour
    {
        public bool show;
        private Texture2D _white;
        private Sprite[] _vehFrames = new Sprite[3]; // 三种车各取第 0 帧
        private GUIStyle _title, _text, _small;
        private bool _stylesReady;

        private void Awake()
        {
            _white = Texture2D.whiteTexture;
#if UNITY_EDITOR
            LoadFrames();
#endif
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.V)) show = !show;
        }

        private void OnGUI()
        {
            if (!show) return;
            var s = ServiceLocator.Get<GameState>();
            var vs = ServiceLocator.Get<VehicleSystem>();
            if (s == null || vs == null) return;
            EnsureStyles();

            float w = 392f, h = 286f;
            var p = new Rect(Screen.width - w - 16f, 16f, w, h);
            Fill(p, new Color(0.043f, 0.055f, 0.078f, 0.92f));
            Border(p);

            float x = p.x + 12f, y = p.y + 10f;
            GUI.Label(new Rect(x, y, w - 24f, 20f), "车辆 · VEHICLE", _title); y += 26f;

            // 肖像（右侧）
            var fr = _vehFrames.Length > (int)s.currentVehicle ? _vehFrames[(int)s.currentVehicle] : null;
            if (fr != null)
                GUI.DrawTexture(new Rect(p.x + w - 132f, y, 116f, 80f), fr.texture, ScaleMode.ScaleToFit);

            int dataW = w - 156;
            string vName = VehicleName(s.currentVehicle);
            GUI.Label(new Rect(x, y, dataW, 18f), $"当前：{vName}", _text); y += 22f;

            var cfg = GameConfig.Instance;
            var vc = cfg?.vehicles?.FirstOrDefault(v => v.id == s.currentVehicle);
            int trackCount = (vc != null) ? vc.upgradeTracks.Count : 2;
            for (int t = 0; t < trackCount; t++)
            {
                string tName = (vc != null && t < vc.upgradeTracks.Count) ? vc.upgradeTracks[t].trackName : $"改装线{t}";
                int flat = (int)s.currentVehicle * 2 + t;
                int lvl = s.upgradeLevels[flat];
                if (vs.GetUpgradeCost(s, s.currentVehicle, t, out int pc, out int fc, out bool mx))
                {
                    GUI.Label(new Rect(x, y, dataW, 18f),
                        $"{tName}  Lv{lvl}  下一级: {pc}件 + {fc}蓝", _text);
                }
                else
                {
                    GUI.Label(new Rect(x, y, dataW, 18f),
                        $"{tName}  Lv{lvl}  {(mx ? "已满级" : "—")}", _text);
                }
                // 等级条
                var track = new Rect(x, y + 18f, dataW, 8f);
                Fill(track, new Color(0.08f, 0.11f, 0.15f, 0.95f));
                int maxLv = (vc != null && t < vc.upgradeTracks.Count) ? vc.upgradeTracks[t].maxLevel : 2;
                float ratio = maxLv > 0 ? (float)lvl / maxLv : 0f;
                Fill(new Rect(track.x + 1f, track.y + 1f, (track.width - 2f) * ratio, track.height - 2f), new Color(0.13f, 0.83f, 0.93f));
                y += 34f;
            }

            float bonus = vs.GetSpeedBonus(s, s.currentVehicle, 0) + vs.GetSpeedBonus(s, s.currentVehicle, 1);
            GUI.Label(new Rect(x, y, dataW, 18f), $"速度加成合计 +{bonus:F2}", _text); y += 22f;
            GUI.Label(new Rect(x, y, dataW, 16f), $"零件 {s.parts}    蓝图碎片 {s.blueprintFragments}", _small); y += 18f;
            GUI.Label(new Rect(x, y, dataW, 16f), "[U] 升级线0  [P] +零件  [B] +蓝图", _small); y += 18f;
            GUI.Label(new Rect(x, y, dataW, 16f), "[V] 关闭面板", _small);
        }

        private void Border(Rect p)
        {
            var b = new Color(0.13f, 0.83f, 0.93f, 0.85f);
            Fill(new Rect(p.x, p.y, p.width, 1f), b);
            Fill(new Rect(p.x, p.yMax - 1f, p.width, 1f), b);
            Fill(new Rect(p.x, p.y, 1f, p.height), b);
            Fill(new Rect(p.xMax - 1f, p.y, 1f, p.height), b);
            Fill(new Rect(p.x, p.y, 46f, 2f), new Color(1f, 0.69f, 0f, 0.95f));
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            var font = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Courier New", "Menlo" }, 13);
            _title = new GUIStyle { font = font, fontSize = 14, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.13f, 0.83f, 0.93f) } };
            _text = new GUIStyle { font = font, fontSize = 12, normal = { textColor = new Color(0.9f, 0.95f, 1f) } };
            _small = new GUIStyle { font = font, fontSize = 11, normal = { textColor = new Color(0.7f, 0.78f, 0.88f) } };
            _stylesReady = true;
        }

        private void Fill(Rect r, Color c)
        {
            var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, _white); GUI.color = old;
        }

        private static string VehicleName(VehicleId id)
        {
            switch (id) { case VehicleId.Van: return "厢式货车"; case VehicleId.Motorcycle: return "摩托"; default: return "皮卡"; }
        }

#if UNITY_EDITOR
        private void LoadFrames()
        {
            string[] paths = {
                "Assets/Art/Sprites/Vehicles/pickup_0.png",
                "Assets/Art/Sprites/Vehicles/van_0.png",
                "Assets/Art/Sprites/Vehicles/moto_0.png"
            };
            for (int i = 0; i < 3; i++)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(paths[i]);
                if (s != null && s.texture != null) s.texture.filterMode = FilterMode.Point;
                _vehFrames[i] = s;
            }
        }
#endif
    }
}
