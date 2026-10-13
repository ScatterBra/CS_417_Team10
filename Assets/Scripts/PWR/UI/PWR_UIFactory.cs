using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Deadzone.Power
{
    /// <summary>Builds the station's small UI pieces in code so no UI prefabs are needed.</summary>
    public static class PWR_UIFactory
    {
        public static readonly Color PanelColor = new Color(0.06f, 0.07f, 0.09f, 0.88f);
        public static readonly Color ButtonColor = new Color(0.18f, 0.22f, 0.28f, 1f);
        public static readonly Color AccentColor = new Color(1f, 0.78f, 0.25f, 1f);

        /// <summary>World-space canvas. sizePx is in canvas units; metersPerUnit scales it into the world.</summary>
        public static Canvas CreateWorldCanvas(string name, Transform parent, Vector2 sizePx, float metersPerUnit,
            bool interactive)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = sizePx;
            rt.localScale = Vector3.one * metersPerUnit;
            if (interactive)
            {
                go.AddComponent<GraphicRaycaster>();
                go.AddComponent<TrackedDeviceGraphicRaycaster>();
            }
            return canvas;
        }

        public static Canvas CreateOverlayCanvas(string name, int sortingOrder = 10)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static Image CreatePanel(Transform parent, Color color)
        {
            var go = new GameObject("Panel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static TMP_Text CreateText(Transform parent, string text, float fontSize,
            TextAlignmentOptions align = TextAlignmentOptions.TopLeft, float margin = 8f)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            Stretch(rt);
            rt.offsetMin = new Vector2(margin, margin);
            rt.offsetMax = new Vector2(-margin, -margin);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        /// <summary>Vertical stack container filling its parent.</summary>
        public static RectTransform CreateVerticalLayout(Transform parent, float spacing, int padding)
        {
            var go = new GameObject("Layout", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            Stretch(rt);
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return rt;
        }

        public static TMP_Text CreateLayoutText(Transform layout, string text, float fontSize, float height)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(layout, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static Button CreateButton(Transform layout, string label, float height, float fontSize, UnityAction onClick)
        {
            var go = new GameObject("Button_" + label, typeof(RectTransform));
            go.transform.SetParent(layout, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            var img = go.AddComponent<Image>();
            img.color = ButtonColor;
            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.35f, 0.42f, 0.5f, 1f);
            colors.pressedColor = AccentColor;
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(onClick);
            var text = CreateText(go.transform, label, fontSize, TextAlignmentOptions.Center, 4f);
            text.textWrappingMode = TextWrappingModes.Normal;
            return button;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
