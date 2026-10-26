using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Deadzone.Power
{
    /// <summary>
    /// Build mode controls. VR: pick an item in the build menu (Y opens it above the right hand), aim the right controller at the floor,
    /// pull the trigger to place, flick the right thumbstick left/right to rotate 90°, B to cancel.
    /// Demolish (menu button): aim at something you built and pull the trigger.
    /// Desktop: 1-5 select, X demolish, mouse to aim, left click place/demolish, R rotate, Q/Esc cancel.
    /// </summary>
    public class PWR_BuildInput : MonoBehaviour
    {
        public float maxAimDistance = 25f;

        private InputAction place, rotateStick, rotateKey, cancel, demolishKey;
        private readonly InputAction[] hotkeys = new InputAction[9];
        private bool stickCentered = true;
        private float ignorePlaceUntil;

        private void Awake()
        {
            place = new InputAction("PWR Place", InputActionType.Button);
            place.AddBinding("<XRController>{RightHand}/{TriggerButton}");
            place.AddBinding("<Mouse>/leftButton");

            rotateStick = new InputAction("PWR Rotate Stick", InputActionType.Value);
            rotateStick.AddBinding("<XRController>{RightHand}/{Primary2DAxis}");

            rotateKey = new InputAction("PWR Rotate Key", InputActionType.Button, "<Keyboard>/r");
            demolishKey = new InputAction("PWR Demolish Key", InputActionType.Button, "<Keyboard>/x");

            cancel = new InputAction("PWR Cancel", InputActionType.Button);
            cancel.AddBinding("<XRController>{RightHand}/{SecondaryButton}");
            cancel.AddBinding("<Keyboard>/q");
            cancel.AddBinding("<Keyboard>/escape");

            for (int i = 0; i < hotkeys.Length; i++)
                hotkeys[i] = new InputAction($"PWR Build {i + 1}", InputActionType.Button, $"<Keyboard>/{i + 1}");
        }

        private void OnEnable()
        {
            place.Enable(); rotateStick.Enable(); rotateKey.Enable(); cancel.Enable(); demolishKey.Enable();
            foreach (var h in hotkeys) h.Enable();
        }

        private void OnDisable()
        {
            place.Disable(); rotateStick.Disable(); rotateKey.Disable(); cancel.Disable(); demolishKey.Disable();
            foreach (var h in hotkeys) h.Disable();
        }

        /// <summary>Called by the wrist menu buttons.</summary>
        public void SelectIndex(int index)
        {
            var bs = PWR_BuildSystem.Instance;
            if (bs == null) return;
            var def = index >= 0 && index < bs.buildables.Count ? bs.buildables[index] : null;
            bs.Select(def);
            // The trigger release that clicked the button must not also place a building.
            ignorePlaceUntil = Time.time + 0.35f;
            if (def != null && (def.BuildingId == "ConcretePad" || def.BuildingId == "ConveyorBelt"))
            {
                var mgr = PWR_TutorialManager.Instance;
                var hud = PWR_WristPanel.Instance != null ? PWR_WristPanel.Instance.Anchor : null;
                if (mgr != null && hud != null)
                    mgr.ShowOnce("concrete_pad",
                        "Lay Concrete Pads first. Conveyor belts can only sit on pads (1 Scrap each). Connect the chute to the Receiving Bin, then press the chute's button.",
                        hud, new Vector3(0f, 0.6f, 0f)); // above the build-controls popup
            }
        }

        /// <summary>Called by the menu's Demolish button.</summary>
        public void SelectDemolish()
        {
            var bs = PWR_BuildSystem.Instance;
            if (bs == null) return;
            bs.SelectDemolish();
            ignorePlaceUntil = Time.time + 0.35f;
        }

        private void Update()
        {
            var bs = PWR_BuildSystem.Instance;
            var grid = PWR_GridManager.Instance;
            var rig = PWR_PlayerRig.Instance;
            if (bs == null || grid == null) return;

            // Outside the Power area (shared scene) the station's build controls are off, so the
            // trigger, B and right stick are free for the other areas.
            if (!PWR_AreaGate.PlayerInside)
            {
                if (bs.IsActive) bs.Select(null);
                if (rig != null) rig.SetTurnEnabled(true);
                return;
            }

            for (int i = 0; i < hotkeys.Length && i < bs.buildables.Count; i++)
                if (hotkeys[i].WasPressedThisFrame()) SelectIndex(i);
            if (demolishKey.WasPressedThisFrame()) SelectDemolish();

            bool building = bs.selected != null;
            // The right thumbstick rotates while placing; demolishing leaves turning alone.
            if (rig != null) rig.SetTurnEnabled(!building);
            if (!bs.IsActive) return;

            if (cancel.WasPressedThisFrame())
            {
                bs.Select(null);
                return;
            }

            if (bs.Demolishing)
            {
                Ray aim = AimRay(rig);
                bs.UpdateDemolishPreview(aim, maxAimDistance);
                if (place.WasPressedThisFrame() && Time.time >= ignorePlaceUntil && !PointerOverUI(aim, maxAimDistance, rig))
                    bs.TryDemolish(aim.GetPoint(2f));
                return;
            }

            if (rotateKey.WasPressedThisFrame()) bs.Rotate(1);
            float x = rotateStick.ReadValue<Vector2>().x;
            if (stickCentered && Mathf.Abs(x) > 0.7f)
            {
                bs.Rotate(x > 0f ? 1 : -1);
                stickCentered = false;
            }
            else if (Mathf.Abs(x) < 0.3f) stickCentered = true;

            Ray ray = AimRay(rig);
            if (!grid.GroundPlane.Raycast(ray, out float dist) || dist > maxAimDistance)
            {
                bs.HidePreview();
                return;
            }
            Vector3 point = ray.GetPoint(dist);
            bs.UpdatePreview(point);

            if (place.WasPressedThisFrame() && Time.time >= ignorePlaceUntil && !PointerOverUI(ray, dist, rig))
                bs.TryBuild(point);
        }

        private static Ray AimRay(PWR_PlayerRig rig)
        {
            if (rig != null && rig.XRActive) return rig.ControllerRay(true);
            var cam = Camera.main;
            if (cam == null) return new Ray(Vector3.up, Vector3.down);
            Vector2 mouse = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width / 2f, Screen.height / 2f);
            return cam.ScreenPointToRay(mouse);
        }

        private static bool PointerOverUI(Ray ray, float groundDistance, PWR_PlayerRig rig)
        {
            if (rig == null || !rig.XRActive)
                return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            // In VR, world UI panels carry a trigger collider; a panel in front of the floor blocks placing.
            foreach (var hit in Physics.RaycastAll(ray, groundDistance, ~0, QueryTriggerInteraction.Collide))
                if (hit.collider.GetComponentInParent<Canvas>() != null) return true;
            return false;
        }
    }
}
