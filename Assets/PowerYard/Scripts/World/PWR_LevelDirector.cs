using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// Runs the level: dark start with red emergency lights, room brightness follows the floodlight,
    /// the bulb burns out once at 80% charge, event-driven tutorials, and the completion message.
    /// The station is complete once the burnt-out bulb has been replaced AND the player has picked
    /// up the Charged Cell, in either order. Other areas check <see cref="StationComplete"/> or
    /// listen to <see cref="StationCompleted"/>.
    /// </summary>
    public class PWR_LevelDirector : MonoBehaviour
    {
        [Header("Scene")]
        public PWR_Floodlight floodlight;
        public PWR_CellCharger charger;
        public List<Light> sceneLightsToDim = new List<Light>();
        public List<Light> emergencyLights = new List<Light>();
        public GameObject bankHint;
        public Transform hudAnchor;

        [Header("Lighting")]
        public Color ambientDark = new Color(0.10f, 0.09f, 0.10f);
        public Color ambientLit = new Color(0.42f, 0.42f, 0.40f);

        [Header("Rules")]
        [Range(0f, 100f)] public float bulbFailurePercent = 80f;
        [Range(0f, 1f)] public float lowChargeWarning = 0.15f;

        /// <summary>True once the Power Station is finished (bulb replaced + Charged Cell picked up).</summary>
        public static bool StationComplete { get; private set; }
        /// <summary>Fired once, the moment <see cref="StationComplete"/> becomes true.</summary>
        public static event Action StationCompleted;

        public bool BulbFailed { get; private set; }

        private readonly Dictionary<Light, float> originalIntensity = new Dictionary<Light, float>();
        private PWR_BuildSystem buildSystem;

        // The scene's own lighting, restored whenever the player is outside the Power area.
        private UnityEngine.Rendering.AmbientMode originalAmbientMode;
        private Color originalAmbientLight;
        private bool stationLightingApplied;

        // Statics survive a scene reload (and play mode without domain reload), so a new level starts fresh.
        private void Awake()
        {
            StationComplete = false;
            if (!GetComponent<PWR_Progression>()) gameObject.AddComponent<PWR_Progression>();
            if (!GetComponent<PWR_AreaGate>()) gameObject.AddComponent<PWR_AreaGate>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            StationComplete = false;
            StationCompleted = null;
        }

        private void OnEnable()
        {
            PWR_CellCharger.Completed += HandleCellReady;
            PWR_CellCharger.CellCollected += HandleCellCollected;
            PWR_Floodlight.BulbBroken += HandleBulbBroken;
            PWR_Floodlight.BulbInstalled += HandleBulbInstalled;
        }

        private void OnDisable()
        {
            PWR_CellCharger.Completed -= HandleCellReady;
            PWR_CellCharger.CellCollected -= HandleCellCollected;
            PWR_Floodlight.BulbBroken -= HandleBulbBroken;
            PWR_Floodlight.BulbInstalled -= HandleBulbInstalled;
        }

        private void Start()
        {
            buildSystem = PWR_BuildSystem.Instance;
            if (charger == null || floodlight == null)
                Debug.LogError("[PWR] Level Director is missing its Cell Charger or Floodlight reference: the station can never be completed.", this);
            originalAmbientMode = RenderSettings.ambientMode;
            originalAmbientLight = RenderSettings.ambientLight;
            foreach (var l in sceneLightsToDim)
                if (l != null) originalIntensity[l] = l.intensity;
            if (TryGetComponent(out PWR_AreaGate gate)) gate.Evaluate();
            UpdateLighting();
            // Shared scene with a Defense Yard: that's where the Charged Cell goes.
            PWR_DefenseCellDock.EnsureInScene();
            // After PWR_PlayerRig.WaitForXRStartup (up to 2 s) so the menu anchor exists.
            Invoke(nameof(ShowIntro), 2.5f);
        }

        private void ShowIntro()
        {
            var anchor = hudAnchor != null ? hudAnchor
                : PWR_WristPanel.Instance != null && PWR_WristPanel.Instance.Anchor != null ? PWR_WristPanel.Instance.Anchor
                : Camera.main != null ? Camera.main.transform : transform;
            Tutorial("wrist_menu",
                "Press Y on your left controller to open the build menu above your right hand. Point at it with your left controller and pull the trigger. Press Y again to close it.\n(Desktop: 1-5 select, X demolish, R rotate, click to place)",
                anchor, new Vector3(0f, 0.25f, 0f));
        }

        /// <summary>
        /// Inside the Power area the room is dark and brightens with the Floodlight; outside it the
        /// scene's own lighting is restored, so the rest of a shared scene isn't darkened.
        /// </summary>
        private void UpdateLighting()
        {
            if (PWR_AreaGate.PlayerInside)
            {
                float lit = floodlight != null ? floodlight.LightFraction : 0f;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = Color.Lerp(ambientDark, ambientLit, lit);
                foreach (var kv in originalIntensity)
                    if (kv.Key != null) kv.Key.intensity = kv.Value * lit;
                stationLightingApplied = true;
            }
            else if (stationLightingApplied)
            {
                // Restore once on the way out, then leave the lighting to the rest of the scene.
                RenderSettings.ambientMode = originalAmbientMode;
                RenderSettings.ambientLight = originalAmbientLight;
                foreach (var kv in originalIntensity)
                    if (kv.Key != null) kv.Key.intensity = kv.Value;
                stationLightingApplied = false;
            }
        }

        private void OnDestroy()
        {
            if (stationLightingApplied)
            {
                RenderSettings.ambientMode = originalAmbientMode;
                RenderSettings.ambientLight = originalAmbientLight;
            }
        }

        private void Update()
        {
            UpdateLighting();

            if (bankHint != null && buildSystem != null && buildSystem.CountBuilt("BatteryBank") > 0)
                bankHint.SetActive(false);

            if (floodlight != null && floodlight.IsLit)
                Tutorial("floodlight", "The Floodlight draws power from a Battery Bank within 2 cells. Its bulb wears out over time.",
                    floodlight.transform, new Vector3(0f, 1.9f, 0f));

            var bank = FindBank();
            if (bank != null && bank.Stats.GetNormalized(bank.energyStat) < lowChargeWarning && !AnyGeneratorRunning())
                Tutorial("charge_gauge", "Charge is draining. Build generators and find fuel before it runs out.",
                    bank.transform, new Vector3(0f, 2.2f, 0f));

            if (!BulbFailed && charger != null && charger.HasBattery && charger.ProgressPercent >= bulbFailurePercent)
            {
                BulbFailed = true;
                if (floodlight != null) floodlight.BreakBulb();
            }
        }

        private static PWR_PowerNode FindBank()
        {
            foreach (var n in FindObjectsByType<PWR_PowerNode>())
                if (n.role == PWR_PowerRole.Storage) return n;
            return null;
        }

        private static bool AnyGeneratorRunning()
        {
            foreach (var g in FindObjectsByType<PWR_Generator>())
                if (g.IsRunning) return true;
            return false;
        }

        private void HandleBulbBroken(PWR_Floodlight light)
        {
            if (light != floodlight) return;
            Tutorial("bulb_out", "The bulb burned out! Craft a Light Bulb (1 Glass + 2 Copper Wire) and drop it into the lamp.",
                light.transform, new Vector3(0f, 1.9f, 0f));
        }

        // ---------- completion: bulb replaced + Charged Cell picked up, in either order ----------

        // Integration completion requires the actual replacement exercise.
        private bool BulbOk => floodlight != null && floodlight.BulbReplaced;

        private void HandleCellReady(PWR_CellCharger c)
        {
            if (c != charger || floodlight == null) return;
            // A large final transfer can cross the failure threshold before this director's Update.
            if (!floodlight.BulbReplaced && floodlight.HasBulb)
            {
                BulbFailed = true;
                floodlight.BreakBulb();
            }
            if (!BulbOk)
                Tutorial("cell_ready_no_bulb",
                    "Charged Cell ready, but the Floodlight is still out. Install the spare bulb to bring the station online.",
                    floodlight.transform, new Vector3(0f, 1.9f, 0f));
            else if (!c.IsCellCollected)
                Tutorial("cell_ready", "Charged Cell ready! Pick it up to bring the station online.",
                    c.transform, new Vector3(0f, 1.2f, 0f));
            TryComplete();
        }

        private void HandleCellCollected(PWR_CellCharger c)
        {
            if (c != charger || floodlight == null) return;
            if (!BulbOk)
                Tutorial("cell_ready_no_bulb",
                    "Charged Cell ready, but the Floodlight is still out. Install the spare bulb to bring the station online.",
                    floodlight.transform, new Vector3(0f, 1.9f, 0f));
            TryComplete();
        }

        private void HandleBulbInstalled(PWR_Floodlight light)
        {
            if (light != floodlight) return;
            if (charger != null && charger.IsComplete && !charger.IsCellCollected)
                Tutorial("cell_pickup", "Pick up the Charged Cell to bring the station online.",
                    charger.transform, new Vector3(0f, 1.2f, 0f));
            TryComplete();
        }

        private void TryComplete()
        {
            if (StationComplete || charger == null || !charger.IsCellCollected || !BulbOk) return;
            StationComplete = true;
            var mgr = PWR_TutorialManager.Instance;
            if (mgr != null)
                mgr.ShowMessage("STATION ONLINE\nPower is restored. Level complete!\nNext: " + PWR_Progression.GoalFor(PWR_Stage.Online),
                    charger.transform, new Vector3(0f, 1.2f, 0f), 0f);
            StationCompleted?.Invoke();
        }

        private static void Tutorial(string id, string message, Transform anchor, Vector3 offset)
        {
            var mgr = PWR_TutorialManager.Instance;
            if (mgr != null) mgr.ShowOnce(id, message, anchor, offset);
        }
    }
}
