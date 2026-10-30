using System.Collections.Generic;
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

        private static readonly List<PWR_Feedback> crossYardCards = new List<PWR_Feedback>();
        private string explanation;
        private Vector3 sourcePosition;
        private bool crossYard;
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
            var tutorials = PWR_TutorialManager.Instance;
            if (tutorials != null && tutorials.IntegratedPresentation)
            {
                for (int i = crossYardCards.Count - 1; i >= 0; i--)
                {
                    var card = crossYardCards[i];
                    if (card == null) { crossYardCards.RemoveAt(i); continue; }
                    if ((card.sourcePosition - position).sqrMagnitude >= .0625f) continue;
                    if (color == Good || (isLong && card.explanation != message))
                    { card.gameObject.SetActive(false); Destroy(card.gameObject); crossYardCards.RemoveAt(i); }
                    else if (isLong && card.explanation == message) return;
                }
                if (color == Good) tutorials.ClearFeedbackAt(position);
                if (isLong && color != Good && PWR_AreaGate.PlayerInside)
                {
                    tutorials.ShowFeedbackNotice(message, position);
                    return;
                }
            }
            int lines = isLong ? Mathf.CeilToInt(message.Length / 40f) + message.Split('\n').Length - 1 : 1;
            var size = isLong ? new Vector2(780, 24 + lines * 40) : new Vector2(500, 80);
            var canvas = PWR_UIFactory.CreateWorldCanvas("PWR_Feedback", null, size, 0.0016f, false);
            // Cross-yard integrations (for example the Defense cell dock) must explain errors
            // where they occur, never queue them in an invisible Power-only HUD.
            bool crossYardExplanation = isLong && color != Good && tutorials != null && tutorials.IntegratedPresentation;
            if (crossYardExplanation)
                PWR_UIFactory.CreatePanel(canvas.transform, new Color(.015f, .02f, .025f, .94f)).raycastTarget = false;
            canvas.transform.position = position;
            canvas.sortingOrder = 50;
            var fb = canvas.gameObject.AddComponent<PWR_Feedback>();
            fb.lifetime = isLong ? Mathf.Clamp(message.Length * 0.06f, 2.5f, 6f) : ShortLifetime;
            if (crossYardExplanation) fb.lifetime = Mathf.Max(fb.lifetime, DEF_UIPresentationPolicy.ReadSeconds(message));
            if (crossYardExplanation)
            {
                fb.crossYard = true; fb.explanation = message; fb.sourcePosition = position;
                fb.lifetime += .35f; crossYardCards.Add(fb);
            }
            fb.riseSpeed = crossYardExplanation ? 0f : isLong ? 0.06f : 0.25f;
            fb.text = PWR_UIFactory.CreateText(canvas.transform, message, isLong ? 32 : 40, TextAlignmentOptions.Center, 0);
            fb.text.fontStyle = FontStyles.Bold;
            fb.text.color = color;
            fb.text.outlineWidth = 0.2f;
            fb.text.outlineColor = Color.black;
            fb.baseColor = color;
            canvas.gameObject.AddComponent<PWR_Billboard>().lockVertical = false;
        }

        private void OnDestroy() { crossYardCards.Remove(this); }

        private void Update()
        {
            age += Time.deltaTime;
            transform.position += Vector3.up * (riseSpeed * Time.deltaTime);
            float alpha = crossYard
                ? 1f - Mathf.Clamp01((age - (lifetime - .35f)) / .35f)
                : 1f - Mathf.Clamp01((age - lifetime * 0.6f) / (lifetime * 0.4f));
            text.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            if (age >= lifetime) Destroy(gameObject);
        }
    }
}
