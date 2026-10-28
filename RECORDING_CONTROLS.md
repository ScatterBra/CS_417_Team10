# Simplified Workshop → Defense recording

This separate branch omits Power and Clinic prerequisites. It does **not** complete the original three-yard graph. Supplies are genuinely harvested, crafted and physically delivered; Defense mastery is earned normally. Main and the older recording branch are unchanged.

## Open and control

Open `Assets/DefenseYard/Scenes/DEF_Workshop_Defense_Recording.unity`. Start Quest Link before Play. This is the headset scene: do not enable a desktop simulator or add another rig. The saved scene is also the branch's only enabled build scene.

- Health, resources and the current objective follow the tracked head in stereo world space. The shop remains anchored where opened.
- Left stick: move; right stick: turn, including while either or both grips hold supplies.
- Point the right controller at a button; squeeze and release its trigger to click.
- Right **A**: open/close Defense build shop, after Workshop readiness. It appears in front of your current view and stays in that position until closed. Reopen to reposition it. Jump is disabled; the outpost has a north doorway and ramp.
- Face open walkway space before opening the shop; an obstructing wall prevents opening. Turn toward clear space and press A again.
- In the shop click the green cost button under the wanted defense. Point at clear ground outside the base; right trigger places it.
- To cancel an equipped defense without spending, click **X on the selected-defense panel**. The shop close X only closes the shop.
- Hold grip while pointing at/touching the actual crafted object to grab. Keep holding while carrying. Release inside its matching receiver to deliver. A held object is deliberately rejected.
- Supplies drop rather than inherit throwing speed from a snap turn. If one falls below the walkway, the same crafted item returns to its last supported position; no inventory or progress is granted.
- Shop stays movement-safe and does not pause physics. **Pause** really pauses combat; point at **Resume** and trigger to continue.
- A failed run's **RESTART RECORDING RUN** starts over, including Workshop supplies and trophies. It does not revive consumed items.

## One continuous recording route

1. Start in Workshop. Use **HARVEST SCRAP** six times on one wreck: 12 Scrap. Wait for the Crusher introduction to finish. This recording branch omits Power and Clinic; it is not full integrated completion.
2. At Crusher, press **PROCESS SCRAP** three times, waiting for each process to finish. Each converts 2 Scrap into 1 Part: 6 Scrap becomes 3 Parts total. Wait for the Workbench introduction.
3. At Workbench craft one **RepairKit** (2 Parts) and one **Ammo Box** (1 Part). Keep both physical outputs. Wait for the ready introduction; don't use old teaching receivers.
4. Follow the direct west walkway through the open east Defense gate. Press **A** to open the shop. Its header distinguishes **DEFENSE** and **WORKSHOP** scrap. Click **TRANSFER 2 SCRAP / WORKSHOP > DEFENSE** twice: 4 Scrap moves to Defense, 2 remains at Workshop. This is the first real cross-yard connection; money is actually debited and credited.
5. Select the Sentry **4 SCRAP** button. Aim at clear east-side ground near `(7, 0, 1)` and place it; it costs 4 Defense Scrap and starts with **zero** ammo. The north ramp near `(1, 0, 6)` enters the raised outpost without jumping, though building takes place outside it.
6. Read the repair introduction: the Sentry remains at full health until it finishes, then training damage and the receiver appear. Retrieve the SAME crafted RepairKit from Workshop. Carry it to the placed Sentry's receiver. Show the held-item instruction, then release it inside. Show integrity increasing and **FIELD MEDIC** appearing. This is the second connection.
7. Read the ammo introduction. Retrieve the SAME crafted Ammo Box. Release it at the Defense ammo intake near `(5, 1, 5)`. Show the positive refill and **SUPPLY LINE** trophy. One box supplies up to 10 rounds. This is the third connection.
8. Read the first-wave introduction, then aim at the green **START WAVE [MENU]** button below the camera-following objective and press trigger. The left controller Menu button is also a shortcut. The recording encounter comes from the east. Let the Sentry defeat it; show the real clear and **WAVE DEFENDER** trophy.
9. Read **BARRICADE AND SHOCK** before using either. Start the next practice wave. You can equip the barricade before starting the wave, then place it across a visible moving enemy's route so it actually walks around it; place Shock beside the route so it stuns a live enemy. A path recalculation alone does not count.
10. Read **MISSILE SENTRY** when both outcomes are earned. After the wave clears, open the shop and show the unlocked Missile Sentry card and its **20 SCRAP** button. This is the currently tested recording endpoint, **not** the game's final Complete state.

If you want the optional later game ending, Missile must actually hit two enemies in one explosion, followed by the Juggernaut sustainment requirements shown in-game. That later ending is not covered by the recording acceptance run.

## Five distinct error clips, with safe recovery

Keep these in the same run where practical. Capture the actual message, not only a disabled button.

| Attempt | Expected cause/remedy | Recovery |
|---|---|---|
| Select Sentry before transferring the 4 Scrap | Not enough scrap; transfer Workshop scrap | Transfer twice, then equip |
| Aim a selected defense inside the base and trigger | Inside-base/non-buildable rejection; choose outside ground | Place on clear east-side ground |
| Bring Ammo Box to the repair receiver | Not a RepairKit; bring the correct kit | Keep the unconsumed box for the ammo intake; deliver the kit |
| Keep holding the kit inside its receiver | Kit still held; release it inside the receiver | Release; real integrity increases |
| Aim a selected defense at an occupied tile and trigger | Occupied; use empty valid ground | Aim at an empty tile and place |

Automated coverage uses real controller input for the main Workshop/UI/placement/carry route. Some negative transaction assertions and advanced placements call the same production methods directly. Real Quest comfort, visual legibility and those exact recorded attempts still require the headset smoke test.
