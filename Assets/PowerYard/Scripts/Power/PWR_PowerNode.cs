using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deadzone.Power
{
    public enum PWR_PowerRole
    {
        Source,   // generators: push to Storage only
        Storage,  // battery bank: receives from Source, pushes to Sink
        Sink,     // charger, floodlight: only receive
    }

    /// <summary>
    /// Grid neighbor energy exchange (rubric: Neighbor Interactions). Every tick, this node
    /// pushes energy into nearby nodes it may feed: its own stat goes down, the receiver's
    /// goes up. Transfers stop when the source is empty or the receiver is full / not accepting.
    /// </summary>
    [RequireComponent(typeof(PWR_StatBlock), typeof(PWR_GridBuilding))]
    public class PWR_PowerNode : MonoBehaviour
    {
        public PWR_PowerRole role = PWR_PowerRole.Storage;

        [Tooltip("Stat that holds this node's energy (for a Sink: the stat that power fills).")]
        public string energyStat = "Charge";

        [Tooltip("Max energy pushed to each neighbor per tick.")]
        public float transferPerTick = 3f;

        [Tooltip("Max energy this node accepts from one neighbor per tick.")]
        public float maxReceivePerTick = 1000f;

        public float tickInterval = 0.5f;

        [Tooltip("Turn off to cut this node from the network.")]
        public bool online = true;

        [Tooltip("Sinks can refuse power (e.g. a charger without a battery, a lamp without a bulb).")]
        public bool acceptingPower = true;

        /// <summary>(from, to, amount) — hook VFX / tutorials here.</summary>
        public static event Action<PWR_PowerNode, PWR_PowerNode, float> OnTransfer;

        public PWR_StatBlock Stats { get; private set; }
        public PWR_GridBuilding Building { get; private set; }

        /// <summary>Energy received during the last tick (for "is powered" visuals).</summary>
        public float LastReceived { get; private set; }

        private readonly List<PWR_PowerNode> neighbors = new List<PWR_PowerNode>();
        private float timer;
        private float receivedThisTick;

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
            LastReceived = receivedThisTick;
            receivedThisTick = 0f;
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
                if (other == null || other == this || !CanFeed(other)) continue;

                float available = Stats.Get(energyStat);
                if (available <= 0f) break;

                float amount = Mathf.Min(transferPerTick, other.maxReceivePerTick, available,
                    other.Stats.GetSpace(other.energyStat));
                if (amount <= 0f) continue;

                Stats.Add(energyStat, -amount);
                other.Stats.Add(other.energyStat, amount);
                other.receivedThisTick += amount;
                OnTransfer?.Invoke(this, other, amount);
            }
        }

        private bool CanFeed(PWR_PowerNode other) =>
            other.online && other.acceptingPower && Feeds(role, other.role);

        /// <summary>Power flows generator -> battery bank -> charger / floodlight, never skipping the bank.</summary>
        public static bool Feeds(PWR_PowerRole from, PWR_PowerRole to)
        {
            switch (from)
            {
                case PWR_PowerRole.Source: return to == PWR_PowerRole.Storage;
                case PWR_PowerRole.Storage: return to == PWR_PowerRole.Sink;
                default: return false;
            }
        }
    }
}
