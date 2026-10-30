using System;
using UnityEngine;

// Presentation duration only. Never use this to advance or unlock gameplay.
public static class DEF_UIPresentationPolicy
{
    public static float ReadSeconds(string text)
    {
        int words = string.IsNullOrWhiteSpace(text) ? 0 :
            text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length;
        return Mathf.Max(5f, 2f + words / 3f);
    }
}
