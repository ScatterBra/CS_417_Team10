using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Presentation duration only. Never use this to advance or unlock gameplay.
public static class DEF_UIPresentationPolicy
{
    public static float ReadSeconds(string text)
    {
        int words = string.IsNullOrWhiteSpace(text) ? 0 :
            text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length;
        return Mathf.Max(5f, 2f + words / 3f);
    }
    // Geometry avoids dependence on a font asset containing the Unicode checkmark glyph.
    // This is passive presentation only: callers supply their existing authoritative completion cue.
    public static void SetCompletionMark(TMP_Text text, bool completed)
    {
        if (text == null) return;
        const string name = "Completion check geometry";
        var existing = text.transform.Find(name);
        if (existing == null && completed)
        {
            var root = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            rect.SetParent(text.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.sizeDelta = new Vector2(28, 28);
            rect.anchoredPosition = new Vector2(-4, -4);
            AddStroke(rect, new Vector2(-6, -2), 12, -45);
            AddStroke(rect, new Vector2(4, 1), 23, 45);
            existing = rect;
        }
        if (existing != null && existing.gameObject.activeSelf != completed)
            existing.gameObject.SetActive(completed);
    }
    static void AddStroke(RectTransform parent, Vector2 position, float length, float angle)
    {
        var go = new GameObject("Check stroke", typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(length, 4);
        rect.localRotation = Quaternion.Euler(0, 0, angle);
        var image = go.GetComponent<Image>();
        image.color = new Color(.55f, 1f, .65f);
        image.raycastTarget = false;
    }
}
