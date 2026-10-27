using System;
using System.Collections.Generic;
using UnityEngine;

// Every failed-action message in the Clinic lives here, so each one is distinct and says WHY the action failed AND
// how to fix it ("Error Tolerant Lessons"). Fail() shows the floating text and counts the message as a catch.
public static class MED_Messages
{
    static readonly Color FailColor = new Color(1f, 0.6f, 0.4f);
    static readonly HashSet<string> distinct = new HashSet<string>();

    /// <summary>Total number of catches (failed actions explained) this session.</summary>
    public static int CatchCount { get; private set; }
    /// <summary>Number of different catch messages seen this session.</summary>
    public static int DistinctCatchCount => distinct.Count;
    public static event Action<string> Caught;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        distinct.Clear();
        CatchCount = 0;
        Caught = null;
    }

    /// <summary>Shows the message at the position and records a catch. Always returns false so callers can "return Fail(...)".</summary>
    public static bool Fail(Vector3 position, string message)
    {
        Record(message);
        MED_FloatingText.Spawn(position, message, FailColor, 0.5f);
        return false;
    }

    public static void Record(string message)
    {
        CatchCount++;
        distinct.Add(message);
        Caught?.Invoke(message);
    }

    // ---------- Shared helpers ----------

    public static string HowToGet(MED_ResourceType type)
    {
        switch (type)
        {
            case MED_ResourceType.Herbs: return "cut herb patches with the Sickle";
            case MED_ResourceType.Water: return "dip the Bucket in the Water Barrel";
            case MED_ResourceType.Alcohol:
            case MED_ResourceType.Gauze: return "crack supply crates with the Crowbar";
            case MED_ResourceType.Health: return "use the Treatment Station";
            default: return "craft it at the Prep Bench";
        }
    }

    /// <summary>"Not enough Herbs (0/2) - cut herb patches with the Sickle" for the first missing input of a recipe.</summary>
    public static string Missing(MED_ResourceType type, int have, int need)
        => $"Not enough {type} ({have}/{need}) - {HowToGet(type)}";

    public static string Locked(MED_Feature feature) => "Locked - " + MED_ProgressionManager.RequirementFor(feature);

    // ---------- Prep Bench ----------
    public const string BenchBusy = "Bench is busy preparing - wait for the bar to fill";
    public const string BenchTooFar = "Too far from the Prep Bench - step within 3 m to craft";

    // ---------- Treatment Station ----------
    public const string RefillTooFar = "Too far from the Treatment Station - step closer to REFILL";
    public const string StationFull = "Station is full - treat yourself on the pad to use some up";
    public const string RefillNoMedicine = "No Medicine to refill with - craft some at the Prep Bench";
    public const string TreatTooFar = "Too far to treat - walk up to the Treatment Station";
    public const string NotOnPad = "Not on the pad - step onto the green pad, then press TREAT";
    public const string HealthFull = "Health is already full - come back after taking damage";
    public const string StationEmpty = "Station is empty - press REFILL with Medicine first";
    public const string TreatCancelled = "Treatment cancelled, you left the pad - step back on and press TREAT";
    public const string RanOutMidTreatment = "Out of Medicine mid-treatment - press REFILL to continue";

    // ---------- Medicine Cabinet ----------
    public const string StockTooFar = "Too far from the Cabinet - step closer to STOCK";
    public const string CabinetFull = "Cabinet is full - TAKE a bottle out first";
    public const string StockNoMedicine = "No Medicine to stock - craft some at the Prep Bench";
    public const string TakeTooFar = "Too far from the Cabinet - step closer to TAKE";
    public const string CabinetEmpty = "Cabinet is empty - STOCK some Medicine first";

    // ---------- Sterilizer ----------
    public const string SterilizerTooFar = "Too far from the Sterilizer - step closer to START";
    public const string SterilizerBusy = "Sterilizer is running - wait for the cycle to end";
    public const string SterilizerHot = "Sterilizer is still hot - wait for the glow to fade";
    public static string SterilizerMissing(MED_ResourceType type, int have, int need)
        => $"Can't start, short of {type} ({have}/{need}) - {HowToGet(type)}";
    public const string InstrumentTooCold = "Instrument too cold - press START and wait for the glow";
    public const string InstrumentTooHot = "Instrument too hot - let the Sterilizer cool a little";

    // ---------- Herb Planter ----------
    public const string PlantTooFar = "Too far from the Planter - step closer to PLANT";
    public const string AlreadyPlanted = "Already planted - wait for it to ripen, then use the Sickle";
    public const string PlantNoHerbs = "No Herbs to plant - cut a herb patch with the Sickle";
    public const string PlanterWaterTooFar = "Too far from the Planter - step closer to WATER";
    public const string SoilWet = "Soil is already wet - wait until it dries out";
    public const string PlanterNoWater = "No Water to pour - dip the Bucket in the Water Barrel";
    public const string PlanterWrongTool = "Wrong tool - the Sickle cuts the plant";
    public const string NothingPlanted = "Nothing planted yet - press PLANT first";
    public const string NotRipe = "Not ripe yet - keep the soil damp and wait for the glow";
    public const string DemoWrongTool = "Wrong tool - cut the demo plant with the Sickle";
    public const string DemoGrowing = "Demo plant is still growing - wait for it to glow";

    // ---------- Water Barrel ----------
    public const string PourTooFar = "Too far from the Barrel - step closer to POUR";
    public const string BarrelFull = "Barrel is full - take some Water out with the Bucket";
    public const string PourNoWater = "No Water to pour - dip the Bucket in the barrel first";
    public const string BarrelEmpty = "Barrel is empty - wait for rain or POUR some Water in";

    // ---------- Harvestables (herb patches, crates) ----------
    public const string NothingLeft = "Nothing left here - find another one";
    public static string HarvestWrongTool(MED_ResourceType type, MED_ToolType tool) => $"Wrong tool - {type} needs the {tool}";

    // ---------- Supply Cart ----------
    public const string CartTooFar = "Too far from the Cart - walk up to UNLOAD";
    public const string CartArriving = "Cart is still arriving - wait until it stops at the bay";
    public const string CartLeaving = "Cart is leaving - catch the next delivery";
    public const string CartEmpty = "Cart is empty - wait for the next delivery";
    public const string CartAlreadyHere = "Cart already here - go UNLOAD it first";

    // ---------- Building placement ----------
    public const string AimTooHigh = "Aiming too high - tilt the ray down at the floor";
    public const string FloorTooFar = "Floor too far from your hand - walk closer to where you want to build";
    public const string OffGrid = "Outside the clinic grid - aim inside the walls";
    public const string FootprintOffGrid = "Doesn't fit - part of it hangs off the grid, move inward";
    public const string CellReserved = "Reserved spot (wall, door, shelf, path) - pick a free cell";
    public const string CellTaken = "Cell already taken - choose an empty cell or Clear All";
    public static string CantAfford(MED_ResourceType type, int need)
        => $"Can't afford it, need {need} {type} - {HowToGet(type)}";

    // ---------- Sandbox ----------
    public const string TrayEmpty = "Nothing to place - craft a Medicine, then try again";
}
