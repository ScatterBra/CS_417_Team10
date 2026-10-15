# Clinic Station (Teammate 4) - Running Summary

## (a) Project overview
Zombie survival VR game (CS 417, Team 10). This log covers the **Clinic station** owned by Suhaas.
- Unity 6000.5.6f1, URP 17.5.0, Input System 1.20.0 (Active Input Handling = Input System Package (New)).
- XR Interaction Toolkit 3.6.1, XR Plugin Management 4.7.0. No XR loader enabled for PC.
- All Clinic assets live under `Assets/Suhaas/` and are prefixed `MED_`.
- Folder structure: `Assets/Suhaas/{Scenes, Prefabs, Scripts, Materials, Audio, Textures, Docs}`.
- Test scene: `Assets/Suhaas/Scenes/MED_Clinic.unity` (XR rig + XR Device Simulator, no headset needed).
- **Design decision:** the XR rig stays in the scene; only the station content lives in the `MED_ClinicStation` prefab. Player-side data (`MED_PlayerResources`) and the HUD are also scene objects, not part of the station prefab.

## (b) Step log

### Step 1 - 2026-10-14 - Foundation
Created:
- Packages: XR Interaction Toolkit 3.6.1, XR Plugin Management 4.7.0 (plus auto-installed dependencies: XR Core Utils, Legacy Input Helpers).
- XRI samples imported: Starter Assets, XR Device Simulator -> `Assets/Samples/XR Interaction Toolkit/3.6.1/`.
- Folders under `Assets/Suhaas/`.
- Materials: `MED_Wall.mat`, `MED_Floor.mat` (URP/Lit).
- Prefab `MED_ClinicStation.prefab` (15 x 15 m room).
- Scene `MED_Clinic.unity`: directional light, MED_ClinicStation at origin, XR Origin (XR Rig) at MED_PlayerSpawn, XR Device Simulator, one XR Interaction Manager, one EventSystem.

Files affected: `Packages/manifest.json`, `Packages/packages-lock.json`, `Assets/Suhaas/Scenes/MED_Clinic.unity`, `Assets/Suhaas/Prefabs/MED_ClinicStation.prefab`, `Assets/Suhaas/Materials/MED_Wall.mat`, `MED_Floor.mat`, `Assets/Suhaas/Docs/summary.md`; package-generated `Assets/Samples/**`, `Assets/XRI/**`.

### Step 2 - 2026-10-14 - Resource/Stat framework, floating HUD, health test tools
Created:
- Resource framework: `MED_ResourceType` (10 values), `MED_PlayerResources` (added to the XR Origin in the scene; Health max 100, starts at 50; Get/Add/TrySpend/SetHealth/ResetHealth; `OnResourceChanged` event; 3 `[ContextMenu]` debug methods).
- Stat framework: `MED_Stat` (serializable, clamped, `OnChanged`), `MED_StatBar` (world-space quad bar, billboard, green->yellow->red) + prefab `MED_StatBar.prefab` (not placed in the scene).
- Floating HUD: `MED_HudFollow` (smooth follow ~1.2 m ahead, 0.5 m below eyes, yaw-only; look-down detection with hysteresis at 25/20 degrees), `MED_PlayerHUD` (health bar + "Health N/100" label always visible; 2-column resource panel fades in when looking down) + prefab `MED_PlayerHUD.prefab`, instance placed in `MED_Clinic.unity`. TextMeshPro 3D text and unlit transparent materials, so room lighting does not affect it.
- Test tools inside `MED_ClinicStation.prefab` under `MED_TestTools`: `MED_ResetHealthButton` (resets Health to 50) and `MED_DamageTestButton` (-10 Health), built from primitives on pedestals ~2 m ahead of the spawn (z=-3.5), using `XRSimpleInteractable`, with hover/press color and push feedback. Scripts: `MED_TestButtonBase`, `MED_ResetHealthButton`, `MED_DamageTestButton`. They find `MED_PlayerResources` at runtime (`FindAnyObjectByType`).
- Materials: `MED_HudBarBG`, `MED_HudBarFill`, `MED_HudPanel` (URP/Unlit transparent), `MED_ButtonReset`, `MED_ButtonDamage`, `MED_Pedestal` (URP/Lit).

Files affected:
- Scripts: `Assets/Suhaas/Scripts/MED_ResourceType.cs`, `MED_PlayerResources.cs`, `MED_Stat.cs`, `MED_StatBar.cs`, `MED_HudFollow.cs`, `MED_PlayerHUD.cs`, `MED_TestButtonBase.cs`, `MED_ResetHealthButton.cs`, `MED_DamageTestButton.cs`
- Prefabs: `MED_StatBar.prefab` (new), `MED_PlayerHUD.prefab` (new), `MED_ClinicStation.prefab` (added MED_TestTools)
- Materials: the six listed above
- Scene: `MED_Clinic.unity` (MED_PlayerResources on XR Origin, MED_PlayerHUD instance)
- Docs: `summary.md`

