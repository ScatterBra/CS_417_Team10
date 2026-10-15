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

## (c) Prefab list
| Prefab | Path | Notes |
|---|---|---|
| MED_ClinicStation | Assets/Suhaas/Prefabs/MED_ClinicStation.prefab | 15x15 m room. Children: MED_Floor, MED_Walls (North with 2 m doorway + MED_DoorMarker, South, East, West), MED_PlayerSpawn (z=-5.5, facing +Z/north), MED_Grid, MED_ModelShelf (5 toy models + placer), MED_Buildings (placed buildings), MED_TestTools (removable at integration). |
| MED_PrepBench | Assets/Suhaas/Prefabs/Buildings/MED_PrepBench.prefab | 2x1 wooden table. Mini: MED_Mini_PrepBench. |
| MED_Sterilizer | Assets/Suhaas/Prefabs/Buildings/MED_Sterilizer.prefab | 1x1 silver cylinder with lid. Mini: MED_Mini_Sterilizer. |
| MED_MedicineCabinet | Assets/Suhaas/Prefabs/Buildings/MED_MedicineCabinet.prefab | 1x1 tall white cabinet, red cross, door line. Mini: MED_Mini_MedicineCabinet. |
| MED_TreatmentStation | Assets/Suhaas/Prefabs/Buildings/MED_TreatmentStation.prefab | 2x1 bed with pale-blue cushion, pillow and green cross. Mini: MED_Mini_TreatmentStation. |
| MED_HerbPlanter | Assets/Suhaas/Prefabs/Buildings/MED_HerbPlanter.prefab | 1x1 low soil box with green herbs. Mini: MED_Mini_HerbPlanter. |
| MED_StatBar | Assets/Suhaas/Prefabs/MED_StatBar.prefab | Reusable world-space stat bar. Not yet used by other prefabs. |
| MED_PlayerHUD | Assets/Suhaas/Prefabs/MED_PlayerHUD.prefab | Floating health HUD + look-down resource panel. Scene object, not part of the station. |

## (d) Stat list
Framework exists (`MED_Stat`, `MED_StatBar`). No stats claimed yet.

## (e) Resource list
| Resource | Status |
|---|---|
| Health | Implemented (max 100, starts at 50, HUD bar, test buttons) |
| Herbs | Framework ready, gameplay in later steps |
| Water | Framework ready, gameplay in later steps |
| Alcohol | Framework ready, gameplay in later steps |
| Gauze | Framework ready, gameplay in later steps |
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

## (g) Pop-up tutorial list
_(none yet)_

## (h) Rubric story tracker
| Story | Target pts | Status |
|---|---|---|
| Statted Instances (prerequisite) | 0 | Not started |
| Player Resource (prerequisite) | 0 | Not started - framework ready |
| Gridded Building (prerequisite) | 0 | Done (Step 3) |
| Prefab Content (8 prefabs) | 3 | Not started |
| Stat Content (8 stats) | 3 | Not started |
| Passive Stat Gain | 2 | Not started |
| Resource Content (8+ resources) | 3 | Not started |
| Wild Instances | 2 | Not started |
| Interface Metaphor (0-4, target 3) | 3 | Not started |
| Building Content (5) | 4 | Done (5 buildings, Step 3) |
| Pop-up Tutorials (8) | 3 | Not started |
| Git Merged | 2 | Not started |
| Pull Request Reviewed | 2 | Not started |
| **Sidequests** | | |
| Harvesting (3 affordances) | 3 | Not started |
| Caretaking (3) | 3 | Not started |
| Crafting (5 recipes) | 3 | Not started |
| Death | 2 | Not started |
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
