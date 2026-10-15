using System;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// The charger. Drop a crafted battery into its slot; it then draws power from the neighboring
    /// battery bank into its Progress stat. At 100% the battery pops out as a Charged Cell and the
    /// level is complete.
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

        public bool HasBattery { get; private set; }
        public bool IsComplete { get; private set; }
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
                    string msg = IsComplete ? "Charged!" : HasBattery ? $"Charging {ProgressPercent:0}%" : "Insert a Battery";
                    PWR_Feedback.Show(transform.position + Vector3.up * 0.8f, msg, PWR_Feedback.Info);
                };
        }

        private void Update()
        {
            node.acceptingPower = HasBattery && !IsComplete;
            if (HasBattery && !IsComplete && stats.GetStat(progressStat).IsFull) Complete();
        }

        public bool CanAcceptItem(PWR_ItemObject item) => !HasBattery && !IsComplete && item.resource == PWR_ResourceType.Battery;

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
            if (chargedCellPrefab != null)
            {
                Transform at = ejectPoint != null ? ejectPoint : transform;
                Instantiate(chargedCellPrefab, at.position, at.rotation);
            }
            PWR_Feedback.Show(transform.position + Vector3.up * 0.8f, "Charged Cell ready!", PWR_Feedback.Good);
            Completed?.Invoke(this);
        }
    }
}