### Step 3 - 2026-10-14 - Gridded Building affordance (toy models on a shelf)
Created:
- Grid: `MED_GridManager` on `MED_Grid` inside `MED_ClinicStation` (15x15 cells, 1 m, cell centers at half-meters from the room center; cell size/origin follow the prefab transform). API: `WorldToCell`, `CellToWorld`, `IsInBounds`, `IsCellFree`, `TryOccupy`, `Free`, `GetFootprintCells` (rotation in 90-degree steps). Reserved cells: 1-cell wall margin, 2-wide x 3-deep corridor at the doorway, 2x2 at the spawn, cells under `MED_ModelShelf` and `MED_TestTools` (computed from their colliders; extra cells can be added in the Inspector; shown with Gizmos). Grid visual = two unlit transparent quads (`MED_GridLines` using the procedural `MED_GridLine.png`, and `MED_GridBlocked` for red-tinted reserved cells); both fade in only while a model is held.
- Data: `MED_BuildingDefinition` ScriptableObject (name, full/mini prefab, footprint, resource costs) + five assets in `Assets/Suhaas/Data/` (`MED_Def_PrepBench` 2x1, `MED_Def_Sterilizer` 1x1, `MED_Def_MedicineCabinet` 1x1, `MED_Def_TreatmentStation` 2x1, `MED_Def_HerbPlanter` 1x1). Costs are empty; placement already checks and spends them via `MED_PlayerResources.TrySpend` and shows a HUD message on failure.
- Buildings (`Assets/Suhaas/Prefabs/Buildings/`): `MED_PrepBench`, `MED_Sterilizer`, `MED_MedicineCabinet`, `MED_TreatmentStation`, `MED_HerbPlanter` (primitives, billboarded name label, one BoxCollider, `MED_Building` script) and matching miniatures `MED_Mini_<Name>` (about 0.15-0.3 m; kinematic Rigidbody, `XRGrabInteractable` with instantaneous tracking and no throw, `MED_BuildingMini`).
- Shelf: `MED_ModelShelf` (west wall near the spawn row, 5 slots at 1.075 m, sign "CLINIC MODELS - grab one, point at the floor, release to build"). `MED_ModelShelf` script respawns a model in a slot when one is consumed; `MED_BuildingMini` animates back to its slot when released at an invalid spot.
- Placement: `MED_BuildingPlacer` (on `MED_ModelShelf`): while a model is held it fades in the grid, casts the grabbing interactor's ray to the floor (10 m), snaps to a cell, uses the head yaw snapped to 90 degrees, and shows a green/red translucent ghost. On release over a valid spot it spends costs, occupies cells, builds the full prefab under `MED_Buildings` with a 0.3 s pop, destroys the mini and restocks the slot. `MED_Building.OnBuilt` / `OnRemoved` static events, `MED_Building.CountOf(def)` and `TotalCount` static counters.
- Test tool: `MED_ClearBuildingsButton` ("CLEAR ALL BUILDINGS", at x=-1.2, z=-3.5 in `MED_TestTools`).
- HUD: `MED_PlayerHUD.ShowMessage(text)` plus a message label (`MED_MessageLabel`) under the health bar in `MED_PlayerHUD.prefab`.
- Helpers: `MED_Billboard` (labels face the camera).

Files affected:
- Scripts: `MED_BuildingDefinition.cs`, `MED_Billboard.cs`, `MED_GridManager.cs`, `MED_Building.cs`, `MED_BuildingMini.cs`, `MED_ModelShelf.cs`, `MED_BuildingPlacer.cs`, `MED_ClearBuildingsButton.cs`; changed `MED_PlayerHUD.cs`
- Data: `Assets/Suhaas/Data/MED_Def_*.asset` (5)
- Prefabs: 5 buildings + 5 minis in `Assets/Suhaas/Prefabs/Buildings/`; changed `MED_ClinicStation.prefab` (MED_Grid, MED_ModelShelf, MED_Buildings, clear button), `MED_PlayerHUD.prefab`
- Materials: `MED_GridOverlay`, `MED_GridBlocked`, `MED_GhostValid`, `MED_GhostInvalid`, `MED_Wood`, `MED_Metal`, `MED_White`, `MED_RedCross`, `MED_PaleBlue`, `MED_GreenCross`, `MED_Soil`, `MED_Leaf`, `MED_DarkTrim`, `MED_BedFrame`, `MED_ButtonClear`
- Texture: `Assets/Suhaas/Textures/MED_GridLine.png`
- Docs: `summary.md`

### Step 4 - 2026-10-15 - Harvesting loop (wild herb patches, supply crates, water barrel)
Created:
- Base: `MED_Harvestable` (holds a `MED_Stat` "remaining", resource, yield per hit, required tool, 0.4 s cooldown, destroyOnEmpty). `TryHarvest(tool, player)`: wrong tool shows a floating "Use the <tool>"; right tool reduces the stat by 1, adds the resource, shows a rising "+1 Herbs" text, squashes the visual, spawns a small particle puff and sends a haptic pulse. On depletion (if destroyOnEmpty): shrink-and-puff, frees its grid cells, raises `OnDepleted`, destroys itself. A `MED_StatBar` (Step 2 prefab) floats above every harvestable. Supporting scripts: `MED_ToolType`, `MED_FloatingText`, `MED_HerbPatchVisual`, `MED_CrateVisual`, `MED_BarrelVisual`.
- Tools (supporting props, not content prefabs): `MED_HarvestTool` + `MED_Sickle`, `MED_Crowbar`, `MED_Bucket` prefabs in `Assets/Suhaas/Prefabs/Tools/` (kinematic Rigidbody, box collider, `XRGrabInteractable`, name label). Harvest triggers when the tool's hit point overlaps a harvestable AND (swing speed > 0.5 m/s OR the activate/trigger button is held). `MED_ToolRack` (east wall, sign "TOOLS") holds the three tools; a tool left alone for 3 s eases back to its slot.
- Harvestables (`Assets/Suhaas/Prefabs/Harvestables/`): `MED_HerbPatch`, `MED_SupplyCrate`, `MED_WaterBarrel`, plus the shared `MED_HarvestPuff` particle prefab.
- Wild instances: `MED_WildSpawner` on `MED_Wild` in the station. On Play (new random seed every run) it places 5-8 herb patches and 3-4 supply crates on random free grid cells (never on reserved or occupied cells) under `MED_WildInstances`, with random yaw. `Respawn()` destroys them and rolls a new set. The fixed water barrel (`MED_WaterBarrel`, east wall, x=7, z=-1) is not part of the wild set; its cells and the rack's cells are reserved.
- Test tools in `MED_TestTools`: `MED_RerollWildButton` ("RE-ROLL WILD ITEMS" calls `Respawn()`) and `MED_RefillBarrelButton` ("REFILL BARREL"). "CLEAR ALL BUILDINGS" only removes buildings (fixed: it no longer frees every grid cell, so wild cells stay occupied).

