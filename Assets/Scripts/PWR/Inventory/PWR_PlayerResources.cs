using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>The player's local Power Station inventory (rubric: Player Resource).</summary>
    public class PWR_PlayerResources : MonoBehaviour
    {
        public static PWR_PlayerResources Instance { get; private set; }

        [SerializeField] private List<PWR_ResourceAmount> startingResources = new List<PWR_ResourceAmount>
        {
            new PWR_ResourceAmount(PWR_ResourceType.Diesel, 3),
            new PWR_ResourceAmount(PWR_ResourceType.CopperWire, 4),
            new PWR_ResourceAmount(PWR_ResourceType.EmptyCell, 2),
            new PWR_ResourceAmount(PWR_ResourceType.Scrip, 20),
        };

        /// <summary>(type, newAmount, delta)</summary>
        public event Action<PWR_ResourceType, int, int> OnResourceChanged;

        private readonly Dictionary<PWR_ResourceType, int> amounts = new Dictionary<PWR_ResourceType, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            foreach (PWR_ResourceType t in Enum.GetValues(typeof(PWR_ResourceType))) amounts[t] = 0;
            foreach (var r in startingResources) amounts[r.type] += r.amount;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public int Get(PWR_ResourceType type) => amounts.TryGetValue(type, out var v) ? v : 0;

        public void Add(PWR_ResourceType type, int amount)
        {
            if (amount == 0) return;
            int next = Mathf.Max(0, Get(type) + amount);
            int delta = next - Get(type);
            amounts[type] = next;
            OnResourceChanged?.Invoke(type, next, delta);
        }

        public bool CanAfford(IReadOnlyList<PWR_ResourceAmount> costs)
        {
            if (costs == null) return true;
            // Sum per type so duplicate entries in a cost list are handled.
            var need = new Dictionary<PWR_ResourceType, int>();
            foreach (var c in costs)
            {
                need.TryGetValue(c.type, out var n);
                need[c.type] = n + c.amount;
            }
            foreach (var kv in need)
                if (Get(kv.Key) < kv.Value) return false;
            return true;
        }

        public bool TrySpend(IReadOnlyList<PWR_ResourceAmount> costs)
        {
            if (!CanAfford(costs)) return false;
            if (costs != null)
                foreach (var c in costs) Add(c.type, -c.amount);
            return true;
        }

        public bool TrySpend(PWR_ResourceType type, int amount) =>
            TrySpend(new[] { new PWR_ResourceAmount(type, amount) });
    }
}
