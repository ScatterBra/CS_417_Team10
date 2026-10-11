# Deadzone Team Conventions

## Station folders (Assets/)
- DefenseYard/ — Daryl (Defense Yard)
- Scrapyard/ — Scrapyard and Workshop
- PowerYard/ — Power Station
- Clinic/ — Clinic

## Ownership prefixes
Every custom asset, script, and prefab uses its station prefix. Never use another station's prefix.

- DEF_ — DefenseYard
- SCV_ — Scrapyard
- PWR_ — PowerYard
- MED_ — Clinic

## Standalone rule (MP2A)
Every station works alone in its own scene. No station imports code, prefabs, or objects from another station's folder. Local supplies only. Connections between stations are planned for later, not built now.

## DefenseYard layout
Scenes, Scripts, Prefabs, Materials, Textures, Models (downloaded .glb files go here)

## Handoff note (for later integration)
When your station is done, include a short note in your folder: what it produces, what it consumes, and how the player interacts with it.