Files affected:
- Scripts: `MED_ToolType.cs`, `MED_FloatingText.cs`, `MED_Harvestable.cs`, `MED_HarvestTool.cs`, `MED_ToolRack.cs`, `MED_WildSpawner.cs`, `MED_HerbPatchVisual.cs`, `MED_CrateVisual.cs`, `MED_BarrelVisual.cs`, `MED_RerollWildButton.cs`, `MED_RefillBarrelButton.cs`; changed `MED_BuildingPlacer.cs`
- Prefabs: 3 tools, 3 harvestables, `MED_HarvestPuff`; changed `MED_ClinicStation.prefab` (MED_ToolRack, MED_WaterBarrel, MED_WildInstances, MED_Wild, two test buttons, reserved areas)
- Materials: `MED_GreyMetal`, `MED_Water`, `MED_BandAlcohol`, `MED_BandGauze`, `MED_PuffMat`, `MED_ButtonReroll`, `MED_ButtonRefill`
- Docs: `summary.md`

### Step 5 - 2026-10-15 - Crafting at the Prep Bench (5 recipes, timed preparation)
Created:
- Data: `MED_Recipe` ScriptableObject (name, inputs, outputs, duration, `Description()` like "2 Herbs + 1 Water -> Medicine") and 5 assets in `Assets/Suhaas/Data/Recipes/` (`MED_Recipe_Medicine`, `_Antiseptic`, `_BandageKit`, `_Painkiller`, `_Tonic`).
- `MED_PrepBench` (added to the existing `MED_PrepBench.prefab`, `MED_Building` and the look are kept): stat `PrepProgress` (0..1), states Idle / Preparing. `TryStartRecipe(recipe)` fails with a floating message if the bench is busy ("Bench is busy"), the player is farther than 3.5 m ("Too far"), or inputs are missing ("Need 2 Herbs (have 1)"). On success all inputs are spent at once (every input is checked first, so partial spending cannot happen). While Preparing, `PrepProgress` advances in `Update` by Euler integration (`+= (1 / duration) * dt`); at 1 the outputs are added, a floating "+1 Medicine" rises, and the bench returns to Idle. Each bench instance works independently.
- Visuals: a `MED_StatBar` over the bench (faint when Idle, bright while Preparing) and a mortar-and-pestle prop that spins and bobs while Preparing and rests when Idle.
- Recipe board (`MED_RecipeBoard`, on the back edge of the bench, tilted back 20 degrees so its face tips up toward a standing player): five `MED_RecipeCard` buttons (primitive card + TextMeshPro name and description lines, `XRSimpleInteractable`). Cards are green when the bench is Idle and the player can afford the recipe, grey otherwise, updated from `MED_PlayerResources.OnResourceChanged` and bench state events (no per-frame polling). Hover highlight and a press-in animation on select.
- Test tool: `MED_AddSuppliesButton` ("TEST: +4 RAW SUPPLIES", +4 each of Herbs, Water, Alcohol, Gauze) in `MED_TestTools`.
- Building ghost: the placement ghost now switches off every script and stat bar on the ghost, so a ghost bench does not run its recipe logic.

Files affected:
- Scripts: `MED_Recipe.cs`, `MED_PrepBench.cs`, `MED_RecipeCard.cs`, `MED_AddSuppliesButton.cs`; changed `MED_BuildingPlacer.cs`
- Data: `Assets/Suhaas/Data/Recipes/MED_Recipe_*.asset` (5)
- Prefabs: changed `Buildings/MED_PrepBench.prefab` (bench script, prop, bar, board, cards) and `MED_ClinicStation.prefab` (supplies button)
- Materials: `MED_BoardPanel`, `MED_RecipeCard`, `MED_ButtonSupplies`
- Docs: `summary.md`

