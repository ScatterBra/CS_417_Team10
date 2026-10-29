using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Deadzone.Power
{
    /// <summary>
    /// Power → Defense link. The player carries the Charged Cell from the Cell Charger to this dock
    /// in the Defense Yard and lets go of it there. Once it is docked, every Shock Trap (including
    /// traps built later) recharges in <see cref="overchargedRecharge"/> seconds instead of its default.
    /// Place one by hand in a shared scene, or <see cref="EnsureInScene"/> spawns one next to the
    /// Defense ammo intake. A scene without a Defense Yard never gets a dock.
    /// </summary>
    public class PWR_DefenseCellDock : MonoBehaviour
    {
        public float overchargedRecharge = 4f;
        public Vector3 mountOffset = new Vector3(0f, 1.05f, 0f);

        // Where a spawned dock stands relative to the Defense ammo intake.
        private static readonly Vector3 SpawnOffsetFromIntake = new Vector3(1.6f, 0f, 0f);

        public static PWR_DefenseCellDock Instance { get; private set; }
        /// <summary>Fired once, when the Charged Cell is docked.</summary>
        public static event Action<PWR_DefenseCellDock> CellDocked;

        public bool IsDocked { get; private set; }
        public int OverchargedCount => overcharged.Count;

        private const float ScanInterval = 0.5f;
        private const float RejectCooldown = 3f;

        // Each overcharged trap and the recharge it had before, restored if the dock goes away.
        private readonly Dictionary<DEF_ShockTrap, float> overcharged = new Dictionary<DEF_ShockTrap, float>();
        private readonly Dictionary<PWR_Grabbable, float> lastReject = new Dictionary<PWR_Grabbable, float>();
        private PWR_Grabbable dockedCell;
        private Renderer ring;
        private GameObject beacon;
        private TMP_Text label;
        private MaterialPropertyBlock mpb;
        private float nextScan;
        private float defaultRecharge = 7f;

        private static readonly Color Waiting = new Color(1f, 0.62f, 0.15f);
        private static readonly Color Online = new Color(0.25f, 0.9f, 1f);

        /// <summary>
        /// Spawns a dock when this scene has both a Power Station and a Defense Yard but nobody placed
        /// one. Called by <see cref="PWR_LevelDirector"/> at Start.
        /// </summary>
        public static void EnsureInScene()
        {
            if (FindAnyObjectByType<PWR_DefenseCellDock>(FindObjectsInactive.Include) != null) return;
            if (FindAnyObjectByType<DEF_GameState>() == null) return;

            Vector3 at;
            var intake = FindAnyObjectByType<DEF_WorkshopAmmoReceiver>(FindObjectsInactive.Include);
            if (intake != null) at = intake.transform.position + SpawnOffsetFromIntake;
            else if (DEF_BaseStructure.Instance != null)
                at = DEF_BaseStructure.Instance.transform.position + Vector3.right * (DEF_BaseStructure.Instance.halfSize + 1.5f);
            else return;

            // Stand on whatever floor is below, ignoring trigger volumes.
            at.y = Physics.Raycast(at + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 10f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? hit.point.y : 0f;
            new GameObject("PWR Cell Dock (Power → Defense)").AddComponent<PWR_DefenseCellDock>().transform.position = at;
        }

        private void Awake()
        {
            Instance = this;
            mpb = new MaterialPropertyBlock();
            BuildVisuals();
            UpdateLabel();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            // Hand the traps back their own recharge if the dock goes away.
            foreach (var kv in overcharged)
                if (kv.Key != null && Mathf.Approximately(kv.Key.rechargeSeconds, overchargedRecharge))
                    kv.Key.rechargeSeconds = kv.Value;
        }

        private void BuildVisuals()
        {
            // Visual parts carry no colliders, so the dock never blocks a turret shot or a shock arc.
            Part(PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0f), new Vector3(0.5f, 0.45f, 0.5f), new Color(0.18f, 0.2f, 0.23f));
            ring = Part(PrimitiveType.Cylinder, new Vector3(0f, 0.92f, 0f), new Vector3(0.38f, 0.02f, 0.38f), Waiting);
            beacon = Part(PrimitiveType.Cylinder, new Vector3(0f, 2.4f, 0f), new Vector3(0.05f, 1.45f, 0.05f), Waiting).gameObject;

            var trigger = gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.15f, 0f);
            trigger.size = new Vector3(0.7f, 0.7f, 0.7f);

            var canvas = PWR_UIFactory.CreateWorldCanvas("PWR_CellDockLabel", transform, new Vector2(760, 230), 0.0016f, false);
            canvas.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            PWR_UIFactory.CreatePanel(canvas.transform, PWR_UIFactory.PanelColor);
            label = PWR_UIFactory.CreateText(canvas.transform, "", 30, TextAlignmentOptions.Center, 14);
            canvas.gameObject.AddComponent<PWR_Billboard>();
        }

        private Renderer Part(PrimitiveType type, Vector3 localPos, Vector3 scale, Color color)
        {
            var part = GameObject.CreatePrimitive(type);
            Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(transform, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = scale;
            var r = part.GetComponent<Renderer>();
            Tint(r, color);
            return r;
        }

        private void Tint(Renderer r, Color color)
        {
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", color);
            mpb.SetColor("_Color", color);
            r.SetPropertyBlock(mpb);
        }

        // ---------- docking ----------

        private void OnTriggerStay(Collider other)
        {
            if (IsDocked) return;
            var grab = other.GetComponentInParent<PWR_Grabbable>();
            // Wait until the player lets go, like the Defense ammo intake.
            if (grab == null || grab.IsHeld) return;
            TryDock(grab);
        }

        private bool TryDock(PWR_Grabbable grab)
        {
            var item = grab.GetComponent<PWR_ItemObject>();
            if (OwnerOf(grab) == null)
            {
                Reject(grab, item != null && item.resource == PWR_ResourceType.Battery
                    ? "This Battery isn't charged. Charge it in the Power Station's Cell Charger, then bring the Charged Cell here."
                    : item != null
                        ? $"The dock only takes the Charged Cell, not a {PWR_Names.Of(item.resource)}. Charge a Battery in the Power Station."
                        : "The dock only takes the Charged Cell from the Power Station's Cell Charger.");
                return false;
            }
            if (!PWR_LevelDirector.StationComplete)
            {
                Reject(grab, "The Power Station isn't online yet. Replace the Floodlight bulb there, then dock the cell.");
                return false;
            }
            Dock(grab);
            return true;
        }

        private static PWR_CellCharger OwnerOf(PWR_Grabbable grab)
        {
            foreach (var c in FindObjectsByType<PWR_CellCharger>())
                if (c.ChargedCell != null && c.ChargedCell == grab) return c;
            return null;
        }

        private void Reject(PWR_Grabbable grab, string message)
        {
            if (lastReject.TryGetValue(grab, out float t) && Time.time - t < RejectCooldown) return;
            lastReject[grab] = Time.time;
            PWR_Feedback.Show(transform.position + Vector3.up * 1.5f, message, PWR_Feedback.Bad);
        }

        private void Dock(PWR_Grabbable grab)
        {
            IsDocked = true;
            dockedCell = grab;
            grab.ForceRelease();
            // The cell stays in the dock for good: it can't be grabbed again or fall off.
            if (grab.TryGetComponent(out XRGrabInteractable xr)) xr.enabled = false;
            // Docked cells are no longer pickup or combat targets.
            foreach (var cellCollider in grab.GetComponentsInChildren<Collider>(true))
                cellCollider.enabled = false;
            grab.Body.isKinematic = true;
            grab.transform.SetParent(transform, false);
            grab.transform.localPosition = mountOffset;
            grab.transform.localRotation = Quaternion.identity;

            Tint(ring, Online);
            beacon.SetActive(false);
            PWR_Feedback.Show(transform.position + Vector3.up * 1.5f,
                $"Charged Cell docked! Shock Traps overcharged: recharge {overchargedRecharge:0}s.", PWR_Feedback.Good);
            nextScan = 0f;
            CellDocked?.Invoke(this);
        }

        // ---------- overcharge ----------

        private void Update()
        {
            if (IsDocked && dockedCell != null)
            {
                // XRGrabInteractable may restore the rigidbody flags it saw on grab after the release.
                if (!dockedCell.Body.isKinematic) dockedCell.Body.isKinematic = true;
                if (dockedCell.transform.parent != transform) dockedCell.transform.SetParent(transform, false);
                dockedCell.transform.localPosition = mountOffset;
            }

            if (Time.time < nextScan) return;
            nextScan = Time.time + ScanInterval;
            if (IsDocked) OverchargeTraps();
            UpdateLabel();
        }

        private void OverchargeTraps()
        {
            var gone = new List<DEF_ShockTrap>();
            foreach (var trap in overcharged.Keys)
                if (trap == null) gone.Add(trap);
            foreach (var trap in gone) overcharged.Remove(trap);

            // Traps are built during play, so keep looking for new ones.
            foreach (var trap in FindObjectsByType<DEF_ShockTrap>())
            {
                if (!trap.isActiveAndEnabled || overcharged.ContainsKey(trap)) continue;
                overcharged[trap] = trap.rechargeSeconds;
                if (trap.rechargeSeconds <= overchargedRecharge) continue;
                defaultRecharge = trap.rechargeSeconds;
                trap.rechargeSeconds = overchargedRecharge;
                Vector3 to = trap.emitter != null ? trap.emitter.position : trap.transform.position + Vector3.up * 0.75f;
                DEF_Effects.SpawnArc(transform.TransformPoint(mountOffset), to);
                PWR_Feedback.Show(trap.transform.position + Vector3.up * 1.6f,
                    $"Overcharged: {defaultRecharge:0}s → {overchargedRecharge:0}s", PWR_Feedback.Good);
            }
        }

        private void UpdateLabel()
        {
            if (label == null) return;
            if (!IsDocked)
            {
                label.text = "<color=#FFC840>CELL DOCK</color>\n" +
                    (PWR_LevelDirector.StationComplete
                        ? "Bring the Charged Cell from the Power Station and let go of it here."
                        : "Restore the Power Station, then bring its Charged Cell here.") +
                    "\nDocking it overcharges the Shock Traps.";
                return;
            }
            bool shockUnlocked = DEF_Progression.Current == null || DEF_Progression.Current.IsAllowed(DEF_Action.Shock);
            label.text = "<color=#40E6FF>CELL DOCK ONLINE</color>\n" +
                (OverchargedCount > 0
                    ? $"Shock Traps overcharged: {OverchargedCount}\nRecharge {defaultRecharge:0}s → {overchargedRecharge:0}s"
                    : shockUnlocked
                        ? "No Shock Traps built yet.\nEvery trap you build is overcharged."
                        : "Shock Traps unlock later in Defense training.\nThey'll be overcharged as soon as you build them.");
        }
    }
}
