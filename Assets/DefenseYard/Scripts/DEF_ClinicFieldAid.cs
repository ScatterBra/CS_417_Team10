using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using Deadzone.Power;
using TMPro;

// Optional reuse of a paid Clinic Medicine dose. No healing, crafting, combat or mastery gates change.
[DisallowMultipleComponent]
public sealed class DEF_ClinicFieldAid : MonoBehaviour
{
    public const float Duration = 60f, Multiplier = 1.15f;
    DEF_ClinicTraining training;
    DEF_ClinicReadiness readiness;
    DEF_Progression progression;
    ContinuousMoveProvider xrMove;
    PWR_DesktopRig desktopMove;
    bool unlocked, boosting, activating, usingXR, pendingUnlockNotice;
    PWR_PlayerRig movementRig;
    Transform movementOrigin;
    Camera movementCamera;
    int unlockedSession, activeSession, activeAttempt, activeGeneration;
    float startedAt, priorSpeed, lastWritten;
    DEF_ClinicMedicineDelivery delivery;
    Canvas timerCanvas;
    TMP_Text timerText;
    public bool IsQualified => Ready;
    public bool IsBoostActive => boosting;
    public float SecondsRemaining => boosting ? Mathf.Max(0, Duration - (Time.time - startedAt)) : 0;

