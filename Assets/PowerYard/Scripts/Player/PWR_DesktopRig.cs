using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deadzone.Power
{
    /// <summary>
    /// Lets the station be played in the Editor without a headset: WASD to walk, hold the right
    /// mouse button to look around. Does nothing when an XR headset is active.
    /// </summary>
    public class PWR_DesktopRig : MonoBehaviour
    {
        public float moveSpeed = 2.5f;
        public float lookSpeed = 0.15f;
        public float eyeHeight = 1.6f;

        private Transform origin;
        private Transform cam;
        private CharacterController controller;
        private float pitch;
        private bool active;

        private IEnumerator Start()
        {
            yield return PWR_PlayerRig.WaitForXRStartup();
            var rig = PWR_PlayerRig.Instance;
            if (rig == null || rig.XRActive || rig.origin == null || Camera.main == null) yield break;
            active = true;
            origin = rig.origin;
            cam = Camera.main.transform;
            controller = origin.GetComponent<CharacterController>();

            // Without a headset the tracked pose drivers would hold the camera at the floor.
            foreach (var b in origin.GetComponentsInChildren<Behaviour>(true))
                if (b != null && b.GetType().Name == "TrackedPoseDriver" && b.transform == cam) b.enabled = false;
            // The rig's camera offset may already add height; place the eyes eyeHeight above the floor.
            cam.position = origin.position + origin.up * eyeHeight;
            cam.localRotation = Quaternion.identity;

            // The headset camera has no desktop holding point; add one in front of the eyes.
            var input = FindAnyObjectByType<PWR_InteractionInput>();
            if (input != null && input.desktopHoldPoint == null)
            {
                var hold = new GameObject("PWR_DesktopHoldPoint").transform;
                hold.SetParent(cam, false);
                hold.localPosition = new Vector3(0.25f, -0.25f, 0.7f);
                input.desktopHoldPoint = hold;
            }
        }

        private void Update()
        {
            if (!active || Keyboard.current == null) return;
            var kb = Keyboard.current;
            Vector3 input = new Vector3(
                (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0), 0f,
                (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0));
            Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 move = (forward * input.z + right * input.x) * (moveSpeed * Time.deltaTime);
            if (controller != null && controller.enabled) controller.Move(move + Vector3.down * 0.01f);
            else origin.position += move;

            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                Vector2 delta = Mouse.current.delta.ReadValue() * lookSpeed;
                origin.Rotate(0f, delta.x, 0f, Space.World);
                pitch = Mathf.Clamp(pitch - delta.y, -80f, 80f);
                cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }
    }
}
