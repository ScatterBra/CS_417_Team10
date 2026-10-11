# Deadzone Team Conventions

## Ownership prefixes
Every custom asset, script, and prefab uses its station prefix. Never use another station's prefix.

- DEF_ — Daryl, Defense Yard
- SCV_ — Scrapyard and Workshop
- PWR_ — Power Station
- MED_ — Clinic

## Standalone rule (MP2A)
Every station works alone in its own scene. No station imports code, prefabs, or objects from another station's folder. Local supplies only. Connections between stations are planned for later, not built now.

## Folder layout
Assets/<PREFIX>/Scenes, Scripts, Prefabs, Materials, Textures

## Handoff note (for later integration)
When your station is done, include a short note in your folder: what it produces, what it consumes, and how the player interacts with it.
