using System;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// The charger. Drop a crafted battery into its slot; it then draws power from the neighboring
    /// battery bank into its Progress stat. At 100% the battery pops out as a Charged Cell; picking
    /// it up is one of the two conditions for the station being complete (see PWR_LevelDirector).
    /// </summary>
    [RequireComponent(typeof(PWR_StatBlock), typeof(PWR_PowerNode))]
    public class PWR_CellCharger : MonoBehaviour, PWR_ISocketOwner
    {
        public string progressStat = "Progress";
        public GameObject slottedBatteryVisual;
        public GameObject chargedCellPrefab;
        public Transform ejectPoint;

        public static event Action<PWR_CellCharger> BatteryInserted;
        public static event Action<PWR_CellCharger> Completed;
        /// <summary>The player picked up the ejected Charged Cell for the first time.</summary>
        public static event Action<PWR_CellCharger> CellCollected;

        public bool HasBattery { get; private set; }
        public bool IsComplete { get; private set; }
        public bool IsCellCollected { get; private set; }
        /// <summary>Offline until PWR_Progression unlocks it (after the first Battery is crafted).</summary>
        public bool Locked { get; set; }
        public float ProgressPercent => stats.GetNormalized(progressStat) * 100f;

        private PWR_StatBlock stats;
        private PWR_PowerNode node;

        private void Awake()
        {
            stats = GetComponent<PWR_StatBlock>();
            node = GetComponent<PWR_PowerNode>();
            if (slottedBatteryVisual != null) slottedBatteryVisual.SetActive(false);
            if (TryGetComponent(out PWR_Interactable interactable))
                interactable.Used += held =>
                {
                    var item = held != null ? held.GetComponentInParent<PWR_ItemObject>() : null;
                    var socket = GetComponentInChildren<PWR_ItemSocket>();
                    if (item != null && socket != null && socket.TryInsert(item)) return;
                    string msg = Locked ? "Charger offline. Craft a Battery at the Workbench to bring it online."
                        : item != null && item.resource != PWR_ResourceType.Battery
                            ? $"The charger only takes a Battery, not a {PWR_Names.Of(item.resource)}."
                        : IsComplete ? "Charged!"
                        : HasBattery ? $"Charging {ProgressPercent:0}%"
                        : "Nothing to charge. Drop a crafted Battery into the charger.";
                    PWR_Feedback.Show(transform.position + Vector3.up * 0.8f, msg, PWR_Feedback.Info);
                };
        }

        private void Update()
        {
            node.acceptingPower = HasBattery && !IsComplete;
            if (HasBattery && !IsComplete && stats.GetStat(progressStat).IsFull) Complete();
        }

        public bool CanAcceptItem(PWR_ItemObject item) => !Locked && !HasBattery && !IsComplete && item.resource == PWR_ResourceType.Battery;

        public void OnItemAccepted(PWR_ItemObject item)
        {
            HasBattery = true;
            stats.Set(progressStat, 0f);
            if (slottedBatteryVisual != null) slottedBatteryVisual.SetActive(true);
            PWR_Feedback.Show(transform.position + Vector3.up * 0.8f, "Battery inserted", PWR_Feedback.Good);
            BatteryInserted?.Invoke(this);
        }

        private void Complete()
        {
            IsComplete = true;
            HasBattery = false;
            if (slottedBatteryVisual != null) slottedBatteryVisual.SetActive(false);
            if (chargedCellPrefab == null)
                Debug.LogError("[PWR] Cell Charger has no Charged Cell prefab: the station can never be completed.", this);
            else
            {
                Transform at = ejectPoint != null ? ejectPoint : transform;
                var cell = Instantiate(chargedCellPrefab, at.position, at.rotation);
                // Collection is only ever reported by an actual pick-up, so other areas can rely on it.
                if (cell.TryGetComponent(out PWR_Grabbable grabbable)) grabbable.Picked += HandleCellPicked;
                else Debug.LogError("[PWR] Charged Cell has no PWR_Grabbable: it can't be picked up, so the station can never be completed.", cell);
            }
            PWR_Feedback.Show(transform.position + Vector3.up * 0.8f, "Charged Cell ready!", PWR_Feedback.Good);
            Completed?.Invoke(this);
        }

        private void HandleCellPicked(PWR_Grabbable grabbable)
        {
            grabbable.Picked -= HandleCellPicked;
            if (IsCellCollected) return;
            IsCellCollected = true;
            CellCollected?.Invoke(this);
        }
    }
}
