using System;
using UnityEngine;

namespace Deadzone.Power
{
    public enum PWR_Stage
    {
        RestoreLight,   // place the Battery Bank so the Floodlight turns on
        GeneratePower,  // build a generator and get it running
        SupplyLine,     // conveyor from the chute to the bin, collect the supplies
        CraftBattery,   // craft a Battery at the Workbench
        ChargeCell,     // charge it, replace the bulb, pick up the Charged Cell
        Online,
    }

    /// <summary>
    /// Rubric: Mastery Thresholding, Unlock Reveal, Thresholds on the Horizon. The station starts with
    /// only the Battery Bank; each threshold shows a reveal card that introduces the next system, then
    /// releases it a moment later (build menu entries, spawns, recipes, machines, resource rows).
    /// <see cref="NextGoal"/> always names the next threshold to aim for.
    /// Added at runtime by PWR_LevelDirector, so the scene needs no extra setup.
    /// </summary>
    public class PWR_Progression : MonoBehaviour
    {
        public static PWR_Progression Instance { get; private set; }

        [Tooltip("Seconds between the reveal card appearing and the content unlocking.")]
        public float unlockDelay = 2.5f;
        public float revealSeconds = 9f;

        public PWR_Stage Stage { get; private set; } = PWR_Stage.RestoreLight;
        public static event Action<PWR_Stage> StageChanged;

        private PWR_Stage preparingStage;
        public string NextGoal => unlocking ? "Preparing " + StageName(preparingStage) + " — available in a moment." : CurrentGoal();

