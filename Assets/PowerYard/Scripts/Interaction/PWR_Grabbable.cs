using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Deadzone.Power
{
    /// <summary>
    /// Bridges a physical pick-up between XR (XRGrabInteractable) and the desktop fallback.
    /// Pulling the trigger while holding fires <see cref="PWR_Interactable.Use"/> on this object.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PWR_Grabbable : MonoBehaviour
    {
        public event Action<PWR_Grabbable> Picked;
        public event Action<PWR_Grabbable> Dropped;

        public bool IsHeld { get; private set; }
        public Rigidbody Body { get; private set; }

        private XRGrabInteractable xrGrab;
        private bool desktopHeld;
        private bool wasKinematic;

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
            xrGrab = GetComponent<XRGrabInteractable>();
            if (xrGrab != null)
            {
                xrGrab.selectEntered.AddListener(_ => HandlePicked());
                xrGrab.selectExited.AddListener(_ => HandleDropped());
                xrGrab.activated.AddListener(_ => UseSelf());
            }
        }

        public void UseSelf()
        {
            if (TryGetComponent(out PWR_Interactable interactable)) interactable.Use(null);
        }

        // ---------- desktop fallback ----------

        public void DesktopPick(Transform holdPoint)
        {
            if (IsHeld) return;
            desktopHeld = true;
            wasKinematic = Body.isKinematic;
            Body.isKinematic = true;
            transform.SetParent(holdPoint, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            HandlePicked();
        }

        public void DesktopDrop()
        {
            if (!desktopHeld) return;
            desktopHeld = false;
            transform.SetParent(null, true);
            Body.isKinematic = wasKinematic;
            if (!Body.isKinematic) Body.linearVelocity = Vector3.zero;
            HandleDropped();
        }

        /// <summary>Releases the object from whoever holds it (call before destroying it).</summary>
        public void ForceRelease()
        {
            if (desktopHeld) DesktopDrop();
            if (xrGrab != null && xrGrab.isSelected && xrGrab.interactionManager != null)
                xrGrab.interactionManager.CancelInteractableSelection((UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)xrGrab);
        }

        private void HandlePicked()
        {
            IsHeld = true;
            Picked?.Invoke(this);
        }

        private void HandleDropped()
        {
            IsHeld = false;
            Dropped?.Invoke(this);
        }
    }
}
