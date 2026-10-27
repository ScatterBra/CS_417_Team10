using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Management;

namespace Deadzone.Power
{
    /// <summary>
    /// Finds the parts of the XR Origin the station needs (controllers, camera, interactors)
    /// and tells the rest of the station whether a headset is active.
    /// </summary>
    public class PWR_PlayerRig : MonoBehaviour
    {
        public static PWR_PlayerRig Instance { get; private set; }

        public Transform origin;
        public Transform leftController;
        public Transform rightController;

        public NearFarInteractor LeftInteractor { get; private set; }
        public NearFarInteractor RightInteractor { get; private set; }

        public bool XRActive => XRSettings.isDeviceActive;

        public Transform Head => Camera.main != null ? Camera.main.transform : null;

        /// <summary>
        /// Startup code that picks VR or desktop mode yields on this first. The OpenXR display can report
        /// inactive for the first frames after the scene loads, and with Quest Link running but no headset
        /// on, the XR loader starts yet the display never does; so wait for the display, up to a timeout.
        /// </summary>
        public static IEnumerator WaitForXRStartup(float timeout = 2f)
        {
            var settings = XRGeneralSettings.Instance;
            bool loaderActive = settings != null && settings.Manager != null && settings.Manager.activeLoader != null;
            if (!loaderActive) yield break; // no XR runtime at all: desktop right away
            float end = Time.realtimeSinceStartup + timeout;
            while (!XRSettings.isDeviceActive && Time.realtimeSinceStartup < end) yield return null;
        }

        private Behaviour[] turnProviders = new Behaviour[0];
        private bool turnEnabled = true;

        private void Awake()
        {
            Instance = this;
            ResolveControllers();
        }

        public void ResolveControllers()
        {
            if (origin == null)
            {
                var xrOrigin = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
                if (xrOrigin != null) origin = xrOrigin.transform;
            }
            if (origin == null) return;
            if (leftController == null) leftController = FindDeep(origin, "Left Controller");
            if (rightController == null) rightController = FindDeep(origin, "Right Controller");
            if (leftController != null) LeftInteractor = leftController.GetComponentInChildren<NearFarInteractor>(true);
            if (rightController != null) RightInteractor = rightController.GetComponentInChildren<NearFarInteractor>(true);

            var list = new System.Collections.Generic.List<Behaviour>();
            foreach (var b in origin.GetComponentsInChildren<Behaviour>(true))
                if (b != null && b.GetType().Name.Contains("TurnProvider")) list.Add(b);
            turnProviders = list.ToArray();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool IsHolding(bool rightHand)
        {
            var interactor = rightHand ? RightInteractor : LeftInteractor;
            return interactor != null && interactor.hasSelection;
        }

        public Ray ControllerRay(bool rightHand)
        {
            var t = rightHand ? rightController : leftController;
            if (t == null) t = Head;
            return t == null ? new Ray(Vector3.zero, Vector3.forward) : new Ray(t.position, t.forward);
        }

        /// <summary>Snap/continuous turn is paused while the right thumbstick rotates a building.</summary>
        public void SetTurnEnabled(bool enabled)
        {
            if (turnEnabled == enabled) return;
            turnEnabled = enabled;
            if (!enabled) turnWasEnabled = System.Array.ConvertAll(turnProviders, b => b != null && b.enabled);
            for (int i = 0; i < turnProviders.Length; i++)
            {
                if (turnProviders[i] == null) continue;
                // Restore exactly what the rig had (only one of snap/continuous turn is normally on).
                turnProviders[i].enabled = enabled ? (turnWasEnabled.Length > i && turnWasEnabled[i]) : false;
            }
        }

        private bool[] turnWasEnabled = new bool[0];

        public static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}

