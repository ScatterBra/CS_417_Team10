# Android / Quest preflight — local test configuration only

Actual read-only Unity preflight ran 2026-10-25 20:20:02 UTC. Report: workspace ../android-static-preflight.txt. No BuildPlayer invocation, target switch, Play Mode, install, device launch or publication occurred. The preflight menu only reads settings/assets and temporarily opens Defense scenes additively in Edit Mode, closing those it opened and restoring the active scene. It does not save scenes.

## Verified existing capability

- Unity6000.5.6f1 has AndroidPlayer installed; BuildPipeline reports Android supported. Active target remains StandaloneWindows64.
- Android ARM64, IL2CPP, Linear color space, Input System only, Vulkan then OpenGLES3. Minimum API32; target API automatic. Android quality index0 is Mobile URP; PC has a separate URP asset.
- Actual configured external paths exist: SDK C:/Users/dokek/AppData/Local/Android/Sdk; NDK subfolder27.2.12479018 (source.properties r27c); JDK C:/Program Files/Eclipse Adoptium/jdk-17.0.19.10-hotspot; Unity AndroidPlayer/Tools/gradle. SDK contains platforms35/36 and build-tools36.0.0. Existence is not a successful compiler/Gradle/license check.
- Packages: Input System1.20.0, XR Interaction Toolkit3.5.1, XR Management4.7.0, OpenXR1.17.1, Meta OpenXR2.5.1, URP17.5.0.
- Android XRGeneralSettings has InitManagerOnStart enabled and references OpenXRLoader. XRManager automatic flags are false, but local XRGeneralSettings implementation explicitly initializes the loader and starts subsystems when InitManagerOnStart is true; those flags alone are NOT proof XR startup is disabled.
- Android OpenXR enables Oculus Touch, Touch Pro, Touch Plus and MetaQuestFeature; MockRuntime is disabled. Android renderMode serialized1. No settings were changed. Current source supports controller actions, not a claimed hand-tracking-only workflow.
- Defense input binds right trigger/grip/primary and left/right menu through Input System XRController controls. XRI sample actions include pose/select/grip bindings; Felix's rig/wrist hierarchy is retained. DEF_VRInput editor simulator mode is conditioned on Application.isEditor. Actual controller mapping, grab/release and UI rays remain device checks.
- Inspected31 Defense scenes/prefabs and2217 hierarchy objects: zero missing MonoBehaviour scripts and zero unresolved serialized asset GUID references. This does not prove every intentionally null field is correctly configured or every local fileID link is valid. Full source readiness is intentionally missing in the locked demo.

## Safe changes made

DEF_Signal.shader and DEF_NightSky.shader now include instancing variants, vertex instance IDs, stereo varying data and vertex/fragment stereo setup macros using the existing URP Core include. These were absent and could produce incorrect single-pass stereo output. Both import with zero ShaderUtil messages in the current Windows editor. Android packaging subsequently compiled shader variants (see result below); both-eye rendering has NOT been observed on Quest.

Added Editor/DEF_AndroidPreflight.cs and its meta. Menu: Defense Yard > MP2B > Android Static Preflight (No Build). It reports module/target/backend/APIs, configured toolchain paths, global scenes, missing scripts/GUIDs and current-editor shader messages. It does not execute gameplay. No ProjectSettings, package manifest, XR settings or shared Integration changes.

## Initial preflight findings (superseded by packaging result below)

Current global build scenes are PWR, SampleScene and MED_Clinic; neither Workshop nor Defense demo is included. Current Android application ID is com.suhaas.clinic. Do not overwrite that identifier or global scene list silently for a Defense test. The shared integrated scene is not ready and real Power/Clinic readiness providers are absent; no final MP2B APK can be represented as complete.

The isolated ammo demo intentionally enabled the existing XRI simulator for Editor investigation. Package source contains runtime simulator OnEnable logic, so do NOT assume it disappears on Android. A test-build scene copy/processor must explicitly remove simulator objects and simulator UI while preserving the single real XR Origin, controller actions, interaction manager and Felix wrist UI. This has not been applied to a build or validated on device.

Proposed separate test recipe is in MP2B_ANDROID_DEMO_BUILD_PLAN.json. It is a reviewable recipe, NOT a Unity Build Profile and NOT automatically applied. No build command is attached. Before a future build: confirm the exact test scene copy, simulator exclusion, unique test app ID and output path, then use a normal Android compile/package operation without AutoRunPlayer. Such a build would be a distinct packaging check, never permission to launch the denied Play/simulator test. Installing/running on a headset remains separately reviewable. Do not use test setup, fake readiness or supply-only content as final integrated evidence.