### Step 6 - 2026-10-15 - Medicine Cabinet and Treatment Station (Caretaking)
Created:
- `MED_ActionButton`: reusable button (hover highlight, press-in animation, green/grey availability set by its owner from events). `MED_PlayerProximity`: head-distance helper used for the 3.5 m rule.
- `MED_MedicineCabinet` (added to the existing `MED_MedicineCabinet.prefab`; `MED_Building` and the look are kept): stat `CabinetStock` (0..10, starts 0). Buttons on the front: "STOCK +1" (spends 1 Medicine, +1 stock; fails with "No Medicine" / "Cabinet full") and "TAKE 1" (-1 stock, +1 Medicine; fails with "Cabinet empty"). Both need the player within 3.5 m ("Too far"). Visuals: 5 amber bottles behind a glass pane (visible bottles = ceil(stock / 2)) and a stat bar. Button colors update from `OnResourceChanged` and the stat's `OnChanged`.
- `MED_TreatmentStation` (added to `MED_TreatmentStation.prefab`, footprint now **2x2**): back row is the bed, front row is a green treatment pad ("STAND HERE"). Stats `StoredMedicine` (0..10, starts 0) and `TreatProgress` (0..1). Console beside the bed with "REFILL +1" (spends 1 Medicine into StoredMedicine; "No Medicine" / "Station is full") and a big "TREAT". TREAT works only while the camera is inside the pad rectangle (rectangle math, no physics triggers), otherwise "Step onto the green pad"; it also refuses when health is full ("Health is full") or the station is empty ("Station is empty - refill it"). State machine Idle/Treating: while Treating, `TreatProgress` advances by Euler integration (`+= (1 / doseDuration) * dt`, dose 1.5 s). At 1, health is re-checked (full: spend nothing, "Fully healed"), then 1 StoredMedicine is spent, Health +20 (capped at 100 by `MED_PlayerResources`) and a floating "+N HP" shows. The next dose starts automatically while health is not full, the station has medicine and the player is still on the pad; otherwise it stops with "Fully healed" / "Station is empty - refill it" (no message if the player stepped away). Stepping off the pad cancels the dose, resets progress and spends nothing ("Treatment cancelled").
- Station visuals: IV bag on a pole whose liquid fill follows StoredMedicine/10, a stat bar for StoredMedicine, a TreatProgress bar shown only while treating, a pulsing glow on the bed while treating, and the pad brightening while the player stands on it. Colliders cover only the bed and console so the player can walk onto the pad.
- Miniature of the Treatment Station rebuilt to match (bed + pad + console). The Step 3 placer already handles a 2x2 footprint (anchor cell = min corner; the extra cells are on the + side; a square footprint is the same under 90-degree turns, and the pivot is the footprint center).
- Test tool: `MED_AddMedicineButton` ("TEST: +5 MEDICINE") in `MED_TestTools`.
- Fix: refill/stock checks report 'full' before 'no Medicine'.

Files affected:
- Scripts: `MED_ActionButton.cs`, `MED_PlayerProximity.cs`, `MED_MedicineCabinet.cs`, `MED_TreatmentStation.cs`, `MED_AddMedicineButton.cs`
- Prefabs: changed `Buildings/MED_MedicineCabinet.prefab`, `Buildings/MED_TreatmentStation.prefab`, `Buildings/MED_Mini_TreatmentStation.prefab`, `MED_ClinicStation.prefab` (test button)
- Data: changed `Assets/Suhaas/Data/MED_Def_TreatmentStation.asset` (footprint 2x2)
- Materials: `MED_PadGreen`, `MED_BedGlow`, `MED_ConsoleBody`, `MED_ButtonAction`, `MED_IVBag`, `MED_IVLiquid`, `MED_BottleAmber`, `MED_Glass`, `MED_ButtonMedicine`
- Docs: `summary.md`

### Step 7 - 2026-10-15 - Herb Planter, Sterilizer and Water Barrel (Caretaking, passive dynamics)
Created:
- `MED_HerbPlanter` (added to `MED_HerbPlanter.prefab`; `MED_Building` and the look are kept): stats `Growth` (0..1) and `Moisture` (0..1), both start 0; states Empty / Growing / Ripe. PLANT (only when Empty, spends 1 Herbs; "Already planted" / "Need 1 Herbs"), WATER (spends 1 Water, Moisture +0.5; "Soil is wet" at >= 0.95 is checked first, then "No Water"; works with or without a plant). `Update` uses Euler integration: Moisture -= 0.04 * dt (never below 0); while Growing and Moisture > 0, Growth += (1 / 30) * dt, and at 1 the state becomes Ripe. With Moisture at 0 growth pauses and the plant droops (squashed, tipped, leaves turn yellow-brown). A Sickle on a Ripe plant gives 3 Herbs ("+3 Herbs"), resets to Empty with Growth 0 and does not destroy the planter ("Not ripe yet" / "Plant something first" / "Use the Sickle" otherwise). Visuals: soil darkens with Moisture, plant height and width follow Growth, a pulsing golden glow when Ripe, blue Moisture and green Growth `MED_StatBar`s.
- `MED_IToolTarget` interface (`TryHarvest(tool, player)`): `MED_Harvestable` implements it and `MED_HarvestTool` now hits any `MED_IToolTarget`, so the planter reuses the existing tool-hit logic. `MED_StatBar` gained `SetFixedColor` (flat blue/green/red bars); the default gradient is unchanged.
- `MED_Sterilizer` (added to `MED_Sterilizer.prefab`): stats `Heat` (0..1) and `SterilizeProgress` (0..1); states Idle / Heating / Sterilizing; recipe asset `Data/Recipes/MED_Recipe_SterileBandages` (2 Gauze + 1 Water -> 1 BandageKit). START needs the player within 3.5 m, Idle ("Sterilizer is busy"), Heat < 0.2 ("Still hot - wait") and the inputs ("Need 1 Gauze + 1 Water" style, listing only what is missing); inputs are spent at once on start. Euler integration: Heating Heat += (1/3) * dt until 1, then Sterilizing SterilizeProgress += (1/3) * dt until 1, then +1 BandageKit ("+1 BandageKit"), progress reset, Idle. While Idle, Heat -= (1/8) * dt even if nobody is near. Visuals: body emission orange to red with Heat (`MED_SterilizerBody` material), lid and knob lift, steam puffs rise while active, red Heat bar, blue progress bar (only while active), "HOT" label when Heat > 0.5.
- `MED_BarrelController` (added to `MED_WaterBarrel.prefab`; the bucket harvest, destroyOnEmpty = false and the REFILL BARREL test button are untouched): passive rain with Euler integration WaterLevel += (1/6) * dt while below max, and a "POUR 1 WATER" button (spends 1 Water, WaterLevel +1; "Barrel is full" checked first, then "No Water").
- All buttons use `MED_ActionButton` (green/grey from events, no per-frame polling), the 3.5 m "Too far" rule and `MED_FloatingText`; capacity/state problems are reported before resource problems.
- Test tool: `MED_RipenPlantersButton` ("TEST: RIPEN PLANTERS", yellow, at x=3.0, z=-2.3 in `MED_TestTools`, its own free grid cell) sets Growth = 1 and state Ripe on every planter.
- HUD: `MED_PlayerHUD` lists every `MED_ResourceType`, so Herbs, Water, Gauze and BandageKit update when these machines spend or give them.

