# Recording branch WIP checkpoint — 2026-10-28 02:21 UTC

NOT RELEASE READY. No publication authorized until parent evidence review.

Latest native run: 32 passes, terminal failure at Workshop readiness after actual input-driven traversal. Actual thumbstick locomotion reaches harvest, crusher and workbench standing areas along capsule-checked floor paths. Six controller-trigger harvests and three controller-trigger process transactions pass. Workbench sequence produces only RepairKit: runtime ledger parts=1, ammo=0, repairKits=1; WorkshopComplete=false, DefenseSupplyReady=false. Investigate why actual Ammo UI click misses before retrying the complete run. The readiness gate correctly refuses incomplete crafting.

The live recording Editor (PID 42056, project workshop-defense-recording, existing bridge port 7803) remains in failed Play for inspection. Probe reached terminal exception; no active batch build or background acceptance loop. No approval pending. Old candidate/editor untouched.

Earlier native evidence (before newest locomotion/zero-preload expansion): actual A open/close, trigger-driven Defense UI purchase/start, real XRI kit and ammo selection, controller-driven carry/release and exact physical delivery, live first-wave combat, and three genuine trophy awards passed. Earlier first-wave run still had the original explicit 30-ammo training baseline; current composer removes it (trainingAmmo=0). Zero-preload combat has NOT yet passed. The earlier proof must not be represented as a current end-to-end pass.

Advanced diagnostics were split: fixed barricade at (15,10) gave Shock stun but no route receipt; live-route barricade at (17,10) gave route receipt but sent actors away from the fixed Shock. Test now waits for actual walked detour then places Shock beside a live enemy's observed detour. Combined revised strategy is not yet reached/passed.

Implemented but not fully signed off: ordered Workshop-ready introduction before Defense affordance; local visible physical-intake feedback; fresh cause/remedy checks for insufficient scrap, inside-base, occupied tile, wrong item, held item; three initially-hidden earned trophies; full reload reset while holding a genuinely recrafted kit; direct 12m x 6m safe floor connection between Workshop and Defense; actual ground-ray/trigger placement coverage. Held-reset has not passed yet (prior fixture recipe error corrected). Native layout checks pass saved HUD/camera bindings; Workshop screenshot shows resized sign, but Defense panels/trophies/intake labels still require rendered review. Late XR and complete optional Missile/Juggernaut traversal remain unverified.

Evidence outside repo: ../workshop-native-play-results.txt (latest failure), ../workshop-native-first-wave-pass.txt (older limited pass), ../recording-workshop-live.png (actual desktop camera capture). Unity compilation completed without errors immediately before latest native run. No Android build for this branch, no headset validation, no commit/push to main.

Next: diagnose the missed Ammo UI ray/click in retained Play state; fix actual reachability or fixture input timing as evidence warrants, then repeat real-input complete traversal, zero-preload combat, advanced detour/stun, held/full reset; inspect rendered Defense UI/labels/trophies; finish source/meta/settings review before parent publication approval.
