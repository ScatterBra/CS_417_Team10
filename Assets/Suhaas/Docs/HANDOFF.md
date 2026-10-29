# Clinic (Suhaas) - Handoff Note

Owner prefix: `MED_` - No namespace (the Clinic code is global, unlike `Deadzone.Power`) - Development scene: `Assets/Development/Scenes/Clinic/MED_Clinic.unity`

## What it produces
- **Health.** The Clinic's output is the player's restored health (`MED_PlayerResources.Health`, 0-100): the
  Treatment Station heals 20 HP per Medicine dose. At integration `MED_PlayerResources.Health` is to be replaced
  by the Defense Yard's player health component (see the note in `MED_PlayerResources.cs`).
- **Medicine and other items** the player carries as counts: Medicine, BandageKit, Antiseptic, Painkiller, Tonic.
  There is no physical item to carry between rooms yet.

## Completion check (for other areas)
The Clinic is complete once the player has been healed to full (100) after clearing the four earlier thresholds.
- `MED_ProgressionManager.ClinicComplete` - static bool, check it any time.
- `MED_ProgressionManager.ClinicCompleted` - static event, fired once when it becomes true (RESET PROGRESSION
  clears the flag, so it can fire again after a reset).
- `MED_ProgressionManager.Instance.NextGoal` - string for the current goal with its progress,
  e.g. `Heal to 100: 60/100`. `"Clinic complete - every station is unlocked"` at the end.
- `MED_ProgressionManager.Instance.CurrentThreshold` - number of thresholds cleared, 0 to 5.
- `MED_ProgressionManager.GoalChanged` / `Changed` - static events for goal text / unlock changes.

## Progression (MP2B)
`MED_ProgressionManager` (a scene object, `MED_Progression`) gates the Clinic in five thresholds:

| # | Requirement | Unlocks |
|---|---|---|
| T1 | Craft the first Medicine | Medicine Cabinet + Treatment Station models |
| T2 | Stock a Treatment Station once | TREAT button + Sterilizer model |
| T3 | First treatment dose given | BandageKit / Tonic / Painkiller recipes |
| T4 | Sterilize a batch (use the Sterilizer once) | Herb Planter model |
| T5 | Heal to 100 | Nothing new; sets `ClinicComplete` |

Start state: only the Prep Bench model, the three tools, herb patches/crates, water barrel and supply cart; the
Medicine / Antiseptic / Sterile Bandages recipes. Every threshold shows one world-space reveal card (name plus
interface metaphor; T5 shows a "Clinic Complete" card) first, then releases the content so its existing pop-up tutorial follows. The NEXT GOAL sign
floats over the room and the goal also shows in the player HUD.

## Achievements
`MED_AchievementManager` (scene object `MED_Achievements`) puts a gold `MED_Trophy` (`Prefabs/MED_Trophy.prefab`, built
from primitives) on the trophy shelf against the east wall when a condition is first met, with a small label and a
floating "Achievement: <name>" text (also shown in the HUD message line). Each trophy appears once per run; nothing is
saved after Play stops. None of the five repeats a progression threshold.

| Achievement | Condition | Raised by |
|---|---|---|
| Apothecary | Craft all 5 Prep Bench recipes at least once | `MED_PrepBench.Crafted` |
| Supply Runner | Unload 3 supply carts completely | `MED_SupplyCart.CartEmptied` |
| Green Thumb | Harvest a ripe Herb Planter (the sandbox demo plant does not count) | `MED_HerbPlanter.Harvested` |
| Stocked Up | Fill the Medicine Cabinet to full | `MED_MedicineCabinet.Filled` |
| Full Recovery | Climb to 100 health twice (drop below 100 in between) | `MED_PlayerResources.OnResourceChanged` |

API: `MED_AchievementManager.Instance`, `IsEarned(MED_Achievement)`, `EarnedCount`, static event `Earned`. Other areas
need not use it. Test buttons: `RESET ACHIEVEMENTS` and `GRANT NEXT ACHIEVEMENT`.

## Wild items (procedural generation)
`MED_WildSpawner` stamps hand-designed patches instead of scattering single items. On Play (new seed) and on
RE-ROLL WILD ITEMS it picks 2-3 of the 5 `MED_WildPatch` assets (`Data/Patches/`), rotates each by 0/90/180/270 and
stamps it on a random spot of the 15x15 grid, only where all its cells are free and clear of the other patches. The
spawned herb patches and crates are normal `MED_Harvestable` objects and free their cells when depleted. Nothing here is
read by other areas. Each stamp is logged: `[MED_WildSpawner] Stamped <patch> at (x,z) rot R`.

## Shared scene
`MED_AreaGate` (added at runtime by `MED_ProgressionManager`) treats the build grid + 3 m as the Clinic area
(+1 m hysteresis on the way out). Outside it, the Clinic ignores action buttons, recipe cards, test buttons,
shelf-model grabs and harvest-tool swings, holds its pop-up tutorials, hides the NEXT GOAL sign and scales the
player HUD to zero. Check `MED_AreaGate.PlayerInside` (static bool) or subscribe to `MED_AreaGate.Changed`.

## What it consumes
Nothing from other stations yet (MP2A standalone rule). Everything is gathered in this room:
- Herbs: wild herb patches (Sickle) - Water: water barrel (Bucket)
- Alcohol / Gauze: supply crates (Crowbar) and the supply cart deliveries

## Test buttons (MED_TestTools, removable at integration)
`RESET PROGRESSION` (back to the start state, clears placed buildings, resets health and the reveal-once memory)
and `SKIP TO NEXT THRESHOLD` (clears the next of the five thresholds; its reveal card and unlock follow as normal).
`RESET ACHIEVEMENTS` / `GRANT NEXT ACHIEVEMENT` are described under Achievements. RESET PROGRESSION does not touch achievements.

If the player is already at 100 HP when T5 begins, T5 clears at once (nothing left to heal), so nobody gets stuck at 100/100.

## Folder layout
`Scripts/`, `Prefabs/` (+ `Buildings/`, `Harvestables/`, `Tools/`, `Delivery/`), `Data/` (building definitions,
`Recipes/`), `Materials/`, `Textures/`, `Shaders/`, `Audio/`, `Scenes/`, `Docs/`.

Shared dependencies (not in this folder): XR Interaction Toolkit Starter Assets (`Assets/Samples`), TextMesh Pro,
XR Device Simulator.
