# Integrated XR fixes — 2026-10-29

Base: 0c89cbea2eba2b03c2d8e8b983631fbc63e80fa1 (includes PR #11).

Restores camera-following Defense HUD/objectives, stationary shop placement, A close/reopen, late-XR Power menu recreation, and carry attachment input arbitration. Docked cell colliders are retired after acceptance; disabled placement previews are excluded from overcharge. Includes the recording branch's outward clearance escape. Jump moves from right A to left X in shared XRI defaults; Power and Clinic source scan found no primary-button binding, but hardware conflicts remain untested.

Adapted scoped fixes from DarylOkeke's recording commits 397573b8262d40958265b769f429db11c8175d97 and 47e437cd3d862362bc7ef515def2d31514a1bb4f. Preserves ScatterBra's Power/Defense link. Does not import recording prerequisite bypasses, reduced prices, preloaded supplies, shortened waves, reset shortcuts, or recording build entry.

## Evidence

- Independent source review of the combined production changes and regression fixtures found no concrete production-code blocker.
- Unity 6000.5.6f1 ScriptAssemblies build backend exited 0 (32 seconds); runtime and editor DLLs were generated and domain reload completed. No C# compiler errors were found in the captured log.
- Full import/validation did NOT finish. The isolated Unity process was stopped as C: free space dropped to approximately 2.3 GB. This is script compilation evidence, not a successful Unity validation run.
- No EditMode assertion suite, Play Mode traversal, headset test, APK build, or final recording-readiness claim is made.

## Resume validation

Free approximately 3 GB additional space before resuming import; an APK may need more. In an isolated project, run `DEF_IntegratedReleaseValidation.RunBatch` via Unity batch mode. It includes existing audit/Power/Clinic suites, clearance escape, synthetic presentation/dock checks, XR configuration, and production entry/rig checks.

Then verify actual headset A open/close/reopen, UI ray clicks, late headset startup, both grips with simultaneous move/turn, Power cell release and desktop repick rejection, real/later traps versus previews, full three-station prerequisites, Defense retry and full restart. Synthetic fixtures do not cover actual input, deferred destruction, physics delivery or complete gameplay. Carry settings intentionally only remove attachment manipulation; broader arbitration requires runtime verification.
