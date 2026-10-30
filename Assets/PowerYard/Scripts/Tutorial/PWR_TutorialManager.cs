using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// Shows each tutorial once: beside its object standalone, or in one readable integrated HUD lane
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
        private struct Notice
        {
            public GameObject popup;
            public float seconds;
            public string message;
            public Vector3 source;
            public bool feedback;
        }
        private readonly Queue<Notice> notices = new Queue<Notice>();
        private GameObject activeNotice;
        private Notice pendingFeedback;
        private Notice activeEntry;
        public bool IntegratedPresentation => integrated;
        private float noticeRemaining;
        private bool integrated;

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
            integrated = DEF_IntegratedXRPresentation.IsIntegratedScene(gameObject.scene);
        }

        private void OnEnable() => PWR_Progression.StageChanged += OnStageChanged;
        private void OnDisable() => PWR_Progression.StageChanged -= OnStageChanged;

        private void OnStageChanged(PWR_Stage stage)
        {
            // A genuine completed stage makes an old machine failure obsolete.
            if (activeEntry.feedback && activeNotice != null) { activeNotice.SetActive(false); Destroy(activeNotice); activeNotice = null; }
            if (pendingFeedback.popup != null) Destroy(pendingFeedback.popup);
            pendingFeedback = default;
        }

        public void ClearFeedbackAt(Vector3 position)
        {
            // Successful use at the same machine clears its stale denial, not another tutorial.
            if (activeEntry.feedback && activeNotice != null && (activeEntry.source - position).sqrMagnitude < .0625f)
            { activeNotice.SetActive(false); Destroy(activeNotice); activeNotice = null; }
            if (pendingFeedback.popup != null && (pendingFeedback.source - position).sqrMagnitude < .0625f)
            { Destroy(pendingFeedback.popup); pendingFeedback = default; }
        }

        public void ShowFeedbackNotice(string message, Vector3 position)
        {
            ShowMessage(message, transform, Vector3.zero, 0f, true, position);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (activeNotice != null) Destroy(activeNotice);
            if (pendingFeedback.popup != null) Destroy(pendingFeedback.popup);
            while (notices.Count > 0)
            {
                var notice = notices.Dequeue();
                if (notice.popup != null) Destroy(notice.popup);
            }
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

        /// <summary>Standalone: beside the anchor, lifetime &lt;= 0 persists. Integrated: queued readable notice.</summary>
        public GameObject ShowMessage(string message, Transform anchor, Vector3 offset, float lifetime,
            bool feedback = false, Vector3 source = default)
        {
            if (integrated && feedback)
            {
                if (activeNotice != null && activeEntry.feedback && activeEntry.message == message &&
                    (activeEntry.source - source).sqrMagnitude < .0625f) return activeNotice;
                if (pendingFeedback.popup != null && pendingFeedback.message == message &&
                    (pendingFeedback.source - source).sqrMagnitude < .0625f) return pendingFeedback.popup;
            }
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
                PWR_UIFactory.CreatePanel(canvas.transform, integrated ? new Color(.015f, .02f, .025f, .94f) : new Color(0.05f, 0.08f, 0.12f, 0.92f));
                var text = PWR_UIFactory.CreateText(canvas.transform, message, 26, TextAlignmentOptions.Center, 12);
                text.color = new Color(1f, 0.95f, 0.8f);
                popup = canvas.gameObject;
            }
            var follow = popup.AddComponent<PWR_TutorialPopup>();
            follow.anchor = anchor;
            follow.offset = offset;
            follow.LateUpdate();
            if (!popup.GetComponent<PWR_Billboard>()) popup.AddComponent<PWR_Billboard>();
            if (integrated)
            {
                // One readable dark card at a time; never stack tutorial panels over the menu.
                // Its lifetime starts when shown, not when another card is still occupying the lane.
                follow.enabled = false;
                var billboard = popup.GetComponent<PWR_Billboard>();
                if (billboard != null) billboard.enabled = false;
                popup.SetActive(false);
                var notice = new Notice { popup = popup, seconds = Mathf.Max(lifetime, DEF_UIPresentationPolicy.ReadSeconds(message)),
                    message = message, source = source, feedback = feedback };
                if (feedback)
                {
                    // Keep only the latest waiting machine explanation; repeat input cannot flood the queue.
                    if (pendingFeedback.popup != null) Destroy(pendingFeedback.popup);
                    pendingFeedback = notice;
                }
                else notices.Enqueue(notice);
            }
            else if (lifetime > 0f) Destroy(popup, lifetime);
            return popup;
        }

        private void Update()
        {
            if (!integrated) return;
            var cam = Camera.main;
            var defense = DEF_VRInput.Instance;
            if (defense != null && defense.gameObject.scene == gameObject.scene && DEF_VRInput.Active && defense.xrCamera != null)
                cam = defense.xrCamera;
            bool visible = cam != null && PWR_AreaGate.PlayerInside &&
                !(PWR_WristPanel.Instance != null && PWR_WristPanel.Instance.IsXRMenuOpen);
            if (!visible)
            {
                if (activeNotice != null) activeNotice.SetActive(false);
                return; // Reading time is preserved while outside Power or using the Y menu.
            }
            // Finite one-time unlock instructions cannot be starved by repeated machine input.
            while (activeNotice == null && notices.Count > 0)
            {
                activeEntry = notices.Dequeue();
                activeNotice = activeEntry.popup;
                noticeRemaining = activeEntry.seconds;
            }
            if (activeNotice == null && pendingFeedback.popup != null)
            {
                activeEntry = pendingFeedback;
                pendingFeedback = default;
                activeNotice = activeEntry.popup;
                noticeRemaining = activeEntry.seconds;
            }
            if (activeNotice == null) return;
            activeNotice.SetActive(true);
            var canvas = activeNotice.GetComponent<Canvas>();
            if (canvas != null) canvas.worldCamera = cam;
            var rect = activeNotice.GetComponent<RectTransform>();
            float halfHeight = rect != null ? rect.rect.height * Mathf.Abs(rect.lossyScale.y) * .5f : .2f;
            // The top edge stays below the persistent upper-right objective, regardless of text height.
            activeNotice.transform.SetPositionAndRotation(
                cam.transform.TransformPoint(new Vector3(.65f, .105f - halfHeight, 1.25f)), cam.transform.rotation);
            noticeRemaining -= Time.unscaledDeltaTime;
            if (noticeRemaining <= 0f)
            {
                activeNotice.SetActive(false);
                Destroy(activeNotice);
                activeNotice = null;
            }
        }

        public void ResetAll()
        {
            shown.Clear();
            order.Clear();
        }
    }
}
