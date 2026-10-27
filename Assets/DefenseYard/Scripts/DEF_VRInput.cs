using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

// Both real controllers and XRI's simulator feed the Input System actions below.
// Pose tracking is handled by the rig's Input System Tracked Pose Drivers.
public class DEF_VRInput : MonoBehaviour
{
    public enum PlaceMode { Turret, BoomTurret, Barricade, ShockTrap }

    public bool simulateInEditor;
    public Transform rayOrigin;
    public XRRayInteractor rayInteractor;
    public NearFarInteractor nearFarInteractor;
    readonly System.Collections.Generic.List<Collider> aimColliders = new();
    readonly System.Collections.Generic.List<RaycastHit> aimHits = new();
    public Camera xrCamera;
    public GameObject desktopRig;
    public float rayLength = 100f;
    public PlaceMode mode = PlaceMode.Turret;
    public static bool Active { get; private set; }
    public static DEF_VRInput Instance { get; private set; }

    public bool BuildArmed { get; private set; }
    GameObject ghost;
    Material ghostMaterial;
    public void SelectBuild(PlaceMode selected)
    {
        if (!DEF_Progression.Allows(selected == PlaceMode.Turret ? DEF_Action.BasicSentry : selected == PlaceMode.BoomTurret ? DEF_Action.Missile : selected == PlaceMode.Barricade ? DEF_Action.Barricade : DEF_Action.Shock)) return;
        CancelBuild(); mode = selected; BuildArmed = true;
        // The shop returns the PC simulator to the right hand that places defenses.
        // Advanced simulator shortcuts can otherwise leave that hand's pose frozen.
        // Keep physical headset/controller input untouched.
        if (Application.isEditor && simulateInEditor)
        {
            var simulator = FindAnyObjectByType<XRInteractionSimulator>();
            if (simulator != null && simulator.isActiveAndEnabled)
            {
                simulator.targetedDeviceInput = TargetedDevices.FPS | TargetedDevices.RightDevice;
                simulator.usePointAndClick = true;
            }
        }
        var prefab = Resources.Load<GameObject>(selected == PlaceMode.ShockTrap ? "Prefabs/DEF_ShockTrap" : selected == PlaceMode.Barricade ? "Prefabs/DEF_Barricade" : selected == PlaceMode.BoomTurret ? "Prefabs/DEF_BlastTurret" : "Prefabs/DEF_Turret");
        if (prefab == null) return;
        ghost = Instantiate(prefab); ghost.name = "Placement preview";
        foreach (var b in ghost.GetComponentsInChildren<MonoBehaviour>()) b.enabled = false;
        foreach (var a in ghost.GetComponentsInChildren<Animator>()) a.enabled = false;
        foreach (var c in ghost.GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (var r in ghost.GetComponentsInChildren<Rigidbody>()) r.detectCollisions = false;
        ghostMaterial = new Material(Resources.Load<Material>("UI/PlacementGhost"));
        foreach (var t in ghost.GetComponentsInChildren<Transform>()) t.gameObject.layer = 2;
        foreach (var r in ghost.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = ghostMaterial;
            r.sharedMaterials = mats; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (r.GetComponent<DEF_Effects>() != null || r.GetComponent<DEF_UVSurface>() != null) r.enabled = false;
        }
        ghost.SetActive(false);
    }
    public void CancelBuild()
    {
        BuildArmed = false;
        if (ghost != null) Destroy(ghost);
        if (ghostMaterial != null) Destroy(ghostMaterial);
    }
    void PreviewBuild(bool hasGround, Vector3 point)
    {
        if (ghost == null || grid == null) return;
        bool show = BuildArmed && hasGround && grid.WorldToTile(point, out _, out _);
        ghost.SetActive(show); if (!show) return;
        grid.WorldToTile(point, out int x, out int z);
        var center = grid.TileCenter(x,z); ghost.transform.position = center;
        if (mode == PlaceMode.Barricade)
        {
            var bc = DEF_Pathfinder.I.BaseCell(); var outward = center - grid.TileCenter(bc.x,bc.y);
            ghost.transform.rotation = Quaternion.Euler(0, Mathf.Abs(outward.x) > Mathf.Abs(outward.z) ? 90 : 0, 0);
        }
        bool valid = !DEF_Pathfinder.I.IsBlocked(x,z) && DEF_Pathfinder.I.OccupantAt(x,z) == null &&
            !DEF_BaseStructure.Instance.Contains(center,0.5f) && DEF_GameState.I.scrap >= DEF_HUD.Prices[(int)mode];
        ghostMaterial.SetColor("_BaseColor", valid ? new Color(0.15f,1f,0.65f,0.45f) : new Color(1f,0.15f,0.08f,0.45f));
    }

    InputActionMap actions;
    InputAction trigger, grip, primary, menu;
    LineRenderer rayLine;
    DEF_GridManager grid;
    DEF_TileSelector selector;
    Camera desktopCamera;
    AudioListener xrListener;
    bool previousTrigger, previousGrip, previousPrimary, previousMenu;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Active = false; Instance = null; }

