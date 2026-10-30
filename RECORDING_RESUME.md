# Recording-only Missile continuation

**Branch: `daryl/mp2c-advanced-recording-resume`. Main and the separate late checkpoint remain unchanged. Do not merge this recording fixture into main.**

This checkpoint now resumes at **MissilePractice**, because the Barricade detour and Shock stun have already been recorded. Earlier milestones are prepared starting state, not new evidence. No splash or final-challenge evidence is seeded.

Open `Assets/Scenes/Deadzone_Final.unity`, enter Play and wait for `RECORDING RESUME READY`. Place your own Missile Sentry; only the damaged basic sentry is preplaced. Start a practice wave and show one missile explosion damaging two distinct enemies. Keep the current objective and successful progression visible. Finish remaining practice enemies, then start the real Juggernaut challenge. Use two defense types, physically repair and refill DURING that wave, and keep the base alive.

The AMMO INTAKE remains available once Ammo has been introduced, including during later introduction cards. Physical repair similarly remains available after its initial introduction. These changes do not bypass box/kit identity, release, Workshop ledger, capacity, positive repair or failed-run validation. After base loss, the learned intake stays visible and explains that you must restart or retry. Delivery still rejects without consuming the box; resupply cannot revive a failed run.

Prepared stock: Defense500scrap and10/60ammo; one physical AmmoBox and RepairKit in the outpost, outside their receivers; Workshop200additional scrap and60additional parts; Clinic20Herbs/10Water. Stock and ammunition are finite. Refill before starting if necessary, then show a real refill again during combat. The real workbench spends materials and creates new physical supplies.

The dock contains a prepared Charged Cell and the outpost medical bin contains one prepared Medicine package. Grip that package for the normal15% movement boost lasting60seconds across yards, then run to Workshop for ammo/repair supplies and bring them back. This first package is not proof of cart delivery. Further Clinic crafts and cart deliveries remain normal and must be performed genuinely.

If the run fails, STOP and re-enter Play to restore this prepared checkpoint. Ordinary Retry still returns to early Defense training; this branch never automatically grants new stock on retry. Full scene reload recreates the fixture. Nothing is saved into the scene.

Static/source review is not a Unity/headset runtime pass. Verify walking, physical refill and the Missile objective before recording. Report the full error if `RECORDING RESUME FAILED` appears. Describe footage as a resumed recording, not one uninterrupted fresh run or new proof of the prepared milestones.

The recording-only movement guard retains the last valid capsule height when tracking briefly reports zero height and keeps step offset within that height. It preserves normal CharacterController collisions, camera pose, input and movement speed; valid tracked height resumes normally. It does not disable collision or force a disabled rig on. This addresses the observed Height0/StepOffset0.5 configuration; headset validation is still required.
