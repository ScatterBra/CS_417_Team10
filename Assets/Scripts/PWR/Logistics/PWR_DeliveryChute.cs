using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// The wall delivery chute (rubric: Intermittent Instances). At random 30–60 s intervals a
    /// fixed batch arrives (2 copper wire + 1 glass), 3 batches in total. Packages wait in the
    /// chute until a conveyor belt is built on the chute's cell, then go out one by one.
    /// </summary>
    public class PWR_DeliveryChute : MonoBehaviour
    {
        [Serializable]
        public struct CargoEntry
        {
            public PWR_ResourceType resource;
            public int count;
        }

        public List<CargoEntry> batch = new List<CargoEntry>
        {
            new CargoEntry { resource = PWR_ResourceType.CopperWire, count = 2 },
            new CargoEntry { resource = PWR_ResourceType.Glass, count = 1 },
        };
        public int totalBatches = 3;
        public Vector2 intervalRange = new Vector2(30f, 60f);
        public float releaseSpacing = 1.6f;

        [Tooltip("Where packages appear; should be above the chute's grid cell.")]
        public Transform outputPoint;
        public PWR_Cargo copperWirePrefab;
        public PWR_Cargo glassPrefab;
        public PWR_StatLabel label;

        public static event Action<PWR_DeliveryChute> BatchArrived;

        public int Queued => queue.Count;
        public int BatchesDelivered { get; private set; }

        private readonly Queue<PWR_ResourceType> queue = new Queue<PWR_ResourceType>();
        private float batchTimer;
        private float releaseTimer;

        private void Start()
        {
            batchTimer = UnityEngine.Random.Range(intervalRange.x, intervalRange.y);
            UpdateLabel();
        }

        private void Update()
        {
            if (BatchesDelivered < totalBatches)
            {
                batchTimer -= Time.deltaTime;
                if (batchTimer <= 0f) DeliverBatch();
            }

            releaseTimer -= Time.deltaTime;
            if (queue.Count > 0 && releaseTimer <= 0f && BeltAtOutput() != null)
            {
                releaseTimer = releaseSpacing;
                Release(queue.Dequeue());
            }
        }

        /// <summary>Delivers the next batch now (also used to test without waiting).</summary>
        public void DeliverBatch()
        {
            if (BatchesDelivered >= totalBatches) return;
            BatchesDelivered++;
            batchTimer = UnityEngine.Random.Range(intervalRange.x, intervalRange.y);
            foreach (var e in batch)
                for (int i = 0; i < e.count; i++) queue.Enqueue(e.resource);
            PWR_Feedback.Show(OutputPosition + Vector3.up * 0.6f, "Supplies arrived", PWR_Feedback.Info);
            BatchArrived?.Invoke(this);
            UpdateLabel();
        }

        private Vector3 OutputPosition => outputPoint != null ? outputPoint.position : transform.position;

        private PWR_ConveyorBelt BeltAtOutput()
        {
            var grid = PWR_GridManager.Instance;
            return grid == null ? null : grid.Find<PWR_ConveyorBelt>(grid.WorldToCell(OutputPosition));
        }

        private void Release(PWR_ResourceType type)
        {
            var prefab = type == PWR_ResourceType.Glass ? glassPrefab : copperWirePrefab;
            if (prefab == null) return;
            var belt = BeltAtOutput();
            var grid = PWR_GridManager.Instance;
            Vector3 pos = grid.CellToWorld(belt.Building.Cell);
            pos.y = belt.SurfaceY + prefab.halfHeight;
            var cargo = Instantiate(prefab, pos, Quaternion.LookRotation(belt.Direction));
            cargo.resource = type;
            cargo.StartRiding();
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (label == null) return;
            string next = BatchesDelivered < totalBatches ? $"Batches: {BatchesDelivered}/{totalBatches}" : "All batches delivered";
            label.extraLine = queue.Count > 0
                ? $"Waiting: {queue.Count}" + (BeltAtOutput() == null ? " (build a belt here)" : "") + $"\n{next}"
                : next;
        }

        private void LateUpdate()
        {
            if (queue.Count > 0) UpdateLabel();
        }
    }
}
