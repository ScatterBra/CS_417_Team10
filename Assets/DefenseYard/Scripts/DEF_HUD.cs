using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// All visible widgets are editable children of the saved DEF_Interface prefab.
public class DEF_HUD : MonoBehaviour
{
    public static DEF_HUD Instance { get; private set; }
    public static bool TrainingActive => Instance != null && Instance.training;
    public static bool BlockingWorldInput => Instance != null && (Instance.shopOpen || Instance.guideOpen || Instance.paused || Time.frameCount <= Instance.suppressUntil);
    public bool movementSafeMenus;
    public bool ReadyForWave => trainingStep >= 5;
    public Text healthText, waveText, waveStatus, zombieText, threatText, ammoText, scrapText, contextText, noticeText, selectedText, objectiveText, guideTitle, guideBody, guideIndex, guideNextText, guideSkipText, shopBalance, startText;
    public Image healthFill, selectedPicture, guidePicture;
    public Image[] damageEdges;
    public Sprite[] itemPictures, guidePictures;
    public GameObject shopPanel, guidePanel, selectedPanel, objectivePanel, pausePanel;
    public Button shopButton, guideButton, startButton, pauseButton, resumeButton, shopClose, guideNext, guideSkip, cancelBuild;
    public Button[] buyButtons;
    public Text[] buyLabels;
    bool shopOpen, guideOpen, paused, training = true;
    int trainingStep, guidePage, suppressUntil;
    bool moved, looked, welcome = true;
    float flash, noticeUntil;
    DEF_GameState state;
    public static readonly string[] Names = { "SENTRY", "MISSILE SENTRY", "BARRICADE", "SHOCK TRAP" };
    public static readonly int[] Prices = { 40, 70, 20, 50 };
    static readonly string[] Titles = { "HOLD THE OUTPOST", "YOUR POST. FOUR SIDES.", "CHOOSE. AIM. BUILD.", "KEEP THE DEFENSE RUNNING", "KNOW THE INFECTED", "READ THE YARD", "READY FOR THE FIRST WAVE" };
    static readonly string[] Pages = {
        "The yard is overrun. Stay inside the base and defend every approach.\n\nBuild sentries outside the windows, keep them supplied, and survive escalating waves.\n\nA short hands-on training run will show you how. Select FIELD GUIDE to reopen it anytime.",
        "Use the left thumbstick to move around the base. Turn your head to look around.\n\nYou stay inside the base. Turn to cover all four windows. Point your right controller to aim the ray.",
        "A on your right controller opens the build shop. Select a pictured item to equip it, then aim your ray at the ground outside.\n\nA green preview means the position is valid. Squeeze the right trigger to place it. Red means blocked or too expensive. Select the close button to cancel for free. Scrap is spent only after a successful placement.\n\nSentries fire automatically. Missile sentries use the twin launcher pods to damage groups. Barricades force detours or absorb attacks. Shock traps stun nearby zombies for 2 seconds, then recharge for 7 seconds. They use no ammo.",
        "All sentries share the ammo counter. Aim at a SUPPLY CRATE and squeeze the right grip to collect 10 ammo. Empty crates stay put and refill after 20 seconds. Clearing a wave restocks all three. The refill countdown floats above each crate.\n\nDamaged equipment becomes dark and scorched. Aim at it to see integrity and repair advice. Aim and squeeze the right grip to repair 40 health for 10 scrap.\n\nZombie kills earn scrap. Every cleared wave awards 30 scrap and up to 15 ammo.",
        "WALKER  /  Wave 1+\nSlow approach. 50 health, 1.15 m/s.\n\nRUNNER  /  Wave 2+\nSmaller and quicker, but fragile. 30 health, 1.75 m/s.\n\nJUGGERNAUT  /  Wave 3+\nHuge mutant with heavy arms. 130 health, 0.7 m/s.\n\nFloating health bars show each zombie's remaining health. Health rises 10% each wave.",
        "Red screen edges and a direction alert mean the BASE IS BEING HIT. Turn toward that window and reinforce it.\n\nAim at an object to see its health, supply, or reload time. Moving strips show sentry reloads; the radio signal becomes more distorted as threat level rises.\n\nHigher threat level adds zombies. Stragglers may appear between waves after training.",
        "Spread your defenses around the base instead of putting everything at one window. Sentries prioritize visible zombies attacking the base and can turn a full 360 degrees.\n\nPress the left controller Menu button or select START WAVE when you are ready. The next wave waits for you.\n\nA: build shop   Right trigger: place\nRight grip: collect / repair   Left Menu: start wave"
    };
    void Awake() { Instance = this; }
    void Start()
    {
        state = DEF_GameState.I;
        if (state != null) state.BaseHit += OnBaseHit;
        shopButton.onClick.AddListener(ToggleShop); shopClose.onClick.AddListener(CloseMenus);
        guideButton.onClick.AddListener(OpenGuide); pauseButton.onClick.AddListener(TogglePause); resumeButton.onClick.AddListener(TogglePause);
        startButton.onClick.AddListener(() => DEF_WaveManager.I?.StartWave());
        cancelBuild.onClick.AddListener(() => DEF_VRInput.Instance?.CancelBuild());
        for (int i = 0; i < buyButtons.Length; i++) { int index = i; buyButtons[i].onClick.AddListener(() => Buy(index)); }
        guideNext.onClick.AddListener(NextGuide); guideSkip.onClick.AddListener(SkipGuide);
        guideOpen = true; guidePage = 0;
        if (DEF_Progression.Current != null) { training = false; welcome = false; guideOpen = false; guideButton.gameObject.SetActive(false); }
        SyncModal(); RefreshGuide();
    }
    void OnDestroy()
    {
        if (state != null) state.BaseHit -= OnBaseHit;
        if (Instance == this) { Instance = null; Time.timeScale = 1; }
    }
    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.bKey.wasPressedThisFrame) ToggleShop();
            if (kb.hKey.wasPressedThisFrame) OpenGuide();
            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (shopOpen || guideOpen) { if (welcome) SkipGuide(); else CloseMenus(); }
                else if (DEF_VRInput.Instance != null && DEF_VRInput.Instance.BuildArmed) DEF_VRInput.Instance.CancelBuild();
                else TogglePause();
            }
            if (training && trainingStep == 0 && !BlockingWorldInput)
            {
                moved |= kb.wKey.isPressed || kb.aKey.isPressed || kb.sKey.isPressed || kb.dKey.isPressed;
                looked |= Mouse.current != null && Mouse.current.rightButton.isPressed && Mouse.current.delta.ReadValue().sqrMagnitude > 0.1f;
                if (moved && looked) { trainingStep = 1; Notify("Open the shop with A and choose your first defense."); }
            }
        }
        if (state == null) return;
        healthText.text = Mathf.CeilToInt(state.playerHealth) + " <size=16>/ " + state.maxHealth + "</size>";
        healthFill.fillAmount = state.playerHealth / state.maxHealth;
        healthFill.color = state.playerHealth < state.maxHealth * 0.3f ? new Color(1,0.32f,0.25f) : new Color(0.32f,0.85f,0.7f);
        ammoText.text = state.ammo + " <size=16>/ " + state.maxAmmo + "</size>"; scrapText.text = state.scrap.ToString();
        var wave = DEF_WaveManager.I;
        waveText.text = "WAVE " + state.waveNumber.ToString("00");
        bool ready = wave != null && (wave.phase == DEF_WaveManager.Phase.Idle || wave.phase == DEF_WaveManager.Phase.Cleared);
        waveStatus.text = state.playerHealth <= 0 ? "BASE BREACHED" : ready ? "PREPARE YOUR DEFENSE" : "DEFEND ALL FOUR SIDES";
        zombieText.text = DEF_Zombie.all.Count + "  ZOMBIES";
        threatText.text = "THREAT LEVEL  " + (wave != null ? Mathf.FloorToInt(wave.threat) : 0);
        startButton.interactable = ready && (!training || ReadyForWave) && !shopOpen && !guideOpen;
        startText.text = (state.playerHealth <= 0 || (wave != null && wave.AttemptFailed)) ? (DEF_Progression.Current!=null && DEF_Progression.Current.workshopRecording ? "RESTART RECORDING RUN [MENU]" : "RETRY DEFENSE  [MENU]") : ready ? "START WAVE  [MENU]" : "WAVE IN PROGRESS";
        shopBalance.text = state.scrap + " SCRAP AVAILABLE";
        for (int i = 0; i < buyButtons.Length; i++)
        {
            var progression = DEF_Progression.Current;
            if (progression != null) buyButtons[i].gameObject.SetActive(progression.IsAllowed(i == 0 ? DEF_Action.BasicSentry : i == 1 ? DEF_Action.Missile : i == 2 ? DEF_Action.Barricade : DEF_Action.Shock));
            buyButtons[i].interactable = progression != null || state.scrap >= Prices[i];
            buyLabels[i].text = state.scrap >= Prices[i] ? Prices[i] + " SCRAP" : "NEED " + (Prices[i] - state.scrap) + " MORE SCRAP";
        }
        var gates = DEF_Progression.Current;
        if (gates != null)
        {
            SetResourceVisibility(ammoText, "Ammo label", gates.IsAllowed(DEF_Action.PhysicalAmmo));
            ammoText.gameObject.SetActive(gates.IsAllowed(DEF_Action.PhysicalAmmo));
            SetResourceVisibility(scrapText, "Scrap label", gates.IsAllowed(DEF_Action.BasicSentry));
            ammoText.transform.parent.gameObject.SetActive(gates.IsAllowed(DEF_Action.PhysicalAmmo) || gates.IsAllowed(DEF_Action.BasicSentry));
            scrapText.gameObject.SetActive(gates.IsAllowed(DEF_Action.BasicSentry));
            shopButton.gameObject.SetActive(gates.IsAllowed(DEF_Action.BasicSentry));
            startButton.gameObject.SetActive(gates.IsAllowed(DEF_Action.StartWave) || gates.IsAllowed(DEF_Action.Combat));
            startButton.interactable = !shopOpen && !guideOpen;
            bool waveVisible=gates.IsAllowed(DEF_Action.StartWave)||gates.IsAllowed(DEF_Action.Combat);
            waveText.gameObject.SetActive(waveVisible);waveStatus.gameObject.SetActive(waveVisible);
            zombieText.gameObject.SetActive(waveVisible);threatText.gameObject.SetActive(waveVisible);
            if(!gates.IsAllowed(DEF_Action.BasicSentry) && shopOpen)CloseMenus();
        }
        var input = DEF_VRInput.Instance;
        bool selected = input != null && input.BuildArmed && !BlockingWorldInput;
        selectedPanel.SetActive(selected);
        if (selected) { int i = (int)input.mode; selectedText.text = Names[i] + "  /  " + Prices[i] + " SCRAP\n<size=17>Right trigger: place  ·  Close button: cancel</size>"; selectedPicture.sprite = itemPictures[i]; }
        contextText.text = BlockingWorldInput || (gates != null && !gates.IsAllowed(DEF_Action.Repair)) ? "" : TargetReadout();
        objectivePanel.SetActive(training && !guideOpen && !shopOpen);
        objectiveText.text = trainingStep == 0 ? "01 / GET YOUR BEARINGS\n<size=18>" + (moved ? "OK" : "•") + " Left stick: move   " + (looked ? "OK" : "•") + " Turn your head: look</size>" :
            trainingStep == 1 ? "02 / BUILD YOUR FIRST DEFENSE\n<size=18>A: shop → select an item → aim outside → right trigger</size>" :
            trainingStep == 2 ? "03 / REPAIR YOUR DEFENSE\n<size=18>Training damage applied. Aim at your defense + right grip. Repair cost covered.</size>" :
            trainingStep == 3 ? "04 / COLLECT SUPPLIES\n<size=18>10 ammo set aside for training. Aim at a supply crate + right grip.</size>" :
            "05 / FORTIFY ALL FOUR SIDES\n<size=18>A: build shop. Press left Menu when ready to begin.</size>";
        if (Time.unscaledTime > noticeUntil) noticeText.text = state.playerHealth <= 0 ? "BASE BREACHED  /  Press left Menu to restart" : "";
        flash = Mathf.MoveTowards(flash, 0, Time.unscaledDeltaTime * 1.1f);
        foreach (var edge in damageEdges) edge.color = new Color(0.9f,0.07f,0.04f,flash * 0.55f);
    }
    public static void SetResourceVisibility(Text value, string labelName, bool visible)
    {
        if(value==null)return;
        value.gameObject.SetActive(visible);
        var label=value.transform.parent.Find(labelName);if(label!=null)label.gameObject.SetActive(visible);
    }
    public void ToggleShop()
    {
        if (!DEF_Progression.Allows(DEF_Action.BasicSentry)) return;
        if (welcome || paused) return;
        if (guideOpen && trainingStep == 4) trainingStep = 5;
        shopOpen = !shopOpen; guideOpen = false; DEF_VRInput.Instance?.CancelBuild(); SyncModal();
    }
    public void Buy(int index)
    {
        if (index < 0 || index >= Prices.Length) return;
        var action = index == 0 ? DEF_Action.BasicSentry : index == 1 ? DEF_Action.Missile : index == 2 ? DEF_Action.Barricade : DEF_Action.Shock;
        if (!DEF_Progression.Allows(action)) return;
        if (state == null) return;
        if (state.scrap < Prices[index]) { DEF_Progression.Current?.Reject("select defense", "there is not enough scrap", "Transfer Workshop scrap, then select this defense again."); return; }
        CloseMenus(); DEF_VRInput.Instance?.SelectBuild((DEF_VRInput.PlaceMode)index);
        Notify("Aim outside the base. Right trigger places your " + Names[index].ToLowerInvariant() + ".");
    }
    public void OpenGuide() { if (DEF_Progression.Current != null) return; welcome = false; guidePage = 1; guideOpen = true; shopOpen = false; SyncModal(); RefreshGuide(); }
    void NextGuide()
    {
        if (welcome) { welcome = false; training = true; trainingStep = 0; CloseMenus(); return; }
        if (++guidePage >= Pages.Length) { if (training && trainingStep == 4) trainingStep = 5; CloseMenus(); }
        else RefreshGuide();
    }
    void SkipGuide() { if (welcome) FinishTraining(); else if (training && trainingStep == 4) trainingStep = 5; welcome = false; CloseMenus(); }
    void RefreshGuide()
    {
        guideTitle.text = Titles[guidePage]; guideBody.text = Pages[guidePage]; guideIndex.text = "FIELD GUIDE  /  " + (guidePage + 1).ToString("00") + " OF 07";
        guideNextText.text = welcome ? "START TRAINING" : guidePage == Pages.Length - 1 ? "RETURN TO YARD" : "NEXT";
        guideSkipText.text = welcome ? "SKIP TRAINING" : "CLOSE GUIDE";
        guidePicture.sprite = guidePictures[Mathf.Min(guidePage, guidePictures.Length - 1)];
    }
    public void CloseMenus() { if (guideOpen && trainingStep == 4) trainingStep = 5; shopOpen = guideOpen = false; suppressUntil = Time.frameCount + 2; SyncModal(); }
    void TogglePause() { if (shopOpen || guideOpen) return; paused = !paused; suppressUntil = Time.frameCount + 2; SyncModal(); }
    void SyncModal() { shopPanel.SetActive(shopOpen); guidePanel.SetActive(guideOpen); pausePanel.SetActive(paused); Time.timeScale = movementSafeMenus ? 1 : (shopOpen || guideOpen || paused ? 0 : 1); }
    public void FinishTraining() { if (!training) return; training = false; Notify("Training complete. Defend the outpost."); }
    public void RecordPlacement(GameObject defense)
    {
        if (DEF_Progression.Current != null || !training || trainingStep != 1) return;
        trainingStep = 2; state.scrap = Mathf.Min(state.maxScrap, state.scrap + 10);
        var turret = defense.GetComponent<DEF_Turret>(); var bar = defense.GetComponent<DEF_Barricade>();
        if (turret != null) turret.TakeDamage(20); if (bar != null) bar.TakeDamage(20);
        defense.GetComponent<DEF_ShockTrap>()?.TakeDamage(20);
        DEF_Tutorials.Show("training-repair", defense.transform, "TRAINING REPAIR\nAim here and squeeze the right grip. The 10 scrap cost is covered.");
    }
    public void RecordRepair()
    {
        if (!training || trainingStep != 2) return;
        DEF_Tutorials.Dismiss("training-repair");
        trainingStep = 3; state.ammo = Mathf.Min(state.ammo, state.maxAmmo - 10);
        Notify("Now aim at a supply crate and squeeze the right grip to refill your ammo.");
        var crate = FindFirstObjectByType<DEF_AmmoCrate>(); if (crate != null) DEF_Tutorials.Show("training-crate", crate.transform, "TRAINING SUPPLIES\nAim at a crate and squeeze the right grip to collect ammo.");
    }
    public void RecordHarvest()
    {
        if (!training || trainingStep != 3) return;
        DEF_Tutorials.Dismiss("training-crate");
        trainingStep = 4; guidePage = 4; guideOpen = true; SyncModal(); RefreshGuide();
    }
    public void Notify(string message) { noticeText.text = message; noticeUntil = Time.unscaledTime + 5f; }
    void OnBaseHit(Vector3 source, float damage)
    {
        flash = 1;
        Vector3 offset = source - (DEF_BaseStructure.Instance != null ? DEF_BaseStructure.Instance.transform.position : Vector3.zero);
        string direction = Mathf.Abs(offset.x) > Mathf.Abs(offset.z) ? offset.x > 0 ? "EAST" : "WEST" : offset.z > 0 ? "NORTH" : "SOUTH";
        Notify("BASE HIT  /  " + direction + " SIDE  /  −" + Mathf.CeilToInt(damage) + " HEALTH");
    }
    static string DamageAdvice(float ratio) => ratio <= .34f ? "  /  CRITICAL" : ratio < .8f ? "  /  DAMAGED" : "";
    string TargetReadout()
    {
        var gates = DEF_Progression.Current;
        if (gates != null)
        {
            SetResourceVisibility(ammoText, "Ammo label", gates.IsAllowed(DEF_Action.PhysicalAmmo));
            ammoText.gameObject.SetActive(gates.IsAllowed(DEF_Action.PhysicalAmmo));
            SetResourceVisibility(scrapText, "Scrap label", gates.IsAllowed(DEF_Action.BasicSentry));
            ammoText.transform.parent.gameObject.SetActive(gates.IsAllowed(DEF_Action.PhysicalAmmo) || gates.IsAllowed(DEF_Action.BasicSentry));
            scrapText.gameObject.SetActive(gates.IsAllowed(DEF_Action.BasicSentry));
            shopButton.gameObject.SetActive(gates.IsAllowed(DEF_Action.BasicSentry));
            startButton.gameObject.SetActive(gates.IsAllowed(DEF_Action.StartWave) || gates.IsAllowed(DEF_Action.Combat));
            startButton.interactable = !shopOpen && !guideOpen;
            bool waveVisible=gates.IsAllowed(DEF_Action.StartWave)||gates.IsAllowed(DEF_Action.Combat);
            waveText.gameObject.SetActive(waveVisible);waveStatus.gameObject.SetActive(waveVisible);
            zombieText.gameObject.SetActive(waveVisible);threatText.gameObject.SetActive(waveVisible);
            if(!gates.IsAllowed(DEF_Action.BasicSentry) && shopOpen)CloseMenus();
        }
        var input = DEF_VRInput.Instance;
        if (input == null || !input.TryGetAimHit(out var hit)) return "";
        var turret = hit.collider.GetComponentInParent<DEF_Turret>();
        if (turret != null) return (turret.explosive ? "MISSILE SENTRY" : "SENTRY") + "  ·  " + Mathf.CeilToInt(turret.health) + "/" + turret.maxHealth + " HP" + DamageAdvice(turret.health / turret.maxHealth) + "  ·  Reload " + turret.ShotCooldownRemaining.ToString("0.0") + "s  ·  " + DEF_Tutorials.RepairAdvice;
        var bar = hit.collider.GetComponentInParent<DEF_Barricade>();
        if (bar != null) return "BARRICADE  ·  " + Mathf.CeilToInt(bar.health) + "/" + bar.maxHealth + " HP" + DamageAdvice(bar.health / bar.maxHealth) + "  ·  " + DEF_Tutorials.RepairAdvice;
        var trap = hit.collider.GetComponentInParent<DEF_ShockTrap>();
        if (trap != null) return "SHOCK TRAP  ·  " + Mathf.CeilToInt(trap.health) + "/" + trap.maxHealth + " HP" + DamageAdvice(trap.health / trap.maxHealth) + "  ·  " + (trap.RechargeRemaining > 0 ? "Recharging " + trap.RechargeRemaining.ToString("0.0") + "s" : "READY") + "  ·  " + DEF_Tutorials.RepairAdvice;
        var crate = hit.collider.GetComponentInParent<DEF_AmmoCrate>();
        if (crate != null) return crate.IsRefilling ? "SUPPLY CRATE  ·  REFILLING IN " + Mathf.CeilToInt(crate.RefillRemaining) + "s" : "SUPPLY CRATE  ·  " + crate.supply + " ammo remaining  ·  Right grip: collect 10";
        var zombie = hit.collider.GetComponentInParent<DEF_Zombie>();
        if (zombie != null) return zombie.DisplayName.ToUpperInvariant() + "  ·  " + Mathf.CeilToInt(zombie.health) + "/" + Mathf.CeilToInt(zombie.maxHealth) + " HP  ·  Next strike " + zombie.AttackCooldownRemaining.ToString("0.0") + "s";
        return "";
    }
}


