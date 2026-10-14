using TMPro;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>Short floating text ("+1 Coal", "Needs Coal") that rises and fades.</summary>
    public class PWR_Feedback : MonoBehaviour
    {
        public static readonly Color Good = new Color(0.55f, 1f, 0.55f);
        public static readonly Color Bad = new Color(1f, 0.5f, 0.45f);
        public static readonly Color Info = new Color(1f, 0.9f, 0.6f);

        private const float Lifetime = 1.6f;

        private TMP_Text text;
        private float age;
        private Color baseColor;

        public static void Show(Vector3 position, string message, Color color)
        {
            Debug.Log($"[PWR] {message}");
            var canvas = PWR_UIFactory.CreateWorldCanvas("PWR_Feedback", null, new Vector2(500, 80), 0.0016f, false);
            canvas.transform.position = position;
            canvas.sortingOrder = 50;
            var fb = canvas.gameObject.AddComponent<PWR_Feedback>();
            fb.text = PWR_UIFactory.CreateText(canvas.transform, message, 40, TextAlignmentOptions.Center, 0);
            fb.text.fontStyle = FontStyles.Bold;
            fb.text.color = color;
            fb.text.outlineWidth = 0.2f;
            fb.text.outlineColor = Color.black;
            fb.baseColor = color;
            canvas.gameObject.AddComponent<PWR_Billboard>().lockVertical = false;
        }

        private void Update()
        {
            age += Time.deltaTime;
            transform.position += Vector3.up * (0.25f * Time.deltaTime);
            float alpha = 1f - Mathf.Clamp01((age - Lifetime * 0.6f) / (Lifetime * 0.4f));
            text.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            if (age >= Lifetime) Destroy(gameObject);
        }
    }
}
