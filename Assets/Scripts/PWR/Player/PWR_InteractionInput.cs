using UnityEngine;
using UnityEngine.InputSystem;

namespace Deadzone.Power
{
    /// <summary>
    /// "Use" input. VR: point either controller at a machine and pull its trigger (holding an
    /// object, the trigger uses that object instead — XRI forwards it via PWR_Grabbable).
    /// Desktop fallback: E uses what the mouse points at (with the held object, if any),
    /// F picks up / drops.
    /// </summary>
    public class PWR_InteractionInput : MonoBehaviour
    {
        public float reach = 6f;
        public Transform desktopHoldPoint;

        private InputAction useRight, useLeft, useKey, grabKey;
        private PWR_Grabbable desktopHeld;

        public PWR_Grabbable DesktopHeld => desktopHeld;

        private void Awake()
        {
            useRight = new InputAction("PWR Use Right", InputActionType.Button, "<XRController>{RightHand}/{TriggerButton}");
            useLeft = new InputAction("PWR Use Left", InputActionType.Button, "<XRController>{LeftHand}/{TriggerButton}");
            useKey = new InputAction("PWR Use Key", InputActionType.Button, "<Keyboard>/e");
            grabKey = new InputAction("PWR Grab Key", InputActionType.Button, "<Keyboard>/f");
        }

        private void OnEnable()
        {
            useRight.Enable(); useLeft.Enable(); useKey.Enable(); grabKey.Enable();
        }

        private void OnDisable()
        {
            useRight.Disable(); useLeft.Disable(); useKey.Disable(); grabKey.Disable();
        }

        private void Update()
        {
            var rig = PWR_PlayerRig.Instance;
            bool building = PWR_BuildSystem.Instance != null && PWR_BuildSystem.Instance.selected != null;

            if (rig != null && rig.XRActive)
            {
                if (useRight.WasPressedThisFrame() && !building && !rig.IsHolding(true))
                    UseAlong(rig.ControllerRay(true), null);
                if (useLeft.WasPressedThisFrame() && !rig.IsHolding(false))
                    UseAlong(rig.ControllerRay(false), null);
                return;
            }

            if (!desktopHeld) desktopHeld = null; // the held object was destroyed (e.g. consumed by a socket)
            var cam = Camera.main;
            if (cam == null) return;
            Ray ray = Mouse.current != null
                ? cam.ScreenPointToRay(Mouse.current.position.ReadValue())
                : new Ray(cam.transform.position, cam.transform.forward);

            if (useKey.WasPressedThisFrame())
            {
                GameObject held = desktopHeld != null ? desktopHeld.gameObject : null;
                if (!UseAlong(ray, held) && desktopHeld != null) desktopHeld.UseSelf();
            }

            if (grabKey.WasPressedThisFrame())
            {
                if (desktopHeld != null)
                {
                    desktopHeld.DesktopDrop();
                    desktopHeld = null;
                }
                else
                {
                    var target = FirstHit<PWR_Grabbable>(ray, null);
                    if (target != null)
                    {
                        var holdPoint = desktopHoldPoint != null ? desktopHoldPoint : cam.transform;
                        target.DesktopPick(holdPoint);
                        // Pickup-harvested objects (scrap, coal) destroy themselves; don't keep a reference.
                        if (target.GetComponent<PWR_Harvestable>() == null) desktopHeld = target;
                    }
                }
            }
        }

        /// <summary>Uses the first interactable along the ray. Returns true if one was used.</summary>
        public bool UseAlong(Ray ray, GameObject held)
        {
            var target = FirstHit<PWR_Interactable>(ray, held);
            return target != null && target.Use(held);
        }

        private T FirstHit<T>(Ray ray, GameObject ignore) where T : Component
        {
            var hits = Physics.RaycastAll(ray, reach, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (ignore != null && hit.collider.transform.IsChildOf(ignore.transform)) continue;
                var comp = hit.collider.GetComponentInParent<T>();
                if (comp != null) return comp;
                // Stop at the first solid thing that isn't what we want (walls, floor).
                if (hit.collider.GetComponentInParent<PWR_Grabbable>() == null) return null;
            }
            return null;
        }
    }
}
