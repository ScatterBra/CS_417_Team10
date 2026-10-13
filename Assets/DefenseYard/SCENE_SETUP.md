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

---

# Defense Yard - Phase 2 (wave combat)

One-click setup. Run Phase 1 first.

## Steps

1. Pull the daryl/defense-yard-phase2 branch.
2. In the Unity menu bar, click Deadzone > Build Defense Yard Phase 2.
3. It adds the wave manager, pathfinder, threat meter, placement input,
   three ammo crates, and spawn portals at the north and south edges,
   then re-saves Assets/DefenseYard/Scenes/DefenseYard.unity.
4. Hit Play. Press Space to start a wave.

## Controls

- Click a tile, then T places a turret (40 scrap), Shift+T places an
  explosive turret (70 scrap)
- B places a barricade (20 scrap), R repairs the barricade on the
  selected tile (10 scrap)
- H harvests 10 ammo from the crate on the selected tile
- Space starts the next wave; zombies path around turrets and barricades
  and repath when you build during a wave

## Rubric stories touched

Harvesting (ammo crates), Caretaking (barricade repair), Collision
Content (projectile hits zombie, zombie hits barricade/turret, explosive
AoE), Intermittent Instances (wave spawns), Path Following and Path
Planning (BFS repathing around placed obstacles), Population Resource
with Pathing (threat meter scales spawns), Solar Coaster (muzzle flash
UV scroll), Solar Blast (explosion ring), UV Distortion (spawn portal).
