using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deadzone.Power
{
    public enum PWR_PowerRole
    {
        Source,   // generator, solar panel: pushes to Storage and Sink
        Storage,  // battery bank: receives from Source, pushes to Sink
        Sink,     // charger, lamp, refinery: only receives
    }

    /// <summary>
    /// Grid neighbor energy exchange (rubric: Neighbor Interactions). Every tick, this node
    /// pushes energy into adjacent nodes it may feed: its own stat goes down, the
    /// receiver's goes up. Transfers stop when the source is empty or the receiver is full.
    /// </summary>
    [RequireComponent(typeof(PWR_StatBlock), typeof(PWR_GridBuilding))]
    public class PWR_PowerNode : MonoBehaviour
    {
        public PWR_PowerRole role = PWR_PowerRole.Storage;

        [Tooltip("Stat that holds this node's energy.")]
        public string energyStat = "Charge";

        [Tooltip("Max energy pushed to each neighbor per tick.")]
        public float transferPerTick = 5f;
        public float tickInterval = 0.5f;

        [Tooltip("Turn off to cut this node from the network (e.g. broken, switched off).")]
        public bool online = true;

        /// <summary>(from, to, amount) — hook cable VFX / tutorials here.</summary>
        public static event Action<PWR_PowerNode, PWR_PowerNode, float> OnTransfer;

        public PWR_StatBlock Stats { get; private set; }
        public PWR_GridBuilding Building { get; private set; }

        private readonly List<PWR_PowerNode> neighbors = new List<PWR_PowerNode>();
        private float timer;

        private void Awake()
        {
            Stats = GetComponent<PWR_StatBlock>();
            Building = GetComponent<PWR_GridBuilding>();
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < tickInterval) return;
            timer -= tickInterval;
            Tick();
        }

        private void Tick()
        {
            if (!online || role == PWR_PowerRole.Sink) return;
            var grid = PWR_GridManager.Instance;
            if (grid == null || !Building.IsRegistered) return;

            grid.GetNeighbors(Building.Cell, neighbors);
            foreach (var other in neighbors)
            {
                if (other == this || !other.online || !CanFeed(other)) continue;

                float available = Stats.Get(energyStat);
                if (available <= 0f) break;

                float amount = Mathf.Min(transferPerTick, available, other.Stats.GetSpace(other.energyStat));
                if (amount <= 0f) continue;

                Stats.Add(energyStat, -amount);
                other.Stats.Add(other.energyStat, amount);
                OnTransfer?.Invoke(this, other, amount);
            }
        }

        private bool CanFeed(PWR_PowerNode other)
        {
            switch (role)
            {
                case PWR_PowerRole.Source: return other.role != PWR_PowerRole.Source;
                case PWR_PowerRole.Storage: return other.role == PWR_PowerRole.Sink;
                default: return false;
            }
        }
    }
}
