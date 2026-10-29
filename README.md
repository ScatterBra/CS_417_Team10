# CS 417 Team 10

## Open the final game

**`Assets/Scenes/Deadzone_Final.unity`** — Unity **6000.5.6f1**.

This is the integrated Workshop, Power, Clinic, and Defense scene and the only enabled build scene.
Standalone scenes and development demos live under `Assets/Development/Scenes/`; they are not the final game.

## Team Members

- Alleline He
- Daryl Okeke
- Felix Romero
- Suhaas Kowthalam Jois

## Defense Yard (standalone development scene)

For Defense-only development, open `Assets/Development/Scenes/Defense/DefenseYard.unity`.
Defend a ground-level outpost from all four sides with sentries, barricades, and
shock traps. The opening training and field guide explain building, supply
collection, repairs, and waves. Scene objects and UI are editable prefab instances.
The scene includes the official XR Interaction Simulator as an `EditorOnly` prefab
instance for PC testing; it is omitted from player builds. Shared XR settings are
left to the team project. Native Quest validation is still required.

Additional CC0 environment models/textures are credited in
`Assets/DefenseYard/Art/World/SOURCES.json` and `Assets/DefenseYard/Art/BaseKit/SOURCE.txt`.

## Third-Party Assets (CC-BY 4.0)

The Defense Yard uses the following CC-BY licensed models from Sketchfab, in `Assets/DefenseYard/Resources/Models/`:

- Archived original Zombie Hazmat (no longer used by enemy prefabs) by LxNazarov — https://sketchfab.com/3d-models/zombie-hazmat-49b3b4307f6a4d2386fdb02354158d04
- Sci-Fi Turret Animated by berkgedik — https://sketchfab.com/3d-models/sci-fi-turret-animated-d855153c0e844a49b2ccf56c77f178b5
- Turret Gun Flash Animated by berkgedik — https://sketchfab.com/3d-models/turret-gun-flash-animated-3ab8ed35c31c43aea8ccb971110b218a
- Low Poly Barricade Game Ready II by .arthur — https://sketchfab.com/3d-models/low-poly-barricade-game-ready-ii-89a79068de614a478aeaea49371cfef8
- Old Ammo Crate by SuperMopsek — https://sketchfab.com/3d-models/Old-Ammo-Crate-6a834e917e4f4194a49e9f66e6915d8a (original .dae converted to FBX)
- Cracked Concrete Old 3D Scan by grafi — https://sketchfab.com/3d-models/cracked-concrete-old-3d-scan-2d01ca3fd8fb4e96bf591110389a128c

The missile sentry uses [Missile Turret by Zsky](https://poly.pizza/m/RqnAj5N9fL), from [Low Poly Sci Fi Turrets](https://zsky2000.itch.io/low-poly-sci-fi-turrets), under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Converted to FBX; materials, aiming joints and recoil animation adapted for this project. Attribution is also in `Art/MissileTurret/SOURCE.txt`.

The three non-hazmat infected use the animated [Zombie Apocalypse Kit by Quaternius](https://quaternius.com/packs/zombieapocalypsekit.html), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/). Source/conversion notes are in `Art/Infected/SOURCE.txt`.

## Third-Party Audio (CC0 1.0)

Sound effects in `Assets/DefenseYard/Resources/Audio/` are public domain (CC0), no attribution required:

- Turret shots, explosions, UI clicks, pickups, wave alerts: Kenney audio packs (https://kenney.nl/assets) — Sci-Fi Sounds, Impact Sounds, Interface Sounds, Digital Audio
- Zombie growls: Ezcah ("Zombie Growl 01") and finnskog ("Zombie Growl") via Freesound (https://freesound.org), both CC0

- Ammo collection: [pickup_ammo.wav by Thunderman98](https://freesound.org/people/Thunderman98/sounds/656183/), CC0. Public high-quality preview layered with Kenney Impact Sounds light-metal recordings into a 0.72-second cartridge-rattle effect; normalized with a soft fade.

Visible controller geometry is from Unity XR Interaction Toolkit 3.5.1 Starter Assets (Universal Controller), distributed under the Unity Companion License. Source details are in `Assets/DefenseYard/Art/XRControllers/SOURCE.txt`. The shock trap reuses the credited CC0 portable generator environment model, with project-authored electrical coil and stun effect.