Files affected:
- Scripts: `MED_IToolTarget.cs`, `MED_HerbPlanter.cs`, `MED_Sterilizer.cs`, `MED_BarrelController.cs`, `MED_RipenPlantersButton.cs`; changed `MED_Harvestable.cs`, `MED_HarvestTool.cs`, `MED_StatBar.cs`
- Prefabs: changed `Buildings/MED_HerbPlanter.prefab`, `Buildings/MED_Sterilizer.prefab`, `Harvestables/MED_WaterBarrel.prefab`, `MED_ClinicStation.prefab` (ripen button + pedestal)
- Data: `Assets/Suhaas/Data/Recipes/MED_Recipe_SterileBandages.asset`
- Materials: `MED_RipeGlow`, `MED_SterilizerBody`, `MED_ButtonRipen`
- Docs: `summary.md`

## (c) Prefab list
| Prefab | Path | Notes |
|---|---|---|
| MED_ClinicStation | Assets/Suhaas/Prefabs/MED_ClinicStation.prefab | 15x15 m room. Children: MED_Floor, MED_Walls (North with 2 m doorway + MED_DoorMarker, South, East, West), MED_PlayerSpawn (z=-5.5, facing +Z/north), MED_Grid, MED_ModelShelf (5 toy models + placer), MED_Buildings (placed buildings), MED_TestTools (removable at integration). |
| MED_PrepBench | Assets/Suhaas/Prefabs/Buildings/MED_PrepBench.prefab | 2x1 wooden table. Mini: MED_Mini_PrepBench. Affordances/dynamics so far: build from a shelf model; prepare 5 recipes from a recipe-card board; passive PrepProgress over each recipe's duration; busy/idle state (working prop + progress bar). |
| MED_Sterilizer | Assets/Suhaas/Prefabs/Buildings/MED_Sterilizer.prefab | 1x1 silver cylinder with lid. Mini: MED_Mini_Sterilizer. Affordances/dynamics: build from a shelf model; START (spend 2 Gauze + 1 Water, run a sterilize cycle for 1 BandageKit); passive heating then sterilizing (Euler); passive cooling while Idle (blocks restarting until Heat < 0.2); glow, steam and HOT label signifiers. |
| MED_MedicineCabinet | Assets/Suhaas/Prefabs/Buildings/MED_MedicineCabinet.prefab | 1x1 tall white cabinet with glass-front bottles. Mini: MED_Mini_MedicineCabinet. Affordances/dynamics so far: build from a shelf model; STOCK +1 (spend Medicine, raise CabinetStock); TAKE 1 (get Medicine back); stat bar and bottle count follow CabinetStock. |
| MED_TreatmentStation | Assets/Suhaas/Prefabs/Buildings/MED_TreatmentStation.prefab | **2x2 footprint**: bed (back row) with IV bag, console, and a green treatment pad (front row). Mini: MED_Mini_TreatmentStation. Affordances/dynamics so far: build from a shelf model; REFILL +1 (spend Medicine, raise StoredMedicine); TREAT while standing on the pad (20 HP per 1.5 s dose, passive TreatProgress, auto-repeat until healed, empty, or the player steps off). |
| MED_HerbPlanter | Assets/Suhaas/Prefabs/Buildings/MED_HerbPlanter.prefab | 1x1 low soil box with green herbs. Mini: MED_Mini_HerbPlanter. Affordances/dynamics: build from a shelf model; PLANT (spend 1 Herbs); WATER (spend 1 Water, raise Moisture); Sickle harvest of a ripe plant (3 Herbs, planter stays); passive soil drying; passive growth only while the soil is damp; drooping when dry; glow when ripe. |
| MED_StatBar | Assets/Suhaas/Prefabs/MED_StatBar.prefab | Reusable world-space stat bar. Used by HerbPatch, SupplyCrate and WaterBarrel. |
| MED_HerbPatch | Assets/Suhaas/Prefabs/Harvestables/MED_HerbPatch.prefab | Harvest with Sickle (1 Herbs per hit); random wild spawn; one plant disappears per hit; Death on depletion (shrink + puff, cells freed). Stat: HerbsRemaining. |
| MED_SupplyCrate | Assets/Suhaas/Prefabs/Harvestables/MED_SupplyCrate.prefab | Harvest with Crowbar (1 Alcohol or Gauze per hit, chosen randomly at spawn and shown by lid band + label); random wild spawn; lid opens as supplies drop; Death on depletion. Stat: SuppliesRemaining. |
| MED_WaterBarrel | Assets/Suhaas/Prefabs/Harvestables/MED_WaterBarrel.prefab | Harvest with Bucket (1 Water per hit); water surface follows the stat; shows "EMPTY" at 0 and stays. Fixed instance near the east wall. Stat: WaterLevel. Affordances/dynamics: harvest with Bucket; passive rain collection (+1 per ~6 s while not full); POUR 1 WATER by hand (spends 1 Water); test REFILL BARREL button. |
| MED_Sickle / MED_Crowbar / MED_Bucket | Assets/Suhaas/Prefabs/Tools/ | Supporting props: grabbable harvest tools on the east-wall rack. Not claimed as content prefabs. |
| MED_PlayerHUD | Assets/Suhaas/Prefabs/MED_PlayerHUD.prefab | Floating health HUD + look-down resource panel. Scene object, not part of the station. |

