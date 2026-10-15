# Power Station (PowerYard) — Handoff Note

Owner prefix: `PWR_` · Namespace: `Deadzone.Power` · Scene: `Scenes/PWR.unity`

## What it produces
- **Charged Cell** (`Prefabs/PWR_ChargedCell`): a physical, grabbable item ejected by the Cell Charger when
  charging reaches 100%. This is the station's output, meant to be carried to the next room.

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
