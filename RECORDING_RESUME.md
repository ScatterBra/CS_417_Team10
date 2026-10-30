# Recording-only Barricade/Shock continuation

**Branch: `daryl/mp2c-advanced-recording-resume`. Do not merge into main.**
This separate checkpoint continues after the first Defense wave and the already-recorded Barricade/Shock introduction. The later `daryl/mp2c-defense-integration-resume` checkpoint and main remain unchanged.

Open `Assets/Scenes/Deadzone_Final.unity`, enter Play, and wait for `RECORDING RESUME READY` in the Console. You start near the outpost north entrance. The current objective asks for a moving-enemy barricade detour and a live Shock stun. No wave starts automatically.

## Record these remaining actions in order
1. Start the practice wave. Place a barricade across a moving enemy's route and let the enemy actually walk the detour. Simply placing it or having it absorb damage is not enough.
2. Place Shock so it stuns a live enemy. Both real actions are needed; their order does not matter. Keep footage of the resulting Missile introduction and unlock.
3. Once the Missile introduction finishes, place a Missile Sentry. Get one explosion to damage at least two distinct enemies. Repeat the practice wave if needed; it does not auto-complete this requirement.
4. Finish any remaining practice enemies, then start the real Juggernaut challenge. Keep the base alive, have at least two defense types contribute against enemies including the Brute, and physically repair and refill DURING that wave. Pre-wave repair/refill does not count toward its sustainment requirement.
5. Show the integration loop: grip the Medicine package in the OUTPOST MEDICAL SUPPLIES bin beyond the north service gate; use its normal 15% movement boost to run to Workshop, craft/grab ammo and repair kits, and return to physically deliver them. The boost lasts60seconds across yards and does not speed crafting.

## Explicitly prepared starting state
Earlier station prerequisites, self-treatment and early Defense tutorials are prepared debug state, NOT new evidence of completing them. No historical completion/craft/delivery receipts are replayed. Use the original footage for those milestones; describe this footage as a resumed recording, not one uninterrupted fresh run.

- Current phase is AdvancedPractice. No detour, stun, splash or final-challenge evidence is seeded.
- Only a basic sentry is placed; it begins40integrity below full. Missile remains genuinely locked until the actions above unlock it.
- Defense500scrap, healthy base, ammo10/60. One physical AmmoBox and RepairKit are inside the outpost, outside their receivers, ready for normal pickup/release consumption.
- Charged Cell is already mounted in the dock; subsequent Shock recharge uses the normal Power benefit.
- One prepared Medicine package is already in the medical bin. It is consumed normally, not infinite. This first prepared package is not proof of cart delivery.
- Workshop200additional scrap and60additional parts support finite, normal new crafting. Its workbench, processor and supply interactions are unlocked.
- Clinic20Herbs/10Water support later real crafting; build a PrepBench normally if needed. The real incoming-ingredient/outgoing-Medicine cart cycle remains available.

Refill and prepare before starting if necessary: the basic sentry can kill targets before the detour/stun demonstration, and low ammunition can halt firing. Nothing grants automatic wins, infinite stock, remote refill or fabricated advanced progress.

The bootstrap runs once per fresh Editor Play/final-scene load. Normal Retry returns to the ordinary early Defense tutorial and does not replenish this checkpoint. STOP and re-enter Play if you need to restart this prepared recording. Nothing is saved to the scene.

## Verification
Static regression checks confirm AdvancedPractice, displayed prior wave1, no preplaced missile, no seeded advanced evidence, and unchanged real progression/consumption code. No Unity/headset runtime pass is claimed. Runtime acceptance: see the correct objective and locked missile, earn both control actions, observe Missile reveal/unlock, earn two-target splash, then see Juggernaut readiness and complete its real requirements. If `RECORDING RESUME FAILED` appears, report the full error rather than recording a partial fixture.
