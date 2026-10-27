# PR10 reconciliation — code review checkpoint

Merged Suhaas's PR10 f039673 with owner commit history intact. Standalone MED_Clinic scene, patch layouts, trophies, achievements and reveal content are retained. Integrated Defense prerequisites remain ordered Clinic7–10 plus authenticated medicine/healing evidence; sterilization is optional and owner ClinicComplete is not a Defense receipt.

Resolved five conflicts preserving atomic medicine transfers and paid recipe/dose provenance. Owner craft/stock/dose notifications follow successful committed actions. Integrated owner progression detects the scene's DEF_ClinicTraining, avoids its own threshold/reveal loop, ignores SkipToNext, and reloads the scene for full reset. Defense retry retains its existing behavior. New patch stamping retains a marked learning-source fallback for older integrated scenes without patch assignments.

Scene migration is NOT executed: open each integrated scene in Unity, run Defense Yard > MP2B > Bind Owner Clinic Managers In Active Integrated Scene, inspect trophy shelf position, and save. The command binds progression, area gate and achievement managers. Existing isolated scenes also still require current route/gate propagation. Do not infer that merging owner standalone scene updates copied integrated scenes.

Validation: source conflict markers cleared and conflict index resolved. Added three Edit Mode assertions for integrated owner mode and debug-skip rejection; these have NOT run. No Unity compilation, live play or Quest validation performed for this merge. Earlier 202-pass candidate results predate this reconciliation.

Required Unity/Quest checks: compile; rerun all evidence and ordered training suites; test harvest→wild+cart→bench/paid Medicine→stock/heal100 without sterilization; verify owner standalone craft→stock→dose→sterilize→100 reveals; test cabinet FIFO and foreign/debug medicine rejection; reset mid-craft/dose; retry Defense without losing prerequisites; inspect area gating, miniature visibility, trophies and all integrated scene routes. Optional owner achievement events remain legacy global presentation events and must not be treated as authenticated Defense evidence.

## Single release command and compiler result
Use Defense Yard > MP2B > Build Integrated Candidate APK (No Run), or batch executeMethod DEF_QuestPackaging.BuildIntegrated in an isolated Unity checkout. This refreshes staging, Quest and desktop copies, applies ordered training/restart/routes and dedicated Quest pipeline, binds owner managers, strips Quest simulator controls, validates routes, runs full/Power/Clinic/ordered training fixtures, rejects failed reports or missing manager/evidence bindings, then packages. Saved scenes are not trusted without preparation. Fixture assertions require Unity; no runtime test pass is claimed here.

Standalone Unity 6000.5.6f1 Roslyn compilation of release Assembly-CSharp and Assembly-CSharp-Editor completed with exit 0, using read-only candidate dependency references. Samples retain separate existing assemblies. This verifies C# compilation, not Unity import, serialization, native calls, scene behavior or Quest compatibility. No GUI or Unity process launched.

Remaining operator action: open the release checkout in Unity after other work is stopped, invoke the single command, resolve any reported validation failure, then install the resulting APK and exercise the documented Quest checklist. Main merge remains held for source/Muse audit and that release validation. No guarantee of zero device issues.