## Initial proposed checks (historical; packaging completed below)

1. Preserve open work; prepare an explicitly named Android-only copy of the isolated supply demo, excluding simulator roots/UI. Keep final Integration scene owner untouched. Verify all required ledger/receiver, XR camera, input action manager and wrist references in that copy.
2. Agree a unique local test identifier (proposed com.cs417.team10.defenseammotest), output under Builds/DefenseAmmoTest, and test-only title. Do not reuse Clinic's installed-app identity. Confirm runtime Android toolchain compatibility with this Unity version via a real build report; do not install/update tools without authorization.
3. Run Android compile/package without launch when that distinct action is approved and scene preflight is ready. Capture exact BuildReport, warnings/errors, target/API and artifact hash; until then IL2CPP/AOT stripping, Android shader variants, manifest merge and Gradle packaging are NOT RUN.
4. Device checks: both-eye sky/signal visibility; actual XR startup; headset/controller poses; movement, grip/release, trigger and menu mapping; UI ray selection; exact physical delivery; stereo overlays/text; frame time/thermal behavior; suspend/resume and reset. Current137 Edit Mode gameplay checks do not establish any of these.
5. Final build later requires authentic full team graph, owner-composed scene list, agreed application identity, complete recording/contribution evidence and explicit publication/submission review. No APK has been created or uploaded by this preflight.

## Packaging smoke result — October 25, 2026

A separate authorized build-only operation completed at approximately 20:37 UTC. Unity BuildReport result: **Succeeded**, duration 13m10.224s, **1 error and 1,013 warnings**. This is a qualified packaging success, not a clean validation or gameplay pass.

- Explicit only scene: Assets/DefenseYard/Scenes/DEF_QuestPackaging_Demo.unity, copied from the locked progression demo. Removed XR Interaction Simulator object; verified one real XR Origin remains. Added INCOMPLETE DEMO label. Missing Power/Clinic readiness stays unassigned and locked. This replaces the earlier proposed supply-demo recipe.
- Output: Builds/DefensePackagingTest/20261025-202439/INCOMPLETE-MP2B-DEMO.apk, **156,805,878 bytes**. BuildReport totalSize 1,497,758,124 includes build outputs and is not APK file size. Backup/debug directories are not delivery artifacts.
- aapt confirms com.darylokeke.cs417.mp2btest (distinct from production com.suhaas.clinic), label INCOMPLETE MP2B Packaging Demo, version0.1.0/code1, ARM64 only, minAPI32/targetAPI36.
- SHA256: 66D203DD22A11AC771C180EA4CB4D8782624A12748553405F21CE7FFC0E890AB. Existing SDK apksigner verifies v2 signature, one signer.
- Build error is NullReferenceException in installed OpenXR MetaQuestFeature.CreateValidationRules, line539, dereferencing selected-build-target settings/feature during OnPreprocessBuild. Unity continued and produced the APK. This means the OpenXR validation stage is not trustworthy yet; do not claim all project validation passed. Warnings include unsupported simulator shaders. No package source or security settings were changed to suppress this.
- Runtime compilation, IL2CPP ARM64 compilation, shader processing, manifest generation and Gradle assembleDebug completed. No installation, execution, Play Mode, simulator or headset test occurred.
- Build wrapper restored original identifier/product/bundle mode and switched back to StandaloneWindows64. Disk serialization needed explicit cleanup of build-induced settings. ProjectSettings.asset SHA256 restored exactly to 3963006985D846C5DABC85A8F03FB96C9441DA9D213FE7475BA95EDDA386826C; EditorBuildSettings.asset unchanged at 616756356FB45185B93A720C3FAD3651B3B782C2EC6A85BB226726CE0793C2BE. Git reports no ProjectSettings changes. UI returned to existing WorkshopAmmo demo in Windows editing.

Evidence outside project root: quest-packaging-prepare.txt, quest-packaging-build.txt, ammo-static/pre-packaging-settings.json; full Editor log in Logs/Editor.log. No production APK was overwritten or external content published.

Next: investigate the OpenXR validation target-context exception before another build; create a Defense-owned staging copy of canonical Integration and explicitly rebind Felix's XR instance/managers without writing the shared scene. Keep Power/Clinic readiness locked until authentic owner-approved success contracts exist. Device checks listed above remain NOT RUN.

## Validator diagnosis and corrected build — 20:45 UTC

