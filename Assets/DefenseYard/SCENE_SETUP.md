# Defense Yard - Scene Setup (Phase 1)

I can't assemble Unity scenes from here, so this is the 5-minute editor setup.
All scripts are in Assets/DefenseYard/Scripts/.

## Steps

1. Open the project, create a new scene: File > New Scene. Save it as
   Assets/DefenseYard/Scenes/DefenseYard.unity
2. Create an empty GameObject named "CameraRig". Drag the scene's Main Camera
   under it as a child. Attach DEF_CameraRig to CameraRig.
3. Create an empty GameObject named "GameState". Attach DEF_GameState.
4. Create an empty GameObject named "Grid". Attach DEF_GridManager.
5. Create an empty GameObject named "Selector". Attach DEF_TileSelector.
6. Create an empty GameObject named "HUD". Attach DEF_HUD.
7. Make sure the scene has a Directional Light (default new scenes do).
8. Hit Play.

## What you get

- 20x20 tile grid with 4 zones (concrete border, dirt yard, grass strip,
  metal pads), visible grid lines
- WASD/arrow pan, mouse wheel or +/- zoom, Q/E rotate, P toggles
  perspective/ortho, right-drag pans
- Camera stays inside the yard, zoom has min/max limits
- HUD: health, ammo, wave, clock, pause/play/fast-forward buttons,
  camera mode button, selected tile panel
- Click any tile to select it (yellow highlight) and see its zone

## Rubric stories touched

Movement, Zoom, Basic Camera, Textures (placeholder colors, real textures
later), GUI, Mouse, Game Time, Selection, Tile Textures, Camera Rotation,
Camera Bounds, Smooth Movement.
