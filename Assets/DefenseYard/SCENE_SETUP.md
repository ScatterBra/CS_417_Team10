# Defense Yard - Scene Setup (Phase 1)

One-click setup. No manual steps.

## Steps

1. Open the project in Unity and let it import.
2. Pull the daryl/defense-yard-phase1 branch.
3. In the Unity menu bar, click Deadzone > Build Defense Yard Scene.
4. It creates the camera rig, game state, grid, selector, HUD, and a light,
   then saves Assets/DefenseYard/Scenes/DefenseYard.unity.
5. Hit Play.

Delete Assets/DefenseYard/Editor/ after if you want, the scene stays built.

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
