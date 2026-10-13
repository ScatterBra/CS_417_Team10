using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// Runs the level: dark start with red emergency lights, room brightness follows the floodlight,
    /// the bulb burns out once at 80% charge, event-driven tutorials, and the completion message.
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

        public static event Action LevelCompleted;

        public bool BulbFailed { get; private set; }
        public bool Complete { get; private set; }

        private readonly Dictionary<Light, float> originalIntensity = new Dictionary<Light, float>();
        private PWR_BuildSystem buildSystem;

        private void OnEnable()
        {
            PWR_CellCharger.Completed += HandleCompleted;
            PWR_Floodlight.BulbBroken += HandleBulbBroken;
        }

        private void OnDisable()
        {
            PWR_CellCharger.Completed -= HandleCompleted;
            PWR_Floodlight.BulbBroken -= HandleBulbBroken;
        }

        private void Start()
        {
            buildSystem = PWR_BuildSystem.Instance;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambientDark;
            foreach (var l in sceneLightsToDim)
            {
                if (l == null) continue;
                originalIntensity[l] = l.intensity;
                l.intensity = 0f;
            }
            Invoke(nameof(ShowIntro), 1.5f);
        }

        private void ShowIntro()
        {
            var anchor = hudAnchor != null ? hudAnchor
                : PWR_WristPanel.Instance != null && PWR_WristPanel.Instance.Anchor != null ? PWR_WristPanel.Instance.Anchor
                : Camera.main != null ? Camera.main.transform : transform;
            Tutorial("wrist_menu",
                "Open your wrist menu to place machines. Push the thumbstick left/right to rotate 90°.\n(Desktop: 1-5 select, R rotate, click to place)",
                anchor, new Vector3(0f, 0.25f, 0f));
        }

        private void Update()
        {
            float lit = floodlight != null ? floodlight.LightFraction : 0f;
            RenderSettings.ambientLight = Color.Lerp(ambientDark, ambientLit, lit);
            foreach (var kv in originalIntensity)
                if (kv.Key != null) kv.Key.intensity = kv.Value * lit;

            if (bankHint != null && buildSystem != null && buildSystem.CountBuilt("BatteryBank") > 0)
                bankHint.SetActive(false);

            if (floodlight != null && floodlight.IsLit)
                Tutorial("floodlight", "The Floodlight draws power from a nearby Battery Bank. Its bulb wears out over time.",
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
            Tutorial("bulb_out", "The bulb burned out! Craft a Light Bulb (1 Glass + 2 Copper Wire) and drop it into the lamp.",
                light.transform, new Vector3(0f, 1.9f, 0f));
        }

        private void HandleCompleted(PWR_CellCharger c)
        {
            if (Complete) return;
            Complete = true;
            var mgr = PWR_TutorialManager.Instance;
            if (mgr != null)
                mgr.ShowMessage("STATION ONLINE\nThe Charged Cell is ready to take to the Defense Yard.\nLevel complete!",
                    c.transform, new Vector3(0f, 1.2f, 0f), 0f);
            LevelCompleted?.Invoke();
        }

        private static void Tutorial(string id, string message, Transform anchor, Vector3 offset)
        {
            var mgr = PWR_TutorialManager.Instance;
            if (mgr != null) mgr.ShowOnce(id, message, anchor, offset);
        }
    }
}