Package source MetaQuestFeature.cs:537-539 uses EditorUserBuildSettings.selectedBuildTargetGroup instead of the validation targetGroup. Live diagnostic: active/selected Standalone; Standalone OpenXR settings exist but contain no MetaQuestFeature; Android settings contain enabled MetaQuestFeature. Temporarily selecting Android lets the unchanged validator complete with two advisories. The Defense build wrapper now selects Android for the build and restores the selection, active target and exact original PlayerSettings bytes afterward. No vendor edits or validator suppression.

Second BuildReport: Succeeded, **0 errors / 982 warnings**, 89.7166 seconds. Output Builds/DefensePackagingTest/20261025-204348/INCOMPLETE-MP2B-DEMO.apk, 156,807,294 bytes. SHA256 DB4A52644559801D0F1FA93B7DCD5A7A0022C5501B47D5A9D65D39BCA8E710D3. aapt verifies the same test ID, label, ARM64 and API32/36; apksigner verifies v2. Both settings hashes again match the pre-build originals. No runtime/device execution.

Exact first BuildReport warning accounting (1,013):
- 920 inference ConvGeneric unsupported transpose/group variant warnings. Package Resources compiled for the build; no evidence Defense invokes these kernels. Review removing unused inference dependency only with team package ownership, or vendor-supported update after compatibility review; do not patch kernels or suppress warnings locally.
- 58 inference pixel shader warnings: ScatterND23, ConvTranspose12, SliceSet10, GridSample7, ScatterElements4, Pad2. Primarily integer modulus performance advisories. Device performance impact depends on actually using these inference operations.
- 30 CS0618 obsolete FindFirstObjectByType API uses. Maintenance work requires checking uniqueness/ordering semantics before migration; no warning suppression or blanket behavior-changing replacement.
- 3 TextMeshPro IL2CPP notices about large methods placed in separate C++ files: build-time cost, not reported gameplay failures.
- 1 Diagnostics Data/debug-symbol advisory: choose intentional symbol policy for final distribution; do not change telemetry settings incidentally.
- 1 experimental Pipeline package missing RuntimePipelineConfig: that package disables itself in Player. Confirm whether team requires it before removing/configuring it.

The second build retains all 978 inference warnings plus Diagnostics/Pipeline advisories and now correctly reports SSAO mobile performance and optional Prioritize Input Polling advisories (982 total). Missing CS0618/TMP notices in the incremental build do NOT mean those sources were fixed. SSAO/input tuning needs isolated mobile renderer validation and eventual device measurement; shared renderer assets were not modified. Unsupported Simulation shader messages occur elsewhere in Editor.log and are NOT additional entries in the 1,013 BuildReport count.

Evidence: quest-build-diagnostics.txt, quest-build-messages-first.tsv and per-output build-messages.tsv. DEF_QuestBuildDiagnostics.cs provides a read-only report/validator diagnostic with temporary selected-target restoration.

## Local integration staging prepared

Assets/DefenseYard/Scenes/DEF_Integration_Staging.unity starts from the exact canonical Integration c5157dd8f8eb76a4e089b21dc4f06322cf3b3e4d scene (Directional Light only), then combines copies of the local compatible Workshop demo and locked Defense demo. No canonical or source scene was overwritten. DEF_IntegrationStaging.cs refuses to overwrite an already composed staging scene.

The Workshop and Felix customized XR hierarchy are shifted +40m X as a provisional separated layout; Defense keeps its grid origin. The single real XR rig retains Felix wrist hierarchy and action configuration. Removed duplicate desktop camera, simulator, second rig/interaction manager, duplicate EventSystem, Workshop test Defense state and test barricade. Defense VR input is bound to Felix Right Controller, its ray interactor and rig camera. Actual controller behavior remains untested.

One Defense state/progression controller remains. Workshop readiness is bound to fresh workbench craft events and both physical teaching receivers; repairLedger is the same Workshop ledger; physical ammo intake targets actual Defense state. Power and Clinic remain null, so the full prerequisite AND gate stays locked. The staging terminal hides remote ammo and repair buttons while retaining Scrap; original showcase scene is unchanged. Defense UI/presenter use separate provisional world-space panels, not Felix-owned text fields.

Actual composition-time checks: missing scripts0, cross-scene object references0, XR Origins1, Defense states1. Editor utility compiled and scene saved/imported. Evidence integration-staging-results.txt and staging-source-inventory.txt. These checks do not prove every nullable field, UI ray hit, camera activation sequence or spatial layout is correct. Scene was not played, packaged, or device-tested; the verified APK predates staging and contains only DEF_QuestPackaging_Demo.
