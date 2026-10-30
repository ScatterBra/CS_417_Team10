# Recording-only Defense integration checkpoint

**Branch: `daryl/mp2c-defense-integration-resume`. Do not merge into main.**
Base: `f4f562a4b7cea27b85a505025ba62b6f2448681e`.

Open `Assets/Scenes/Deadzone_Final.unity`, enter Play, and wait for the Console message `RECORDING RESUME READY`. The bootstrap runs only in the Unity Editor, only in this scene, once after normal scene initialization. No saved scene or production gameplay file changes.

## Prepared starting state—not proof of earlier gameplay
- Station prerequisites and Defense tutorials are explicitly prepared. No historical completion/craft/delivery receipts are replayed.
- Clinic self-treatment is prepared; one clearly named prepared Medicine dose is already visible in the OUTPOST MEDICAL SUPPLIES bin beyond the north service gate. Grip it for the normal 15% movement boost lasting60seconds across yards.
- Charged Cell already mounted in the outpost dock; real Shock Traps use its normal faster recharge.
- One physical AmmoBox and one RepairKit inside the outpost, separate from their receivers. Pick up and release into the correct receiver to consume them normally.
- Defense500scrap, base healthy, ammo10/60, basic and missile sentries placed. Basic sentry starts damaged for a real repair demonstration.
- Workshop200additional scrap and60additional parts: workbench/processor/supply interactions unlocked. Actual new ammo and repair-kit crafting spends real finite material and creates normal physical items.
- Clinic20Herbs/10Water added for later real crafts/cart deliveries; the normal incoming-ingredient cart cycle remains active. Build a PrepBench normally if returning for more Medicine.

The next START WAVE runs the real Juggernaut challenge. Place any additional defenses first. To complete it, use at least two defense types against enemies including the Brute, and perform physical repair and ammo refill DURING the active wave. Pre-wave actions do not count as final-challenge sustainment. No wave is automatically won; depleted ammo, repair costs, damage, the Medicine timer and subsequent deliveries remain normal.

For integration footage, take the speed boost, run to Workshop, craft/grab supplies, and return to physically repair/refill. Describe this as a resumed/prepared recording, not a single continuous full run or fresh proof of the seeded station milestones.

If you lose and want this checkpoint again, STOP and re-enter Play. Normal Retry still returns to the normal early Defense tutorial; this branch deliberately does not grant new stock on every retry. A full scene reload also recreates this prepared checkpoint. Nothing is saved back to the scene.

Static/API review only; actual Unity/headset execution is not claimed. If `RECORDING RESUME FAILED` appears, do not treat a partially prepared scene as ready; share the full error.
