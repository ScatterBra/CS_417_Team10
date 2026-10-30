using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

// Presentation only: no resource, progression, receiver or retry state is changed here.
// Installed on the integrated HUD, not on standalone or recording scenes.
[DisallowMultipleComponent]
public sealed class DEF_IntegratedXRPresentation : MonoBehaviour
{
    DEF_HUD hud;
    XROrigin origin;
    Camera boundCamera;
    Canvas popup;
    float nextScan;
    bool configured;
    readonly System.Collections.Generic.List<Canvas> headCanvases = new();
    readonly System.Collections.Generic.List<GameObject> feedbackCards = new();
    readonly System.Collections.Generic.List<TMP_Text> feedbackTexts = new();

    public static bool IsIntegratedScene(Scene scene)
    {
        foreach (var clinic in FindObjectsByType<DEF_ClinicTraining>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (clinic.gameObject.scene == scene) return true;
        return false;
    }

    void Awake() { hud = GetComponent<DEF_HUD>(); }

    void LateUpdate()
    {
        for (int i = 0; i < feedbackCards.Count; i++)
            if (feedbackCards[i] != null && feedbackTexts[i] != null)
                feedbackCards[i].SetActive(feedbackTexts[i].gameObject.activeSelf && !string.IsNullOrEmpty(feedbackTexts[i].text));
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + .25f;
        BindPresentation();
    }

    bool BindPresentation()
    {
        // The Defense input owns the active XR/desktop camera decision, including late XR startup.
        var input = DEF_VRInput.Instance;
        if (input == null || input.gameObject.scene != gameObject.scene || input.xrCamera == null)
            return false;
        if (!configured && !DEF_VRInput.Active) return false;
        var camera = DEF_VRInput.Active ? input.xrCamera : Camera.main;
        if (camera == null) return false;
        origin = input.xrCamera.GetComponentInParent<XROrigin>();
        if (origin == null || hud == null) return false;

        bool justConfigured = !configured;
        if (!configured)
        {
            var canvas = hud.GetComponent<Canvas>();
            if (canvas == null) return false;
            ConfigureHeadCanvas(canvas, camera, 1.6f, new Vector2(1600, 900));
            headCanvases.Add(canvas);
            // Keep Pause clear of every yard's shared upper-right objective and introduction cards.
            if (hud.pauseButton != null)
                hud.pauseButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(40, -138);
            if (hud.ammoText != null)
                ((RectTransform)hud.ammoText.transform.parent).anchoredPosition = new Vector2(40, -220);
            var popupObject = new GameObject("Integrated Defense shop", typeof(RectTransform), typeof(Canvas));
            SceneManager.MoveGameObjectToScene(popupObject, gameObject.scene);
            popup = popupObject.GetComponent<Canvas>();
            popup.renderMode = RenderMode.WorldSpace;
            popup.worldCamera = camera;
            var popupRect = (RectTransform)popup.transform;
            popupRect.sizeDelta = new Vector2(1600, 900);
            popupRect.localScale = Vector3.one * .001f;
            EnsureTrackedUI(popup, camera);
            // Keep persistent HUD at the head; interactive shop stays still during selection.
            if (hud.shopPanel != null) hud.shopPanel.transform.SetParent(popup.transform, false);
            // Keep selected-item status and its Cancel button head-attached while placing.

            foreach (var presenter in FindObjectsByType<DEF_ProgressionPresenter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (presenter.gameObject.scene != gameObject.scene || presenter.goalText == null) continue;
                var presentation = presenter.goalText.GetComponentInParent<Canvas>();
                if (presentation == null || presentation == canvas) continue;
                ConfigureHeadCanvas(presentation, camera, 1.55f, new Vector2(1600, 900));
                headCanvases.Add(presentation);
                AddReadableCard(presentation, presenter.goalText, "Current goal", new Vector2(420, 260), new Vector2(640, 250), 28);
                AddReadableCard(presentation, presenter.introductionText, "Introduction", new Vector2(440, -40), new Vector2(600, 320), 24);
                AddReadableCard(presentation, presenter.failureText, "Feedback", new Vector2(0, -300), new Vector2(1100, 200), 24);
            }
            boundCamera = camera;
            configured = true;
        }
        else if (boundCamera != camera)
        {
            // Preserve local panel layout if the runtime replaces its tracked camera.
            foreach (var canvas in headCanvases)
                if (canvas != null) canvas.transform.SetParent(camera.transform, false);
            boundCamera = camera;
        }

        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (canvas.gameObject.scene == gameObject.scene && canvas.renderMode == RenderMode.WorldSpace)
            {
                // Passive Power/Clinic labels intentionally have no raycaster. Do not make
                // their backing images steal build/interaction rays.
                if (canvas.worldCamera != camera) canvas.worldCamera = camera;
                if (canvas.GetComponent<GraphicRaycaster>() != null ||
                    canvas.GetComponent<TrackedDeviceGraphicRaycaster>() != null ||
                    canvas.GetComponentInChildren<Selectable>(true) != null)
                    EnsureTrackedUI(canvas, camera);
            }
        if (justConfigured && hud.shopPanel != null && hud.shopPanel.activeSelf && !PlaceShop())
            hud.CloseMenus();
        return true;
    }

    public bool TryPositionShop()
    {
        // Desktop keeps the scene's normal shop. XR initialization failure must not strand the user.
        if (!configured && !DEF_VRInput.Active) return true;
        if (!BindPresentation()) { hud?.Notify("Shop is waiting for the headset camera. Try A again."); return false; }
        return PlaceShop();
    }

    bool PlaceShop()
    {
        var forward = Vector3.ProjectOnPlane(boundCamera.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < .5f) forward = Vector3.ProjectOnPlane(origin.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < .5f) forward = Vector3.forward;
        var position = boundCamera.transform.position + forward * 1.35f - Vector3.up * .08f;
        var rotation = Quaternion.LookRotation(forward, Vector3.up);
        foreach (var collider in Physics.OverlapBox(position, new Vector3(.8f, .45f, .03f), rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (BlocksShop(collider)) return ShopBlocked();
        foreach (var hit in Physics.RaycastAll(boundCamera.transform.position, forward, 1.35f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (BlocksShop(hit.collider)) return ShopBlocked();
        popup.transform.SetPositionAndRotation(position, rotation);
        return true;
    }

    bool BlocksShop(Collider collider) => !collider.transform.IsChildOf(origin.transform) && collider.GetComponentInParent<SCV_CraftedItem>() == null;
    bool ShopBlocked() { hud.Notify("Shop needs clear space. Face the open walkway and press A again."); return false; }

    static void ConfigureHeadCanvas(Canvas canvas, Camera camera, float distance, Vector2 size)
    {
        canvas.renderMode = RenderMode.WorldSpace;
        var rect = (RectTransform)canvas.transform;
        rect.SetParent(camera.transform, false);
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        rect.localPosition = new Vector3(0, 0, distance);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one * .001f;
        EnsureTrackedUI(canvas, camera);
    }

    static void EnsureTrackedUI(Canvas canvas, Camera camera)
    {
        if (canvas.worldCamera != camera) canvas.worldCamera = camera;
        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
    }

    void AddReadableCard(Canvas canvas, TMP_Text text, string name, Vector2 position, Vector2 size, float fontSize)
    {
        if (text == null) return;
        var card = new GameObject("Defense " + name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)card.transform;
        rect.SetParent(canvas.transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var image = card.GetComponent<Image>();
        image.color = new Color(.025f, .045f, .06f, .97f);
        image.raycastTarget = false;
        text.transform.SetParent(rect, false);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(24, 12);
        text.rectTransform.offsetMax = new Vector2(-24, -12);
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        feedbackCards.Add(card);
        feedbackTexts.Add(text);
    }

    void OnDestroy() { if (popup != null) Destroy(popup.gameObject); }
}
