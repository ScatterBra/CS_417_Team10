# Power Station (PowerYard) — Handoff Note

Owner prefix: `PWR_` · Namespace: `Deadzone.Power` · Scene: `Scenes/PWR.unity`

## What it produces
- **Charged Cell** (`Prefabs/PWR_ChargedCell`): a physical, grabbable item ejected by the Cell Charger when
  charging reaches 100%. This is the station's output, meant to be carried to the next room.

## Completion check (for other areas)
The station is complete once the burnt-out Floodlight bulb has been replaced **and** the player has
picked up the Charged Cell (either order).
- `Deadzone.Power.PWR_LevelDirector.StationComplete` — static bool, check it any time.
- `Deadzone.Power.PWR_LevelDirector.StationCompleted` — static event, fired once when it becomes true.
- Finer-grained: `PWR_CellCharger.Completed` (cell ejected), `PWR_CellCharger.CellCollected` (cell first
  picked up), `PWR_Floodlight.BulbInstalled` (replacement bulb installed).

## Power → Defense link (Cell Dock)
`PWR_DefenseCellDock` is the Charged Cell's destination in a shared scene. If the scene has a Defense Yard
(`DEF_GameState`) and nobody placed a dock, `PWR_LevelDirector` spawns one 1.6 m east of the Defense ammo
intake. To choose the spot yourself, put an empty GameObject with `PWR_DefenseCellDock` in the scene.
- The player carries the Charged Cell along the station routes and lets go of it inside the dock. Only the
  exact cell from `PWR_CellCharger.ChargedCell` is accepted, and only once `StationComplete` is true;
  anything else gets an explanation.
- Once docked, the cell stays there and every `DEF_ShockTrap` (also ones built later) gets
  `rechargeSeconds` 7 → 4. An arc and a "+Overcharged" popup show each trap being boosted.
- `PWR_DefenseCellDock.Instance.IsDocked` / static event `CellDocked` if Defense wants to react.
- The dock has no solid colliders (one trigger only), so it never blocks shots, arcs or zombies.

## Progression (MP2B)
`PWR_Progression` (added at runtime by `PWR_LevelDirector`) gates the station in five thresholds:
Floodlight lit → generators + barrels/crates + Refine Oil · first generator running → pads/belts + scrap +
chute/bin · first bin collection → Fuse/Battery/Bulb recipes + electrolyte tank · first Battery crafted →
Cell Charger · station complete. Each shows a reveal card first; `PWR_Progression.Instance.NextGoal` is
the current goal (also shown in the wrist menu).

## Shared scene
`PWR_AreaGate` (added at runtime) treats the build grid + 3 m as the Power area. Outside it the station
restores the scene's own ambient light and lights, closes its wrist menu and ignores its build controls
(Y, B, right trigger placing, right stick rotating). Check `PWR_AreaGate.PlayerInside` if needed.

## What it consumes
Nothing from other stations (MP2A standalone rule). Everything is gathered in this room:
- Scrap (scattered on the floor) → conveyor belts
- Crude Oil (oil barrels) → Refined Oil at the workbench → Oil Generator
- Coal (wooden crates smashed with the wrench) → Coal Generator
- Copper Wire + Glass (delivery chute, carried by player-built conveyors to the receiving bin)
- Electrolyte (tank next to the workbench)

## How the player interacts
1. Wrist menu: place the Battery Bank, Coal/Oil Generators, Concrete Pads and Conveyor Belts on the grid.
2. Grab a barrel + trigger to draw oil; swing the wrench at crates; pick up scrap and coal.
3. Pull the trigger on a generator to refuel it; generators charge the Battery Bank, which powers
   neighbors (8 surrounding cells): the Floodlight and the Cell Charger.
4. Workbench recipes: Refined Oil, Fuse, Battery, Light Bulb.
5. Insert the Battery into the Cell Charger; replace the Floodlight bulb when it burns out.
6. At 100% the Charged Cell pops out and the level is complete.

## Folder layout
`Scripts/` (runtime + `Editor/`), `Prefabs/` (+ `Defs/` build definitions), `Models/` (+ `Textures/`),
`Materials/`, `Textures/`, `Shaders/`, `Scenes/`.

`Tools > PWR > Build Assets + Setup Scene` regenerates the generated textures, materials, prefabs and the
`PWR_Station` root in the scene. Manual edits inside `PWR_Station` are overwritten by it; edit objects
outside that root freely.

Shared dependencies (not in this folder): XR Interaction Toolkit Starter Assets (`Assets/Samples`), TextMesh Pro,
OpenXR settings (`Assets/XR`).
