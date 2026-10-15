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

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    public void Assign(MED_ModelShelf shelf, int slotIndex)
    {
        Shelf = shelf;
        SlotIndex = slotIndex;
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
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
        grab.enabled = true;
        routine = null;
    }
}