## (d) Stat list
Framework: `MED_Stat` (clamped value, `OnChanged`) and `MED_StatBar` (world-space bar, green to yellow to red).

| Stat | Owning prefab | Max | What changes it | How it is shown |
|---|---|---|---|---|
| HerbsRemaining | MED_HerbPatch | 4 | -1 per Sickle hit | Floating stat bar; one plant disappears per remaining unit |
| SuppliesRemaining | MED_SupplyCrate | 5 | -1 per Crowbar hit | Floating stat bar; the lid opens wider as it drops |
| WaterLevel | MED_WaterBarrel | 10 | -1 per Bucket hit; passive rain +1/6 per second (Euler) while below max; +1 with POUR 1 WATER (spends Water); "REFILL BARREL" test button | Floating stat bar; water surface height; "EMPTY" label at 0 |
| PrepProgress | MED_PrepBench | 0-1 | Time: advances by Euler integration (`+= (1 / duration) * dt`) while Preparing; resets to 0 when done | Floating stat bar over the bench (bright while Preparing); the pestle spins and bobs |
| CabinetStock | MED_MedicineCabinet | 10 | +1 with STOCK +1 (spends Medicine), -1 with TAKE 1 | Stat bar over the cabinet; visible bottles = ceil(stock / 2) |
| StoredMedicine | MED_TreatmentStation | 10 | +1 with REFILL +1 (spends Medicine), -1 per completed dose | Stat bar over the console; IV bag liquid level |
| TreatProgress | MED_TreatmentStation | 0-1 | Time: Euler integration `+= (1 / 1.5) * dt` while Treating; reset when a dose completes or is cancelled | Small bar over the bed, shown only while treating; pulsing bed glow |
| Heat | MED_Sterilizer | 0-1 | Euler: `+= (1/3) * dt` while Heating (reaches 1); `-= (1/8) * dt` while Idle (cooling runs even when unused) | Red stat bar; body emission orange to red; "HOT" label above 0.5; lid lifts and steam rises while active |
| SterilizeProgress | MED_Sterilizer | 0-1 | Euler: `+= (1/3) * dt` while Sterilizing; reset to 0 when the BandageKit is delivered | Blue stat bar shown only while active |
| Growth | MED_HerbPlanter | 0-1 | PLANT sets 0; Euler `+= (1/30) * dt` while Growing and Moisture > 0; RIPEN test button sets 1; harvest resets to 0 | Green stat bar; plant scale/height; golden glow at 1 |
| Moisture | MED_HerbPlanter | 0-1 | WATER +0.5 (spends Water); Euler `-= 0.04 * dt` (drying) | Blue stat bar; soil color darkens when wet; plant droops at 0 while Growing |

## (e) Resource list
| Resource | Status |
|---|---|
| Health | Implemented (max 100, starts at 50, HUD bar, test buttons) |
| Herbs | Obtainable (Step 4): MED_HerbPatch + Sickle |
| Water | Obtainable (Step 4): MED_WaterBarrel + Bucket |
| Alcohol | Obtainable (Step 4): MED_SupplyCrate + Crowbar (blue band) |
| Gauze | Obtainable (Step 4): MED_SupplyCrate + Crowbar (white band) |
| Medicine | Framework ready, gameplay in later steps |
| BandageKit | Framework ready, gameplay in later steps |
| Antiseptic | Framework ready, gameplay in later steps |
| Painkiller | Framework ready, gameplay in later steps |
| Tonic | Framework ready, gameplay in later steps |

## (f) Building/affordance list
Interface metaphor: **toy models on a shelf, set down on a floor grid** (dollhouse / model town). The player picks up a small model, sees the floor become a grid, and puts the model down to make the full-size building appear.

Signifiers shared by all five: a small toy model on a shelf slot (grabbable, with a name label), the shelf sign "CLINIC MODELS - grab one, point at the floor, release to build", the grid fading in while a model is held (reserved cells tinted red), and a green/red translucent ghost showing where the building will go. A fresh model reappears on the shelf after building; an invalid release flies the model back.