    void Awake()
    {
        Instance = this;
        Active = (Application.isEditor && simulateInEditor) || XRSettings.isDeviceActive;
        actions = new InputActionMap("Defense Yard XR");
        trigger = Button("Place", "RightHand", "triggerButton", "triggerPressed");
        grip = Button("Harvest or Repair", "RightHand", "gripButton", "gripPressed");
        primary = Button("Build Mode", "RightHand", "primaryButton");
        menu = Button("Start Wave", "LeftHand", "menuButton");
        menu.AddBinding("<XRController>{RightHand}/menuButton");
    }

    InputAction Button(string name, string hand, params string[] controls)
    {
        var action = actions.AddAction(name, InputActionType.Button);
        foreach (string control in controls)
            action.AddBinding("<XRController>{" + hand + "}/" + control);
        return action;
    }

    void OnEnable() { actions?.Enable(); }
    void OnDisable()
    {
        actions?.Disable();
        if (Instance == this) { Active = false; Instance = null; }
    }
    void OnDestroy() { CancelBuild(); actions?.Dispose(); }

    void Start()
    {
        grid = FindFirstObjectByType<DEF_GridManager>();
        selector = FindFirstObjectByType<DEF_TileSelector>();
        // Near/Far owns its visual curve; never manipulate the teleport line.
        if (nearFarInteractor == null && rayOrigin != null) rayLine = rayOrigin.GetComponent<LineRenderer>();
        if (desktopRig != null) desktopCamera = desktopRig.GetComponentInChildren<Camera>(true);
        if (xrCamera != null) xrListener = xrCamera.GetComponent<AudioListener>();
        SetCameraMode();
    }

    void SetCameraMode()
    {
        if (desktopCamera != null) desktopCamera.tag = Active ? "Untagged" : "MainCamera";
        if (desktopRig != null) desktopRig.SetActive(!Active);
        if (xrCamera != null)
        {
            xrCamera.enabled = Active;
            xrCamera.tag = Active ? "MainCamera" : "Untagged";
        }
        if (xrListener != null) xrListener.enabled = Active;
        if (rayLine != null) rayLine.enabled = Active;
    }

