using TMPro;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>Floating text that rises and fades: short pops ("+1 Coal") and error explanations
    /// ("Needs Coal to run. Break a Wooden Crate with the Wrench to get Coal.").</summary>
    public class PWR_Feedback : MonoBehaviour
    {
        public static readonly Color Good = new Color(0.55f, 1f, 0.55f);
        public static readonly Color Bad = new Color(1f, 0.5f, 0.45f);
        public static readonly Color Info = new Color(1f, 0.9f, 0.6f);

        private const float ShortLifetime = 1.6f;
        private const int ShortLength = 28;

        private TMP_Text text;
        private float age;
        private float lifetime;
        private float riseSpeed;
        private Color baseColor;

        public static void Show(Vector3 position, string message, Color color)
        {
            Debug.Log($"[PWR] {message}");
            // Short pops ("+1 Coal") rise and fade quickly; explanations get a wider box and time to read.
            bool isLong = message.Length > ShortLength;
            int lines = isLong ? Mathf.CeilToInt(message.Length / 40f) + message.Split('\n').Length - 1 : 1;
            var size = isLong ? new Vector2(780, 24 + lines * 40) : new Vector2(500, 80);
            var canvas = PWR_UIFactory.CreateWorldCanvas("PWR_Feedback", null, size, 0.0016f, false);
            canvas.transform.position = position;
            canvas.sortingOrder = 50;
            var fb = canvas.gameObject.AddComponent<PWR_Feedback>();
            fb.lifetime = isLong ? Mathf.Clamp(message.Length * 0.06f, 2.5f, 6f) : ShortLifetime;
            fb.riseSpeed = isLong ? 0.06f : 0.25f;
            fb.text = PWR_UIFactory.CreateText(canvas.transform, message, isLong ? 32 : 40, TextAlignmentOptions.Center, 0);
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
            transform.position += Vector3.up * (riseSpeed * Time.deltaTime);
            float alpha = 1f - Mathf.Clamp01((age - lifetime * 0.6f) / (lifetime * 0.4f));
            text.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            if (age >= lifetime) Destroy(gameObject);
        }
    }
}