| Building | Interaction | Footprint | Notes |
|---|---|---|---|
| Prep Bench | Build from shelf model | 2x1 | Wooden table. |
| Sterilizer | Build from shelf model | 1x1 | Silver cylinder with lid. |
| Medicine Cabinet | Build from shelf model | 1x1 | White cabinet with red cross. |

| Treatment Station | Build from shelf model | 2x1 | Bed with green cross. |
| Herb Planter | Build from shelf model | 1x1 | Soil box with herbs. |

All costs are empty for now.

### Crafting (Step 5)
Metaphor: an **apothecary bench**. Raw herbs, water, alcohol and gauze go onto the bench, a card on the recipe board says what each preparation needs, and the pestle grinds while the bar fills.

| Recipe | Inputs | Output | Duration |
|---|---|---|---|
| Medicine | 2 Herbs + 1 Water | 1 Medicine | 4 s |
| Antiseptic | 1 Alcohol + 1 Water | 1 Antiseptic | 3 s |
| BandageKit | 2 Gauze + 1 Antiseptic | 1 BandageKit | 4 s |
| Painkiller | 2 Herbs + 1 Alcohol | 1 Painkiller | 4 s |
| Tonic | 1 Herbs + 2 Water | 1 Tonic | 3 s |

Signifiers: a recipe-card board on the bench (name plus "inputs -> output" on each card), card color (green = can start now, grey = missing inputs or bench busy), hover highlight and press-in on select, a progress bar over the bench, a spinning mortar-and-pestle while working, a floating "+1 <Product>" when done, and floating "Bench is busy" / "Too far" / "Need 2 Herbs (have 1)" messages when a recipe cannot start. The player must stand within 3.5 m of the bench.

Extra recipe (Step 7, at the Sterilizer instead of the Prep Bench): **Sterile Bandages = 2 Gauze + 1 Water -> 1 BandageKit** (heat 3 s + sterilize 3 s; asset `MED_Recipe_SterileBandages`). Signifiers: START button green when idle, cool and the inputs are held; steam, glow and a progress bar while it runs.

### Passive Stat Gain (Steps 5-7)
All use Euler integration (`value += rate * dt` in `Update`): PrepProgress, TreatProgress, Growth (only while the soil is damp), Moisture (drying), Heat (heating while Heating, cooling while Idle), SterilizeProgress, and barrel rain (WaterLevel).

### Caretaking (Steps 6-7)
Metaphor: a **hospital ward**: a bed with an IV bag, a cabinet of medicine bottles, and a green "stand here" pad for treatment. Caretaking means keeping these supplied with the Medicine you crafted.

| # | Affordance | Spends | Raises | Signifiers |
|---|---|---|---|---|
| 1 | Refill the Treatment Station (REFILL +1) | 1 Medicine | StoredMedicine | IV bag liquid level, stat bar, console button turns green when you hold Medicine and the station is not full |
| 2 | Restock the Medicine Cabinet (STOCK +1) | 1 Medicine | CabinetStock | Bottles appear behind the glass (one per 2 stock), stat bar, button green/grey |

| 3 | Water the Herb Planter (WATER) | 1 Water | Moisture (+0.5) | Soil color darkens when wet, blue Moisture bar, plant droops when dry, button green/grey ("Soil is wet" / "No Water") |
| 4 | Pour Water into the Water Barrel (POUR 1 WATER) | 1 Water | WaterLevel (+1) | Water surface rises, stat bar, button green/grey ("Barrel is full" / "No Water") |

Metaphor links (Step 7): the Herb Planter is the clinic's **garden planter**: soil that darkens when wet and a plant that wilts if neglected. The Sterilizer is the **autoclave**: it glows orange to red and steams while it runs, shows "HOT" afterwards and has to cool before the next load. The Water Barrel is the **rain barrel**: it fills slowly by itself and can be topped up by hand. Treatment itself (TREAT, standing on the green pad) uses the stocked Medicine to heal the player: the pad brightens when you stand on it, the bed glows while treating, and a progress bar fills for each dose. The TREAT button is green only when treatment can start right now.

### Harvesting (Step 4)
Metaphor: an **apothecary / field clinic**. Each job is matched to the real-world tool for it: a sickle cuts herbs, a crowbar pries open crates, a bucket is dipped into a barrel. Tools sit on a labeled rack ("TOOLS"); the player grabs one and uses it on the matching object. Signifiers for every affordance: a named object with a floating stat bar, a floating "+1 <Resource>" on success, a squash and puff on each hit, and a "Use the <tool>" message when the wrong tool is used.

| Affordance | Tool and target | Yields | Signifiers |
|---|---|---|---|
| Harvest herbs | Sickle on HerbPatch | Herbs | Green plants with a stat bar; plants vanish one by one; the patch pops away when empty |
| Open supply crate | Crowbar on SupplyCrate | Alcohol or Gauze | Red-cross crate; blue band = ALCOHOL, white band = GAUZE; lid opens wider as supplies drop |
| Draw water | Bucket on WaterBarrel | Water | Water surface drops with each dip; "EMPTY" label at 0 |
| Harvest ripe herbs (Step 7, 4th harvestable) | Sickle on a ripe Herb Planter | 3 Herbs | Golden glow and full-size plant when ripe; "Not ripe yet" / "Plant something first"; the planter is not destroyed and returns to Empty |

Harvesting works by swinging the tool through the target (speed over 0.5 m/s) or by holding the trigger/activate button while touching it, so it works in the XR Device Simulator and in a headset.

