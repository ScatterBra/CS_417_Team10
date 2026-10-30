using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// The wall delivery chute (rubric: Intermittent Instances). At random 30–60 s intervals a
    /// fixed batch arrives (2 copper wire + 1 glass), 3 batches in total. Packages wait in the
    /// chute until the player has laid the belts and pressed the chute's start button, then go out
    /// one by one.
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

        [Header("Start button")]
        [Tooltip("Packages stay in the chute until the player presses the button (once the belts are laid).")]
        public bool requireStart = true;
        public Renderer startButton;
        public Material buttonIdle;
        public Material buttonRunning;
        [Tooltip("Seconds a second press has to come within to start a line that doesn't reach the bin.")]
        public float forceStartWindow = 5f;

        public static event Action<PWR_DeliveryChute> BatchArrived;
        public static event Action<PWR_DeliveryChute> Started;

        public int Queued => queue.Count;
        public float SecondsToNextBatch => Mathf.Max(0f, batchTimer);
        public int BatchesDelivered { get; private set; }
        public bool Running { get; private set; }

        private readonly Queue<PWR_ResourceType> queue = new Queue<PWR_ResourceType>();
        private readonly HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        private float batchTimer;
        private float releaseTimer;
        private float forceStartUntil = -1f;

        private void Awake()
        {
            if (TryGetComponent(out PWR_Interactable interactable)) interactable.Used += _ => PressStart();
        }

        private void Start()
        {
            batchTimer = UnityEngine.Random.Range(intervalRange.x, intervalRange.y);
            Running = !requireStart;
            UpdateButton();
            UpdateLabel();
        }

        /// <summary>The start button: checks the line, then lets the queued packages out.</summary>
        public void PressStart()
        {
            Vector3 at = transform.position + Vector3.up * 1.4f;
            if (Running)
            {
                PWR_Feedback.Show(at, "The conveyor is already running. Packages come out as each delivery arrives.", PWR_Feedback.Info);
                return;
            }
            if (BeltAtOutput() == null)
            {
                PWR_Feedback.Show(at, "Nothing to carry the supplies yet. Build a Conveyor Belt under the chute first.", PWR_Feedback.Bad);
                return;
            }
            // A line that ends short of the bin would dump the packages on the floor; warn once, and a
            // second press starts anyway (so a layout this check misjudges can never block the level).
            if (!RouteReachesBin() && Time.time > forceStartUntil)
            {
                forceStartUntil = Time.time + forceStartWindow;
                PWR_Feedback.Show(at, "The belts don't reach the Receiving Bin yet. Extend the line to the bin, or press again to start anyway.", PWR_Feedback.Bad);
                return;
            }
            Running = true;
            UpdateButton();
            UpdateLabel();
            PWR_Feedback.Show(at, "Conveyor started", PWR_Feedback.Good);
            Started?.Invoke(this);
        }

        /// <summary>Follows the belts from the chute and checks that the last one drops into a bin.</summary>
        public bool RouteReachesBin()
        {
            var grid = PWR_GridManager.Instance;
            var belt = BeltAtOutput();
            if (grid == null || belt == null) return false;
            visited.Clear();
            Vector2Int cell = belt.Building.Cell;
            Vector3 dir = belt.Direction;
            while (belt != null)
            {
                if (!visited.Add(cell)) return false; // the belts loop back on themselves
                dir = belt.Direction;
                cell += new Vector2Int(Mathf.RoundToInt(dir.x), Mathf.RoundToInt(dir.z));
                belt = grid.Find<PWR_ConveyorBelt>(cell);
            }
            // A package leaves the last belt at its speed and lands a little past the belt's end.
            Vector3 drop = grid.CellToWorld(cell) - dir * 0.3f + Vector3.up * 0.4f;
            foreach (var bin in FindObjectsByType<PWR_ReceivingBin>(FindObjectsSortMode.None))
                foreach (var col in bin.GetComponentsInChildren<Collider>())
                    if (col.isTrigger && col.bounds.Contains(drop)) return true;
            return false;
        }

        private void UpdateButton()
        {
            if (startButton != null)
                startButton.sharedMaterial = Running ? buttonRunning : buttonIdle;
        }

        private void Update()
        {
            if (BatchesDelivered < totalBatches)
            {
                batchTimer -= Time.deltaTime;
                if (batchTimer <= 0f) DeliverBatch();
            }

            releaseTimer -= Time.deltaTime;
            if (Running && queue.Count > 0 && releaseTimer <= 0f && BeltAtOutput() != null)
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
            string waiting = queue.Count > 0 ? $"Waiting: {queue.Count}\n" : "";
            string hint = BeltAtOutput() == null ? "Build a belt here\n"
                : !Running ? "Belts done? Press the button\n"
                : "";
            label.extraLine = waiting + hint + next;
        }

        private void LateUpdate()
        {
            if (queue.Count > 0 || !Running) UpdateLabel();
        }
    }
}
