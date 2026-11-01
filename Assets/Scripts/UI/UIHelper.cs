using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DesktopDoomsday.Window;

namespace DesktopDoomsday.UI
{
    /// <summary>
    /// uGUI 运行时构建助手（Batch 6 正式 UI 基础设施）。
    /// 不依赖任何预制体：Bootstrapper 在 Awake 中自动挂 UI 组件，所有面板在代码里动态创建。
    /// 视觉对齐 SPEC 9.3/9.4：暗玻璃面板 + 霓虹青描边 + 等宽文本 + 霓虹条形进度。
    /// 交互模式与点击穿透联动：任何需要点击的面板显示时调用 EnterInteractive（关闭穿透，桌面宠物变可交互），
    /// 隐藏时 ExitInteractive（恢复穿透，回到不挡桌面的宠物态）。
    /// </summary>
    public static class UIHelper
    {
        public const string CanvasName = "DDCanvas";

        private static Canvas _canvas;
        private static int _interactiveCount;

        // ---- 调色板（SPEC 9.3，集中管理）----
        public static class Palette
        {
            public static Color BG = new Color(0.043f, 0.055f, 0.078f, 0.92f);
            public static Color Panel = new Color(0.071f, 0.094f, 0.149f, 0.95f);
            public static Color Cyan = new Color(0.133f, 0.827f, 0.933f, 1f);
            public static Color Amber = new Color(1f, 0.690f, 0f, 1f);
            public static Color Green = new Color(0.204f, 0.831f, 0.600f, 1f);
            public static Color Red = new Color(0.957f, 0.247f, 0.369f, 1f);
            public static Color Text = new Color(0.898f, 0.925f, 0.961f, 1f);
            public static Color Sub = new Color(0.541f, 0.592f, 0.659f, 1f);
        }

        /// <summary>获取/创建全屏 Overlay 画布（透明窗下 Overlay 渲染于最前，无需相机引用）。</summary>
        public static Canvas GetCanvas()
        {
            if (_canvas != null) return _canvas;
            var go = GameObject.Find(CanvasName);
            if (go != null) { _canvas = go.GetComponent<Canvas>(); if (_canvas != null) return _canvas; }
            go = new GameObject(CanvasName);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            go.AddComponent<CanvasScaler>();
            EnsureEventSystem();
            return _canvas;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;
            var go = new GameObject("DDEventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        // ---- 交互态：计数驱动点击穿透 ----
        public static void EnterInteractive()
        {
            _interactiveCount++;
            ApplyClickThrough();
        }

        public static void ExitInteractive()
        {
            _interactiveCount = Mathf.Max(0, _interactiveCount - 1);
            ApplyClickThrough();
        }

        private static void ApplyClickThrough()
        {
            var tw = TransparentWindow.Instance;
            if (tw != null) tw.SetClickThrough(_interactiveCount <= 0);
        }

        // ---- 控件构建器 ----

        /// <summary>暗玻璃面板，左下锚定（x,y 为屏幕像素偏移），可选霓虹描边。</summary>
        public static GameObject Panel(string name, float x, float y, float w, float h, Color bg, Color border)
        {
            var parent = GetCanvas().transform;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            var img = go.AddComponent<Image>();
            img.color = bg;
            if (border.a > 0f)
            {
                var ol = go.AddComponent<Outline>();
                ol.effectColor = border;
                ol.effectDistance = new Vector2(1f, 1f);
            }
            return go;
        }

        public static Text Label(GameObject parent, string txt, float x, float y, float w, float h,
            int size, Color col, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var go = new GameObject("lbl");
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            var t = go.AddComponent<Text>();
            t.text = txt;
            t.fontSize = size;
            t.color = col;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false; // 标签不拦截按钮点击
            return t;
        }

        public static Button Button(GameObject parent, string label, float x, float y, float w, float h, System.Action onClick)
        {
            var go = new GameObject("btn");
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.07f, 0.12f, 0.16f, 0.95f);
            var ol = go.AddComponent<Outline>();
            ol.effectColor = Palette.Cyan;
            ol.effectDistance = new Vector2(1f, 1f);
            Label(go, label, 0, 0, w, h, 13, Palette.Text, TextAnchor.MiddleCenter);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = new ColorBlock
            {
                normalColor = img.color,
                highlightedColor = new Color(0.13f, 0.83f, 0.93f, 0.45f),
                pressedColor = new Color(0.13f, 0.83f, 0.93f, 0.65f),
                disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f),
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };
            btn.colors = cb;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        /// <summary>霓虹条形进度：返回 fill Image，调用方设置 color 与 sizeDelta.width 表示比例。</summary>
        public static Image NeonBar(GameObject parent, float x, float y, float w, float h, out float innerW)
        {
            var bg = new GameObject("barbg");
            bg.transform.SetParent(parent.transform, false);
            var rt = bg.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            var img = bg.AddComponent<Image>();
            img.color = new Color(0.08f, 0.11f, 0.15f, 0.95f);

            var fill = new GameObject("barfill");
            fill.transform.SetParent(bg.transform, false);
            var frt = fill.AddComponent<RectTransform>();
            frt.anchorMin = new Vector2(0f, 0f);
            frt.anchorMax = new Vector2(0f, 0f);
            frt.pivot = new Vector2(0f, 0f);
            frt.anchoredPosition = new Vector2(1f, 1f);
            frt.sizeDelta = new Vector2(w - 2f, h - 2f);
            var fimg = fill.AddComponent<Image>();
            fimg.color = Palette.Green;
            innerW = w - 2f;
            return fimg;
        }

        public static void DestroyChildren(GameObject go)
        {
            if (go == null) return;
            var list = new System.Collections.Generic.List<Transform>();
            foreach (Transform t in go.transform) list.Add(t);
            foreach (var t in list) Object.Destroy(t.gameObject);
        }
    }
}