    void LateUpdate()
    {
        bool active = (Application.isEditor && simulateInEditor) || XRSettings.isDeviceActive;
        if (active != Active) { Active = active; SetCameraMode(); }
        if (BuildArmed && DEF_Progression.Current != null && !DEF_Progression.Current.IsAllowed(mode == PlaceMode.Turret ? DEF_Action.BasicSentry : mode == PlaceMode.BoomTurret ? DEF_Action.Missile : mode == PlaceMode.Barricade ? DEF_Action.Barricade : DEF_Action.Shock)) CancelBuild();
        if (!Active) return;

        if (DEF_HUD.BlockingWorldInput)
        {
            previousTrigger = trigger.IsPressed(); previousGrip = grip.IsPressed(); previousPrimary = primary.IsPressed(); previousMenu = menu.IsPressed();
            selector?.Preview(Vector3.zero, false); PreviewBuild(false, Vector3.zero);
            if (rayLine != null) rayLine.enabled = false;
            return;
        }
        if (rayLine != null) rayLine.enabled = true;
        bool hasHit = TryGetAimHit(out RaycastHit hit);
        bool groundHit = hasHit && grid != null && hit.collider == grid.groundCollider;
        PreviewBuild(groundHit, hasHit ? hit.point : Vector3.zero);
        if (selector != null) selector.Preview(hasHit ? hit.point : Vector3.zero,
            BuildArmed && groundHit);
        if (rayLine != null && rayOrigin != null)
        {
            rayLine.SetPosition(0, rayOrigin.position + rayOrigin.forward * 0.055f);
            rayLine.SetPosition(1, hasHit ? hit.point : rayOrigin.position + rayOrigin.forward * rayLength);
            rayLine.startColor = rayLine.endColor = hasHit ? Color.cyan : Color.gray;
        }

        if (hasHit)
        {
            var aimedCrate = hit.collider.GetComponentInParent<DEF_AmmoCrate>();
            if (aimedCrate != null) DEF_Tutorials.Show("harvest", aimedCrate.transform, "SUPPLY CRATE\nRight grip collects 10 ammo.\nRefills in 20s or on wave clear.");
            var radio = hit.collider.GetComponent<DEF_UVSurface>();
            if (radio != null && radio.signal == DEF_UVSurface.Signal.Radio)
                DEF_Tutorials.Show("radio", radio.transform.position - Vector3.up * 1.2f, "RADIO MONITOR\nA moving signal tracks the yard. Interference rises with threat.");
        }
        bool pressedTrigger = trigger.IsPressed();
        bool pressedGrip = grip.IsPressed();
        bool pressedPrimary = primary.IsPressed();
        bool pressedMenu = menu.IsPressed();
        if (pressedPrimary && !previousPrimary) DEF_HUD.Instance?.ToggleShop();
        if (!DEF_HUD.BlockingWorldInput && BuildArmed && pressedTrigger && !previousTrigger && groundHit && grid != null &&
            grid.WorldToTile(hit.point, out int x, out int z))
        {
            if (selector != null) selector.SelectTile(x, z);
            bool placed = mode == PlaceMode.ShockTrap ? DEF_ShockTrap.TryPlace(x,z) : mode == PlaceMode.Barricade ? DEF_Barricade.TryPlace(x,z) : DEF_Turret.TryPlace(x,z,mode == PlaceMode.BoomTurret);
            if (placed) { DEF_HUD.Instance?.RecordPlacement(DEF_Pathfinder.I.OccupantAt(x,z)); CancelBuild(); }
            else DEF_HUD.Instance?.Notify(DEF_Progression.Current != null ? DEF_Progression.Current.LastFailure : "Cannot build here. Aim at clear ground outside the base and check your scrap.");
        }
        if (!DEF_HUD.BlockingWorldInput && pressedGrip && !previousGrip && hasHit)
        {
            var crate = hit.collider.GetComponentInParent<DEF_AmmoCrate>();
            var barricade = hit.collider.GetComponentInParent<DEF_Barricade>();
            if (crate != null) crate.Harvest(10);
            else if (barricade != null) barricade.TryRepair();
            else if (hit.collider.GetComponentInParent<DEF_ShockTrap>() is DEF_ShockTrap trap) trap.TryRepair();
            else { var turret = hit.collider.GetComponentInParent<DEF_Turret>(); if (turret != null) turret.TryRepair(); }
        }
        var waves = DEF_WaveManager.I;
        if (pressedMenu && !previousMenu && waves != null)
            waves.StartWave();

        previousTrigger = pressedTrigger;
        previousGrip = pressedGrip;
        previousPrimary = pressedPrimary;
        previousMenu = pressedMenu;
    }

    public bool TryGetAimHit(out RaycastHit hit)
    {
        hit = default;
        if (nearFarInteractor != null)
        {
            if (!nearFarInteractor.isActiveAndEnabled || !nearFarInteractor.enableFarCasting ||
                nearFarInteractor.hasSelection || nearFarInteractor.farInteractionCaster == null ||
                nearFarInteractor.TryGetCurrentUIRaycastResult(out _)) return false;
            aimColliders.Clear(); aimHits.Clear();
            if (!nearFarInteractor.farInteractionCaster.TryGetColliderTargets(
                nearFarInteractor.interactionManager, aimColliders, aimHits) || aimHits.Count == 0) return false;
            hit = aimHits[0]; return hit.collider != null;
        }
        // XRI caches its hit until the next interaction update. A harvested crate or
        // defeated enemy may already be destroyed when the HUD reads that cached hit.
        return rayInteractor != null && rayInteractor.TryGetCurrent3DRaycastHit(out hit) && hit.collider != null;
    }
}
