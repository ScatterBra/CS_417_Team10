using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// Shows each tutorial once, in world space next to the object it explains
    /// (rubric: Pop-up Tutorials). The popup prefab should be a world-space Canvas with a TMP_Text.
    /// </summary>
    public class PWR_TutorialManager : MonoBehaviour
    {
        public static PWR_TutorialManager Instance { get; private set; }

        public GameObject popupPrefab;
        public float displaySeconds = 8f;

        private readonly HashSet<string> shown = new HashSet<string>();

        public int ShownCount => shown.Count;

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
            if (string.IsNullOrEmpty(id) || shown.Contains(id) || popupPrefab == null || anchor == null) return false;
            shown.Add(id);

            var popup = Instantiate(popupPrefab, anchor.position + offset, Quaternion.identity, anchor);
            var text = popup.GetComponentInChildren<TMP_Text>();
            if (text) text.text = message;
            if (!popup.GetComponent<PWR_Billboard>()) popup.AddComponent<PWR_Billboard>();
            if (displaySeconds > 0f) Destroy(popup, displaySeconds);
            return true;
        }

        public void ResetAll() => shown.Clear();
    }
}