        private static string StageName(PWR_Stage stage) => stage switch
        {
            PWR_Stage.GeneratePower => "generators and fuel crates",
            PWR_Stage.SupplyLine => "the supply chute, bin and belts",
            PWR_Stage.CraftBattery => "Workbench recipes and Electrolyte Tank",
            PWR_Stage.ChargeCell => "the Cell Charger",
            _ => "the next system"
        };
        private static bool XR => PWR_PlayerRig.Instance != null && PWR_PlayerRig.Instance.XRActive;
        private static string UseHint => XR ? "empty hand: aim + index trigger" : "aim + E";
        private string CurrentGoal()
        {
            var res = PWR_PlayerResources.Instance;
            var bs = PWR_BuildSystem.Instance;
            if (Stage == PWR_Stage.RestoreLight && bs != null && bs.CountBuilt("BatteryBank") > 0)
                return "Floodlight still dark: place the Battery Bank within 2 cells of the Floodlight.";
            if (Stage == PWR_Stage.GeneratePower && bs != null && res != null)
            {
                bool coal = bs.CountBuilt("CoalGenerator") > 0;
                bool oil = bs.CountBuilt("OilGenerator") > 0;
                if (!coal && !oil) return "Build a Coal Generator beside the Battery Bank (Y menu).";
                if (coal && res.Get(PWR_ResourceType.Coal) > 0) return "Fuel the Coal Generator: " + UseHint + ". One use loads 1 Coal.";
                if (oil && res.Get(PWR_ResourceType.RefinedOil) > 0) return "Fuel the Oil Generator: " + UseHint + ".";
                if (coal) return "Get Coal: take the Wrench, strike a Wooden Crate (or trigger with Wrench nearby), then pick up the Coal.";
                if (res.Get(PWR_ResourceType.CrudeOil) > 0) return "At the Workbench choose Refine Oil; then use the Oil Generator to load it.";
                return "Use an Oil Barrel (" + UseHint + "), then choose Refine Oil at the Workbench.";
            }
            if (Stage == PWR_Stage.SupplyLine)
            {
                var receiver = bin != null ? bin.GetComponent<PWR_ReceivingBin>() : null;
                if (receiver != null && receiver.StoredCount > 0)
                    return $"{receiver.StoredCount} package(s) in Receiving Bin. COLLECT: {UseHint}. Collect now; no minimum fill.";
                var delivery = chute != null ? chute.GetComponent<PWR_DeliveryChute>() : null;
                if (delivery != null && delivery.Running && delivery.BatchesDelivered == 0)
                    return $"Chute started. First supplies arrive in about {Mathf.CeilToInt(delivery.SecondsToNextBatch)}s. Then collect them from the bin.";
                if (delivery != null && delivery.Running)
                    return "Move packages into the Receiving Bin (belts or by hand), then USE the bin to collect.";
                if (delivery != null && delivery.RouteReachesBin())
                    return "Start the Delivery Chute: " + UseHint + ". Then collect from the Receiving Bin.";
                return "Lay Concrete Pads + belts from the chute to the bin. Aim belt arrows toward the bin; then USE the chute to start.";
            }
            if (Stage == PWR_Stage.CraftBattery)
            {
                if (workbench != null && workbench.IsBusy) return "Workbench crafting — wait for its progress to finish.";
                if (res != null && res.Get(PWR_ResourceType.Fuse) == 0) return RecipeGoal(PWR_ResourceType.Fuse);
                return RecipeGoal(PWR_ResourceType.Battery);
            }
            if (Stage == PWR_Stage.ChargeCell && director != null)
            {
                var light = director.floodlight;
                var charger = director.charger;
                if (light != null && !light.HasBulb)
                    return res != null && res.Get(PWR_ResourceType.LightBulb) > 0
                        ? "Grip the crafted Light Bulb from the Workbench; release it into the Floodlight socket."
                        : RecipeGoal(PWR_ResourceType.LightBulb);
                if (charger != null && charger.IsComplete && !charger.IsCellCollected)
                    return "Grip the Charged Cell ejected by the charger. Picking up this exact output completes the collection.";
                if (charger != null && !charger.HasBattery && !charger.IsComplete)
                    return "Grip the physical Battery from the Workbench; release it inside the Cell Charger slot.";
                if (charger != null && charger.HasBattery)
                    return $"Charging: {charger.ProgressPercent:0}%. Keep the generators fuelled and the Battery Bank beside the charger.";
            }
            return GoalFor(Stage);
        }
        private string RecipeGoal(PWR_ResourceType output)
        {
            if (workbench == null) return "Use the Workbench to craft " + PWR_Names.Of(output) + ".";
            var res = PWR_PlayerResources.Instance;
            foreach (var recipe in workbench.recipes)
            {
                if (recipe.output != output) continue;
                foreach (var input in recipe.inputs)
                {
                    int missing = input.amount - (res != null ? res.Get(input.type) : 0);
                    if (missing > 0) return $"For {recipe.name}: need {missing} {PWR_Names.Of(input.type)}. " +
                        (input.type == PWR_ResourceType.Electrolyte ? "USE the Electrolyte Tank (" + UseHint + ")." : PWR_Names.HowToGet(input.type));
                }
                return "Workbench: select " + recipe.name + " (" + (XR ? "point + index trigger" : "click the recipe") + ").";
            }
            return "Use the Workbench to craft " + PWR_Names.Of(output) + ".";
        }

        public static string GoalFor(PWR_Stage stage)
        {
            switch (stage)
            {
                case PWR_Stage.RestoreLight: return "Open the build menu (Y) and place the Battery Bank near the Floodlight.";
                case PWR_Stage.GeneratePower: return "Build a generator next to the Battery Bank and load it with fuel.";
                case PWR_Stage.SupplyLine: return "Lay belts from the Delivery Chute to the Receiving Bin and collect the supplies.";
                case PWR_Stage.CraftBattery: return "Craft a Battery (Fuse + Electrolyte) at the Workbench.";
                case PWR_Stage.ChargeCell: return "Charge the Battery in the Cell Charger. Keep the generators fuelled.";
                default:
                    var dock = PWR_DefenseCellDock.Instance;
                    return dock != null && !dock.IsDocked
                        ? "Take the Charged Cell to the Cell Dock in the Defense Yard. Complete Clinic and Workshop to unlock Defense training."
                        : "Complete Clinic and Workshop to unlock Defense training.";
            }
        }