    void Awake()
    {
        training = GetComponent<DEF_ClinicTraining>();
        readiness = GetComponent<DEF_ClinicReadiness>();
        progression = DEF_Progression.Current;
    }
    void OnEnable()
    {
        if (readiness != null) readiness.Completed += Completed;
        if (readiness != null && progression != null && progression.State.Session > 0)
            readiness.ResumeSession(progression.State.Session, Completed);
    }
    void Completed(DEF_PrerequisiteSource source, int session, string receipt)
    {
        if (source != readiness || progression == null || session != progression.State.Session || string.IsNullOrEmpty(receipt)) return;
        if (unlocked && unlockedSession == session) return;
        unlocked = true; unlockedSession = session;
        pendingUnlockNotice = true;
    }
    public static void CancelForReset(Component owner, bool fullSession)
    {
        foreach (var aid in FindObjectsByType<DEF_ClinicFieldAid>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (aid.gameObject.scene == owner.gameObject.scene)
            { aid.StopBoost(); if (fullSession) { aid.unlocked = false; aid.GetComponent<DEF_ClinicMedicineDelivery>()?.ClearForFullReset(); } }
    }
    bool Ready => this != null && isActiveAndEnabled && unlocked && training != null && training.patient != null &&
        training.ReadyForTreatment && progression != null && progression.isActiveAndEnabled && DEF_Progression.Current == progression && progression.gameObject.scene == gameObject.scene &&
        progression.State.Session == unlockedSession;

    public bool TryActivate()
    {
        if (activating) return false;
        if (!Ready) return Fail("Finish Clinic self-treatment first. Field Aid uses an extra Medicine dose afterwards.");
        delivery = GetComponent<DEF_ClinicMedicineDelivery>();
        if (delivery == null || !delivery.AtDefense()) return Fail("Use FIELD AID at the Defense delivery bay.");
        if (boosting) return Fail("Field Aid is already active. No Medicine spent; wait for its timer to finish.");
        if (!delivery.Delivered) return Fail("No Medicine delivered yet. LOAD MEDICINE at Clinic and SEND TO DEFENSE first.");
        if (!FindMovement()) return Fail("Movement is not ready. No Medicine spent; try again when locomotion is available.");
        float speed = ReadSpeed();
        if (float.IsNaN(speed) || float.IsInfinity(speed) || speed <= 0) return Fail("Movement is unavailable. No Medicine spent.");
        activating = true;
        try
        {
            // The arrived shipment owns this exact paid dose; no second inventory debit.
            bool spent = delivery.TryConsumeDelivered(unit =>
            {
                priorSpeed = lastWritten = speed;
                activeSession = progression.State.Session; activeAttempt = progression.State.Attempt;
                activeGeneration = training.patient.EvidenceGeneration;
                startedAt = Time.time; boosting = true;
            });
            if (!spent) return Fail("No current Medicine is delivered. Craft, LOAD MEDICINE and SEND TO DEFENSE.");
            if (this == null || !boosting || !SameRun()) { if (this != null) StopBoost(); return false; }
            delivery.ShowUseFeedback("Used 1 delivered Medicine: +15% movement for 60s across all yards. Crafting is unchanged.");
            return true;
        }
        finally { activating = false; }
    }
    bool Fail(string message) { if (delivery == null) delivery = GetComponent<DEF_ClinicMedicineDelivery>(); if (delivery != null) delivery.ShowUseFeedback(message); else MED_PlayerHUD.ShowMessage(message); return false; }
    bool FindMovement()
    {
        xrMove = null; desktopMove = null;
        var rig = PWR_PlayerRig.Instance;
        if (rig == null || rig.gameObject.scene != gameObject.scene || rig.origin == null) return false;
        var input = DEF_VRInput.Instance;
        var camera = input != null && input.gameObject.scene == gameObject.scene && DEF_VRInput.Active && input.xrCamera != null ? input.xrCamera : Camera.main;
        if (camera == null || !camera.isActiveAndEnabled || !camera.transform.IsChildOf(rig.origin)) return false;
        movementRig = rig; movementOrigin = rig.origin; movementCamera = camera;
        usingXR = rig.XRActive;
        if (usingXR)
        {
            foreach (var move in rig.origin.GetComponentsInChildren<ContinuousMoveProvider>(true))
                if (move.isActiveAndEnabled) { if (xrMove != null) { xrMove = null; return false; } xrMove = move; }
            return xrMove != null;
        }
        foreach (var move in FindObjectsByType<PWR_DesktopRig>(FindObjectsSortMode.None))
            if (move.gameObject.scene == gameObject.scene && move.IsLocomotionActive)
            { if (desktopMove != null) { desktopMove = null; return false; } desktopMove = move; }
        return desktopMove != null;
    }
    float ReadSpeed() => usingXR ? (xrMove != null ? xrMove.moveSpeed : 0) : (desktopMove != null ? desktopMove.moveSpeed : 0);
    void WriteSpeed(float value) { if (usingXR) { if (xrMove != null) xrMove.moveSpeed = value; } else if (desktopMove != null) desktopMove.moveSpeed = value; }
    bool SameRun() => Ready && progression.State.Session == activeSession && progression.State.Attempt == activeAttempt &&
        training.patient.EvidenceGeneration == activeGeneration;
    bool MovementStillOwned()
    {
        var rig = PWR_PlayerRig.Instance;
        return rig != null && rig == movementRig && movementOrigin != null && rig.origin == movementOrigin &&
            movementCamera != null && movementCamera.isActiveAndEnabled && movementCamera.transform.IsChildOf(movementOrigin) &&
            rig.gameObject.scene == gameObject.scene && rig.XRActive == usingXR &&
            (xrMove == null || xrMove.transform.IsChildOf(movementOrigin)) &&
            (usingXR ? xrMove != null && xrMove.isActiveAndEnabled : desktopMove != null && desktopMove.IsLocomotionActive);
    }
    void Update()
    {
        if (pendingUnlockNotice && Ready)
        {
            pendingUnlockNotice = false;
            MED_PlayerHUD.ShowMessage("Craft another Medicine (2 Herbs + 1 Water). UNLOAD incoming ingredients, then LOAD MEDICINE and SEND TO DEFENSE. Keep the dose in your inventory, not REFILL stock. Use delivered Medicine for Field Aid at Defense; crafting speed stays the same.");
        }
        if (boosting)
        {
            if (!SameRun() || !MovementStillOwned() || !Mathf.Approximately(ReadSpeed(), lastWritten) || SecondsRemaining <= 0) StopBoost();
            else
            {
                // Smooth first/last second; never change tracking, turn speed, teleport or input bindings.
                float ease = Mathf.SmoothStep(0, 1, Mathf.Min(Time.time - startedAt, SecondsRemaining));
                lastWritten = priorSpeed * Mathf.Lerp(1, Multiplier, ease); WriteSpeed(lastWritten);
            }
        }
        UpdateTimer();
    }
    void StopBoost()
    {
        // Do not overwrite a newer movement setting from another owner.
        if (boosting && Mathf.Approximately(ReadSpeed(), lastWritten)) WriteSpeed(priorSpeed);
        boosting = false; xrMove = null; desktopMove = null; movementRig = null; movementOrigin = null; movementCamera = null;
        if (timerCanvas != null) timerCanvas.gameObject.SetActive(false);
    }
    void UpdateTimer()
    {
        if (!boosting) { if (timerCanvas != null) timerCanvas.gameObject.SetActive(false); return; }
        var input = DEF_VRInput.Instance;
        var camera = input != null && input.gameObject.scene == gameObject.scene && DEF_VRInput.Active && input.xrCamera != null ? input.xrCamera : Camera.main;
        if (camera == null) { if (timerCanvas != null) timerCanvas.gameObject.SetActive(false); return; }
        if (timerCanvas == null)
        {
            timerCanvas = PWR_UIFactory.CreateWorldCanvas("Field Aid travel timer", null, new Vector2(520, 65), .55f / 520f, false);
            PWR_UIFactory.CreatePanel(timerCanvas.transform, new Color(.015f, .02f, .025f, .94f)).raycastTarget = false;
            timerText = PWR_UIFactory.CreateText(timerCanvas.transform, "", 22, TextAlignmentOptions.Center, 5);
        }
        timerCanvas.gameObject.SetActive(true); timerCanvas.worldCamera = camera;
        timerCanvas.transform.SetPositionAndRotation(camera.transform.TransformPoint(new Vector3(0, -.53f, 1.25f)), camera.transform.rotation);
        timerText.text = "FIELD AID  +15% MOVE  " + Mathf.CeilToInt(SecondsRemaining) + "s";
    }
    void OnDisable()
    {
        if (readiness != null) readiness.Completed -= Completed;
        StopBoost();
    }
    void OnDestroy() { StopBoost(); if (timerCanvas != null) Destroy(timerCanvas.gameObject); }
}
