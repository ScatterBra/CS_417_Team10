using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// A grabbable toy model of a building. Holds its definition and the shelf slot it belongs to.
[RequireComponent(typeof(XRGrabInteractable))]
public class MED_BuildingMini : MonoBehaviour
{
    [SerializeField] MED_BuildingDefinition definition;
    [SerializeField] float returnDuration = 0.4f;

    XRGrabInteractable grab;
    Coroutine routine;

    public MED_BuildingDefinition Definition => definition;
    public MED_ModelShelf Shelf { get; private set; }
    public int SlotIndex { get; private set; } = -1;
    public bool IsHeld { get; private set; }
    public bool IsLocked { get; private set; }

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    void OnEnable() => MED_ProgressionManager.Changed += ApplyLock;
    void OnDisable() => MED_ProgressionManager.Changed -= ApplyLock;
    void Start() => ApplyLock();

    /// <summary>Locked models are invisible and cannot be grabbed until their threshold is cleared.</summary>
    void ApplyLock()
    {
        bool locked = MED_ProgressionManager.TryGetFeature(definition, out var f) && MED_ProgressionManager.IsLocked(f);
        if (locked == IsLocked && grab.enabled == !locked) return;
        IsLocked = locked;
        UpdatePlate();
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = !locked;
        foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = !locked;
        if (routine == null) grab.enabled = !locked;
    }

    // A locked model is invisible, but its slot shows a small LOCKED plate. Poking the plate explains what unlocks it.
    void UpdatePlate()
    {
        var slot = transform.parent;
        if (slot == null) return;
        var plate = slot.Find("MED_LockedPlate");
        if (plate == null)
        {
            if (!IsLocked) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "MED_LockedPlate";
            go.transform.SetParent(slot, false);
            go.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            go.transform.localScale = new Vector3(0.16f, 0.08f, 0.16f);
            go.GetComponent<Renderer>().sharedMaterial = MED_SandboxUtil.MakeMaterial(new Color(0.3f, 0.32f, 0.35f));
            MED_SandboxUtil.Label(go.transform, "LOCKED", new Vector3(0f, 1.2f, 0f), 2.2f);
            go.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>().selectEntered.AddListener(_ => PokePlate());
            plate = go.transform;
        }
        plate.gameObject.SetActive(IsLocked);
    }

    void PokePlate()
    {
        if (!IsLocked || MED_AreaGate.Outside) return;
        if (MED_ProgressionManager.TryGetFeature(definition, out var f))
            MED_Messages.Fail(transform.parent.position + Vector3.up * 0.4f, MED_Messages.Locked(f));
    }

    public void Assign(MED_ModelShelf shelf, int slotIndex)
    {
        Shelf = shelf;
        SlotIndex = slotIndex;
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (IsLocked || MED_AreaGate.Outside)
        {
            // Locked-use feedback (also a catch). Normally unreachable: locked models are hidden and ungrabbable.
            if (IsLocked && MED_ProgressionManager.TryGetFeature(definition, out var f)) MED_Messages.Fail(transform.position + Vector3.up * 0.4f, MED_Messages.Locked(f));
            ReturnHome();
            return;
        }
        if (routine != null) { StopCoroutine(routine); routine = null; }
        IsHeld = true;
        MED_PopupTutorial.FireById("grid", transform);   // first grab of any model explains the build grid
        if (Shelf != null && Shelf.Placer != null) Shelf.Placer.BeginCarry(this, args.interactorObject);
    }

    void OnReleased(SelectExitEventArgs args)
    {
        IsHeld = false;
        if (Shelf != null && Shelf.Placer != null) Shelf.Placer.EndCarry(this);
        else ReturnHome();
    }

    /// <summary>Animates the model back to its shelf slot.</summary>
    public void ReturnHome()
    {
        if (Shelf == null || !gameObject.activeInHierarchy) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ReturnRoutine());
    }

    IEnumerator ReturnRoutine()
    {
        grab.enabled = false; // cannot be re-grabbed mid-flight
        Shelf.GetSlotPose(SlotIndex, out var endPos, out var endRot);
        var startPos = transform.position;
        var startRot = transform.rotation;
        for (float t = 0f; t < returnDuration; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / returnDuration);
            transform.SetPositionAndRotation(Vector3.Lerp(startPos, endPos, k), Quaternion.Slerp(startRot, endRot, k));
            yield return null;
        }
        transform.SetPositionAndRotation(endPos, endRot);
        grab.enabled = !IsLocked;
        routine = null;
    }
}
