# Workshop → Defense recording review checkpoint

Branch: `daryl/mp2b-workshop-defense-recording`. Worktree: `workshop-defense-recording`. Main and the older recording branch remain unchanged. Local source candidate is ready for independent review; publication remains held for parent approval. No Android build or actual headset validation is claimed.

## Final native evidence

2026-10-28 03:51:29 UTC: **360 passing assertions, zero failures/exceptions** in one uninterrupted Unity Editor Play acceptance run. Two fresh recording paths reach **earned Missile selection**, separated by a full scene reset while holding a genuinely recrafted kit. This is not the optional Missile splash/Juggernaut/final Complete ending.

Exact log: `../workshop-native-final-360-pass.txt`.
SHA256: `E0C77B42EB0F0E2CD2E678C95F4C6710BAE7AAB0D114258DA38E104C4B396683`.
The Editor automatically returned to Edit Mode; no test/build process remains running. Earlier failing and 316-pass logs are preserved separately, not conflated with this result.

Passed on actual scene objects: spawn floor/capsule clearance; one XR rig; disabled Jump/A collision; safe contiguous Workshop–Defense walking route; thumbstick locomotion; real Workshop controller-ray/trigger harvest/process/craft/transfer; A open/close; real selected-panel X cancellation without spending; ground-ray/trigger Sentry placement; zero starting ammo; actual XRI grip/carry/release of the same crafted kit and box; positive repair/refill; real first-wave combat; nonempty-wave Pause/Resume; actual walked barricade detour and Shock stun; earned Missile UI selection; three genuine trophies; held-item reset; empty fresh inventories and cleared trophies/mastery; correct post-reset Canvas camera bindings; second complete recording path.

Reveal assertions inspect actual card roots, receiver colliders/renderers and HUD controls. The sentry stays at full health during repair introduction; damage/receiver access appear afterward. Whole advanced cards stay hidden until their introductions finish. Five fresh cause/remedy checks preserve resources/items. Some negative checks and advanced placements invoke production methods directly; they do not prove every manual controller attempt. No mastery receipts, kills, completed yards or supplies are fabricated.

After the passing run, the ONLY production change was a wording correction from “has deliberate training damage” to “will receive training damage,” matching the tested delayed-damage behavior. Final Unity compilation is checked separately. No gameplay, geometry, input or economy changed after the passing run.

## Rendered review

Reviewed actual Play-scene renders from the player's position with a temporary diagnostic look direction:
- `../recording-first-spawn.png`: clear world geometry and readable simplified-run instructions.
- `../recording-first-open-shop-PlaceSentry.png`: only the Sentry card visible before advanced unlocks.
- `../recording-first-open-shop-MissilePractice.png`: earned cards, readable costs, corrected nonpausing and physical-repair instructions.
- `../recording-first-trophies.png`: three visible named earned objects from an accessible front position.

These are actual scene renders, not Quest captures. The separate `recording-final-shop-layout-preview.png` was an Edit Mode layout inspection and is not gameplay proof.

## Scope and handoff

One saved entry scene: `Assets/DefenseYard/Scenes/DEF_Workshop_Defense_Recording.unity`; it is the only enabled build scene. Workshop harvesting/crafting and physical transfers are real; Power/Clinic are explicitly omitted. No fixture preload. Failure recovery restarts the whole recording run. The short control/recording guide is `RECORDING_CONTROLS.md`.

Source-only allowlist preserves existing metadata and excludes Library, generated importer/URP drift, LFS hydration noise, logs, screenshots, binaries and private files. Staged credential-pattern scan found zero matches. Existing unrelated working-tree hydration/import noise is not part of the candidate.

Remaining checks: independent source review/publication decision, then real Quest Link smoke verification of hardware tracking, comfort, stereo readability and recorded error attempts. Optional final game victory is untested. No Android build for this branch; available disk remains about 12.6 GiB and no additional deletion is authorized.
