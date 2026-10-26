using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// Collection bin at the end of the conveyor. A package that enters its trigger is destroyed and
    /// the bin's Stock rises (rubric: Collisions). Pulling the trigger on the bin moves its stock into
    /// the player's resources (Harvesting).
    /// </summary>
    [RequireComponent(typeof(PWR_StatBlock))]
    public class PWR_ReceivingBin : MonoBehaviour
    {
        public string stockStat = "Stock";

        public static event Action<PWR_ReceivingBin> CargoReceived;
        /// <summary>The player emptied the bin into their resources.</summary>
        public static event Action<PWR_ReceivingBin> Collected;

        private PWR_StatBlock stats;
        private readonly Dictionary<PWR_ResourceType, int> contents = new Dictionary<PWR_ResourceType, int>();

        private void Awake()
        {
            stats = GetComponent<PWR_StatBlock>();
            if (TryGetComponent(out PWR_Interactable interactable)) interactable.Used += _ => TakeAll();
        }

        private void OnTriggerEnter(Collider other)
        {
            var cargo = other.GetComponentInParent<PWR_Cargo>();
            if (cargo == null || cargo.Delivered) return;
            cargo.Delivered = true;
            contents.TryGetValue(cargo.resource, out int n);
            contents[cargo.resource] = n + 1;
            stats.Add(stockStat, 1f);
            PWR_Feedback.Show(transform.position + Vector3.up * 1.1f, $"{PWR_Names.Of(cargo.resource)} received", PWR_Feedback.Info);
            if (cargo.TryGetComponent(out PWR_Grabbable grabbable)) grabbable.ForceRelease();
            Destroy(cargo.gameObject);
            CargoReceived?.Invoke(this);
        }

        public int TakeAll()
        {
            var res = PWR_PlayerResources.Instance;
            if (res == null) return 0;
            int total = 0;
            var parts = new List<string>();
            foreach (var kv in contents)
            {
                if (kv.Value <= 0) continue;
                res.Add(kv.Key, kv.Value);
                total += kv.Value;
                parts.Add($"+{kv.Value} {PWR_Names.Of(kv.Key)}");
            }
            contents.Clear();
            if (total == 0)
            {
                PWR_Feedback.Show(transform.position + Vector3.up * 1.1f, "The bin is empty. Packages land here once they ride the conveyor from the chute.", PWR_Feedback.Bad);
                return 0;
            }
            stats.Add(stockStat, -total);
            PWR_Feedback.Show(transform.position + Vector3.up * 1.1f, string.Join("  ", parts), PWR_Feedback.Good);
            Collected?.Invoke(this);
            return total;
        }
    }
}