        private PWR_LevelDirector director;
        private PWR_WildSpawner spawner;
        private PWR_Workbench workbench;
        private GameObject chute, bin, tank;
        private PWR_TutorialTrigger chargerTutorial;
        private bool unlocking;

        private void Awake()
        {
            Instance = this;
            director = GetComponent<PWR_LevelDirector>();
            spawner = FindFirstObjectByType<PWR_WildSpawner>();
            if (spawner != null) spawner.spawnOnStart = false;
            workbench = FindFirstObjectByType<PWR_Workbench>();
            chute = Find<PWR_DeliveryChute>();
            bin = Find<PWR_ReceivingBin>();
            tank = FindTank();
            SetActive(chute, false);
            SetActive(bin, false);
            SetActive(tank, false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            var bs = PWR_BuildSystem.Instance;
            if (bs != null)
            {
                const string afterLight = "Power the Floodlight with the Battery Bank to unlock it.";
                const string afterGenerator = "Get a generator running to unlock it.";
                bs.Lock("CoalGenerator", afterLight);
                bs.Lock("OilGenerator", afterLight);
                bs.Lock("ConcretePad", afterGenerator);
                bs.Lock("ConveyorBelt", afterGenerator);
            }
            if (workbench != null)
                foreach (var r in workbench.recipes) r.locked = true;
            var charger = director != null ? director.charger : null;
            if (charger != null)
            {
                charger.Locked = true;
                chargerTutorial = charger.GetComponent<PWR_TutorialTrigger>();
                if (chargerTutorial != null) chargerTutorial.enabled = false;
            }
        }

        private void OnEnable()
        {
            PWR_Generator.AnyIgnited += HandleIgnited;
            PWR_ReceivingBin.Collected += HandleBinCollected;
            PWR_Workbench.Crafted += HandleCrafted;
            PWR_LevelDirector.StationCompleted += HandleOnline;
        }

        private void OnDisable()
        {
            PWR_Generator.AnyIgnited -= HandleIgnited;
            PWR_ReceivingBin.Collected -= HandleBinCollected;
            PWR_Workbench.Crafted -= HandleCrafted;
            PWR_LevelDirector.StationCompleted -= HandleOnline;
        }

        private void Update()
        {
            if (Stage == PWR_Stage.RestoreLight && !unlocking && director != null && director.floodlight != null && director.floodlight.IsLit)
                Advance(PWR_Stage.GeneratePower,
                    "BACKUP GENERATORS\nThe battery bank won't last forever. Generators burn Coal or Refined Oil to recharge it.\n" +
                    "Fuel barrels and wooden crates have been spotted around the station.",
                    () =>
                    {
                        Unlock("CoalGenerator", "OilGenerator");
                        if (spawner != null) spawner.SpawnBarrelsAndCrates();
                        SetRecipes(false, "Refine Oil");
                    });
        }

        // ---------- thresholds ----------

        private void HandleIgnited(PWR_Generator g)
        {
            if (Stage != PWR_Stage.GeneratePower || unlocking) return;
            Advance(PWR_Stage.SupplyLine,
                "SUPPLY LINE\nParts arrive through the Delivery Chute. Lay concrete pads and conveyor belts, like a factory floor, " +
                "to carry them to the Receiving Bin.\nScrap metal for the belts has turned up around the station.",
                () =>
                {
                    Unlock("ConcretePad", "ConveyorBelt");
                    if (spawner != null) spawner.SpawnScrap();
                    SetActive(chute, true);
                    SetActive(bin, true);
                });
        }

        private void HandleBinCollected(PWR_ReceivingBin b)
        {
            if (Stage != PWR_Stage.SupplyLine || unlocking) return;
            Advance(PWR_Stage.CraftBattery,
                "BATTERY WORKSHOP\nCopper Wire and Glass can now be assembled at the Workbench into Fuses, Batteries and Light Bulbs.\n" +
                "An Electrolyte Tank has been set up next to the bench.",
                () =>
                {
                    SetRecipes(false, "Make Fuse", "Assemble Battery", "Make Light Bulb");
                    SetActive(tank, true);
                });
        }

        private void HandleCrafted(PWR_Workbench w, PWR_Recipe recipe)
        {
            if (Stage != PWR_Stage.CraftBattery || unlocking || recipe.output != PWR_ResourceType.Battery) return;
            Advance(PWR_Stage.ChargeCell,
                "CELL CHARGER ONLINE\nPlug the Battery into the charger. The Battery Bank fills it like a power bank, " +
                "as long as the generators keep running.",
                () =>
                {
                    var charger = director != null ? director.charger : null;
                    if (charger != null) charger.Locked = false;
                    if (chargerTutorial != null) chargerTutorial.enabled = true;
                });
        }

        private void HandleOnline()
        {
            // PWR_LevelDirector shows the STATION ONLINE message; just move the goal on.
            SetStage(PWR_Stage.Online);
        }

        // ---------- helpers ----------

        /// <summary>Reveal card first, content a moment later (the reveal introduces it before its UI appears).</summary>
        private void Advance(PWR_Stage next, string reveal, Action unlock)
        {
            unlocking = true;
            preparingStage = next;
            ShowReveal(next, $"{reveal}\n\n<color=#FFC840>Next goal:</color> {GoalFor(next)}");
            StartCoroutine(UnlockLater(next, unlock));
        }

        private System.Collections.IEnumerator UnlockLater(PWR_Stage next, Action unlock)
        {
            yield return new WaitForSeconds(unlockDelay);
            unlock?.Invoke();
            unlocking = false;
            SetStage(next);
        }

        private void SetStage(PWR_Stage stage)
        {
            if (Stage == stage) return;
            Stage = stage;
            StageChanged?.Invoke(stage);
        }

        private void ShowReveal(PWR_Stage next, string message)
        {
            var mgr = PWR_TutorialManager.Instance;
            var cam = Camera.main;
            if (mgr == null || cam == null) return;
            // A fixed spot in front of the player (not following the head) so it can be read calmly.
            var spot = new GameObject("PWR_RevealSpot").transform;
            Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            spot.position = cam.transform.position + forward * 1.4f;
            mgr.ShowMessage(message, spot, Vector3.zero, revealSeconds, relevantStage: next);
            Destroy(spot.gameObject, revealSeconds + 0.5f);
        }

        private static void Unlock(params string[] buildingIds)
        {
            var bs = PWR_BuildSystem.Instance;
            if (bs == null) return;
            foreach (var id in buildingIds) bs.Unlock(id);
        }

        private void SetRecipes(bool locked, params string[] names)
        {
            if (workbench == null) return;
            foreach (var n in names) workbench.SetRecipeLocked(n, locked);
        }

        /// <summary>Wrist menu rows: a resource appears once the stage that introduces it is reached.</summary>
        public static bool IsResourceVisible(PWR_ResourceType type)
        {
            var p = Instance;
            if (p == null) return true;
            switch (type)
            {
                case PWR_ResourceType.CrudeOil:
                case PWR_ResourceType.RefinedOil:
                case PWR_ResourceType.Coal:
                    return p.Stage >= PWR_Stage.GeneratePower;
                case PWR_ResourceType.Scrap:
                case PWR_ResourceType.CopperWire:
                case PWR_ResourceType.Glass:
                    return p.Stage >= PWR_Stage.SupplyLine;
                default:
                    return p.Stage >= PWR_Stage.CraftBattery;
            }
        }

        private static GameObject Find<T>() where T : Component
        {
            var c = FindFirstObjectByType<T>(FindObjectsInactive.Include);
            return c != null ? c.gameObject : null;
        }

        private static GameObject FindTank()
        {
            foreach (var h in FindObjectsByType<PWR_Harvestable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (h.resource == PWR_ResourceType.Electrolyte) return h.gameObject;
            return null;
        }

        private static void SetActive(GameObject go, bool on)
        {
            if (go != null && go.activeSelf != on) go.SetActive(on);
        }
    }
}