## (g) Pop-up tutorial list
_(none yet)_

## (h) Rubric story tracker
| Story | Target pts | Status |
|---|---|---|
| Statted Instances (prerequisite) | 0 | In progress (HerbPatch, SupplyCrate, WaterBarrel carry stats) |
| Player Resource (prerequisite) | 0 | Not started - framework ready |
| Gridded Building (prerequisite) | 0 | Done (Step 3) |
| Prefab Content (8 prefabs) | 3 | Done (8 with >= 3 affordances/dynamics: HerbPatch, SupplyCrate, PrepBench, MedicineCabinet, TreatmentStation, Sterilizer, HerbPlanter, WaterBarrel) |
| Stat Content (8 stats) | 3 | Done (11: HerbsRemaining, SuppliesRemaining, WaterLevel, PrepProgress, CabinetStock, StoredMedicine, TreatProgress, Heat, SterilizeProgress, Growth, Moisture; target 8 reached) |
| Passive Stat Gain | 2 | Done (PrepProgress, TreatProgress, Growth, Moisture, Heat, SterilizeProgress, barrel rain; Euler integration) |
| Resource Content (8+ resources) | 3 | Not started |
| Wild Instances | 2 | Done (Step 4) |
| Interface Metaphor (0-4, target 3) | 3 | Not started |
| Building Content (5) | 4 | Done (5 buildings, Step 3) |
| Pop-up Tutorials (8) | 3 | Not started |
| Git Merged | 2 | Not started |
| Pull Request Reviewed | 2 | Not started |
| **Sidequests** | | |
| Harvesting (3 affordances) | 3 | Done (Step 4) |
| Caretaking (3) | 3 | Done (4 affordances, 3 pts tier: refill Treatment Station, restock Medicine Cabinet, water Herb Planter, pour into Water Barrel) |
| Crafting (5 recipes) | 3 | Done (Step 5) |
| Death | 2 | Done (herb patch and crate: shrink, puff, cell release; Step 4) |
| Intermittent Instances | 2 | Not started |
| Path Following | 2 | Not started |
| Texture Animation Signifiers | 3 | Not started |

## (i) Notes and known issues
- XR rig and Device Simulator are in the scene only, not in the station prefab.
- North = +Z. Door gap is centered at x=0 on the north wall (z=+7.65); MED_DoorMarker sits at its center on the floor.
- XRI auto-generates `Assets/XRI/`; the sample import created `Assets/Samples/`; both are outside `Assets/Suhaas/`.
- `MED_PlayerResources.Health` is a stand-in: at integration, replace it with the Defense Yard's player health component (one health value in the combined game).
- `MED_TestTools` (reset and damage buttons) is test-only; delete that child of the station prefab at integration.
- Scripts use `FindAnyObjectByType` (equivalent to `FindFirstObjectByType` for a single instance; the latter is deprecated in Unity 6000.5).
- `Assets/XR/Settings/OpenXR Package Settings.asset` is regenerated by Unity and may show fileID-only churn in git. No XR loader is enabled.
- Building costs are empty for now; the cost check/spend path is already wired. A pop-up tutorial for the shelf will be added in the tutorial step.
- Buildings face the player: a building's front is local -Z and it takes the player's head yaw (snapped to 90 degrees) at placement.
- The shelf is on the west wall (about 7 m from the spawn), so the player has to walk to it.
- The HUD has no depth-test override: if the player pushes close to a wall, the HUD can clip into it.
- Harvesting needs a held tool near a target: hold the trigger while touching, or swing quickly. Tools are kinematic and hover where released; after 3 s they ease back to the rack.
- "RE-ROLL WILD ITEMS" and "REFILL BARREL" are in `MED_TestTools` (test-only; remove at integration).
- The `MED_PlayerHUD` resource panel (look down) shows Herbs, Water, Alcohol and Gauze updating as they are harvested.
- In the XR Device Simulator, sending haptics logs a harmless warning ("Failed to get haptic capabilities of XRSimulatedController").
- `com.unity.xr.openxr` is in the project manifest (not added by this log). No XR loader is enabled for PC.
- Crafting inputs are spent when a recipe starts. If the bench is destroyed mid-preparation (for example with "CLEAR ALL BUILDINGS"), the inputs are not refunded.
- `MED_TestTools` now also holds "TEST: +4 RAW SUPPLIES" (test-only; remove at integration).
- The bench uses `FindAnyObjectByType<MED_PlayerResources>()` at runtime (no scene references); `FindFirstObjectByType` is deprecated in this Unity version.
- Treatment Station footprint is 2x2 (bed row + pad row). Its colliders cover only the bed and console so the player can walk onto the pad.
- The "+N HP" text shows the HP actually gained, so the last dose before full health can read less than +20.
- Medicine put into the cabinet or station is lost if the building is destroyed (for example with "CLEAR ALL BUILDINGS"); there is no refund.
- `MED_TestTools` now also holds "TEST: +5 MEDICINE" (test-only; remove at integration).
- `MED_TestTools` now also holds "TEST: RIPEN PLANTERS" (test-only; remove at integration).
- Herb Planter and Sterilizer inputs are spent on action; there is no refund if the building is destroyed. Planter/Sterilizer/barrel state (growth, heat, water) is not saved.
- Barrel rain adds fractional WaterLevel, so a bucket dip at a level below 1 still yields 1 Water (the stat clamps at 0).
