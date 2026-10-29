using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// Shows each tutorial once, in world space next to the object it explains
    /// (rubric: Pop-up Tutorials). Popups are built in code; a prefab is optional.
    /// </summary>
    public class PWR_TutorialManager : MonoBehaviour
    {
        public static PWR_TutorialManager Instance { get; private set; }

        [Tooltip("Optional: a world-space Canvas with a TMP_Text. Built in code when empty.")]
        public GameObject popupPrefab;
        public float displaySeconds = 10f;
        public float popupWidth = 0.7f;

        public static event Action<string> TutorialShown;

        private readonly Dictionary<string, GameObject> popups = new Dictionary<string, GameObject>();
        private readonly HashSet<string> shown = new HashSet<string>();
        private readonly List<string> order = new List<string>();

        public int ShownCount => shown.Count;
        public IReadOnlyList<string> ShownOrder => order;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool HasShown(string id) => shown.Contains(id);

        /// <summary>Shows the popup if this id has not been shown yet. Returns true if shown.</summary>
        public bool ShowOnce(string id, string message, Transform anchor, Vector3 offset)
        {
            if (string.IsNullOrEmpty(id) || shown.Contains(id) || anchor == null) return false;
            shown.Add(id);
            order.Add(id);
            popups[id] = ShowMessage(message, anchor, offset, displaySeconds);
            Debug.Log($"[PWR] Tutorial '{id}': {message}");
            TutorialShown?.Invoke(id);
            return true;
        }

        /// <summary>Dismisses a visible tutorial without resetting its one-time history.</summary>
        public void Dismiss(string id)
        {
            if (!popups.TryGetValue(id, out var popup)) return;
            if (popup != null) { popup.SetActive(false); Destroy(popup); }
            popups.Remove(id);
        }

        /// <summary>Shows a popup next to the anchor. lifetime &lt;= 0 keeps it up.</summary>
        public GameObject ShowMessage(string message, Transform anchor, Vector3 offset, float lifetime)
        {
            GameObject popup;
            if (popupPrefab != null)
            {
                popup = Instantiate(popupPrefab);
                var t = popup.GetComponentInChildren<TMP_Text>();
                if (t != null) t.text = message;
            }
            else
            {
                const float unitsWide = 560f;
                int lines = 2 + message.Length / 34 + message.Split('\n').Length;
                var canvas = PWR_UIFactory.CreateWorldCanvas("PWR_Tutorial", null,
                    new Vector2(unitsWide, 30 + lines * 30), popupWidth / unitsWide, false);
                canvas.sortingOrder = 40;
                PWR_UIFactory.CreatePanel(canvas.transform, new Color(0.05f, 0.08f, 0.12f, 0.92f));
                var text = PWR_UIFactory.CreateText(canvas.transform, message, 26, TextAlignmentOptions.Center, 12);
                text.color = new Color(1f, 0.95f, 0.8f);
                popup = canvas.gameObject;
            }
            var follow = popup.AddComponent<PWR_TutorialPopup>();
            follow.anchor = anchor;
            follow.offset = offset;
            follow.LateUpdate();
            if (!popup.GetComponent<PWR_Billboard>()) popup.AddComponent<PWR_Billboard>();
            if (lifetime > 0f) Destroy(popup, lifetime);
            return popup;
        }

        public void ResetAll()
        {
            shown.Clear();
            order.Clear();
        }
    }
}
