using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
// Scene-owned ordered Clinic exercise. Legacy scenes without this component remain unchanged.
public sealed class DEF_ClinicTraining : MonoBehaviour
{
    public MED_WildSpawner wildSpawner;
    public MED_PlayerResources patient;
    public MED_Recipe medicineRecipe;
    public MED_BuildingDefinition benchDefinition;
    public Transform buildingsRoot;
    public TextMesh directions;
    public int Step {get;private set;}=7;
    // One presentation source for the world sign and the existing head-follow Clinic HUD.
    public string CurrentInstruction { get; private set; } = "Read the Clinic introduction.";
    public bool IsIntroducing => pendingStep != 0;
    public int InstructionStep => pendingStep != 0 ? pendingStep : Step;
    // Compact status is presentation only; the full instruction and all gates remain authoritative.
    public string CompactInstruction
    {
        get
        {
            if (IsIntroducing) return "Read the current self-treatment instruction, then continue.";
            if (Step == 7)
            {
                if (!harvested.Contains(MED_ResourceType.Herbs)) return "Harvest Herbs: grip Sickle, touch a herb patch and swing.";
                if (!harvested.Contains(MED_ResourceType.Water)) return "Harvest Water: grip Bucket, touch the rain barrel.";
                return "Harvest supplies: grip Crowbar, touch a supply crate and swing.";
            }
            if (Step == 8) return "Random harvest: " + (randomFound ? "DONE" : "NEEDED") +
                " | Cart: " + (unloaded ? "DONE" : "NEEDED") + "\n" +
                (!randomFound ? "Harvest the marked resource in the build grid." : "Grip UNLOAD once: cart → your Clinic supplies. No loading.");
            if (Step == 9) return placed ? CraftInstruction(1) : "Grip PrepBench model; aim at green grid and release.";
            var readiness = GetComponent<DEF_ClinicReadiness>();
            if (patient != null && patient.IsHealthFull) return readiness != null ? readiness.PendingReason : "Check Clinic readiness.";
            var station = readiness != null ? readiness.station : null;
            if (station != null && station.State == MED_TreatmentStation.StationState.Treating) return "Treatment running: stay on the green pad until this dose finishes.";
            int needed = patient != null ? Mathf.CeilToInt((patient.MaxHealth - patient.Health) / 20f) : 3;
            int stored = station != null ? Mathf.FloorToInt(station.StoredMedicine.Value) : 0;
            if (patient != null && patient.Get(MED_ResourceType.Medicine) + stored < needed) return CraftInstruction(needed - stored);
            if (station == null) return "Place TreatmentStation on green grid to heal yourself.";
            if (stored < needed) return "Grip REFILL to move your Medicine into the station. Stock: " + stored + "/" + needed + " doses needed.";
            return station.PlayerOnPad ? "Grip TREAT once to heal yourself; stay on the green pad." : "Stand on the station's green pad, then grip TREAT once.";
        }
    }

    string CraftInstruction(int needed)
    {
        var readiness = GetComponent<DEF_ClinicReadiness>();
        if (readiness != null && readiness.bench != null && !readiness.bench.IsIdle)
            return "Medicine crafting — wait for the bench to finish.";
        int herbs = patient != null ? patient.Get(MED_ResourceType.Herbs) : 0;
        int water = patient != null ? patient.Get(MED_ResourceType.Water) : 0;
        int medicine = patient != null ? patient.Get(MED_ResourceType.Medicine) : 0;
        if (herbs < 2) return "Gather " + (2 - herbs) + " more Herbs with Sickle for the next Medicine.";
        if (water < 1) return "Gather 1 Water with Bucket for the next Medicine.";
        return "Grip Medicine at PrepBench (2 Herbs + 1 Water). Inventory: " + medicine + "/" + needed + " needed.";
    }

    // Context hints are not gameplay gates. Failure messages use the HUD separately.
    public bool IsTutorialRelevant(string id)
    {
        if (IsIntroducing) return false;
        if (id == "hud" || id == "statbars") return true;
        if (Step == 7) return id == "toolrack" || id == "herbpatch" || id == "barrel" || id == "crate";
        if (Step == 8) return (id == "cart" && !unloaded) || (!randomFound && (id == "herbpatch" || id == "crate"));
        if (Step == 9) return id == "shelf" || id == "grid" || id == "bench" || id == "herbpatch" || id == "barrel";
        return id == "station" || id == "bench" || id == "shelf" || id == "grid" || id == "herbpatch" || id == "barrel";
    }

    void Awake()
    {
        // Include nested prefab instances, not only top-level scene objects.
        foreach (var button in FindObjectsByType<MED_TestButtonBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (button.gameObject.scene == gameObject.scene) button.HideInIntegratedScene();
    }

    void SetDirections(string text)
    {
        CurrentInstruction = text;
        if (directions != null && directions.text != text) directions.text = text;
    }
    readonly HashSet<MED_ResourceType> harvested=new();
    bool randomFound,unloaded,placed,subscribed;
    MED_Harvestable learningTarget;
    TextMesh learningMarker;
    void UpdateLearningMarker()
    {
        if (Step != 8 || pendingStep != 0 || randomFound || MED_AreaGate.Outside)
        {
            if (learningMarker != null) learningMarker.gameObject.SetActive(false);
            return;
        }
        if (learningTarget == null || !learningTarget.isActiveAndEnabled || learningTarget.IsEmpty)
        {
            learningTarget = null;
            foreach (var marker in FindObjectsByType<DEF_ClinicRandomSource>(FindObjectsSortMode.None))
            {
                var source = marker.GetComponent<MED_Harvestable>();
                if (marker.Owner == wildSpawner && source != null && source.isActiveAndEnabled && !source.IsEmpty)
                { learningTarget = source; break; }
            }
        }
        if (learningTarget == null) return;
        if (learningMarker == null)
        {
            var go = new GameObject("Clinic current harvest target");
            go.transform.SetParent(transform, false);
            learningMarker = go.AddComponent<TextMesh>();
            learningMarker.fontSize = 48; learningMarker.characterSize = .025f;
            learningMarker.anchor = TextAnchor.MiddleCenter; learningMarker.alignment = TextAlignment.Center;
            learningMarker.color = Color.white;
        }
        learningMarker.gameObject.SetActive(true);
        learningMarker.text = "HARVEST THIS\n" + learningTarget.RequiredTool;
        learningMarker.transform.position = learningTarget.transform.position + Vector3.up * 1.1f;
        if (Camera.main != null) learningMarker.transform.rotation = Camera.main.transform.rotation;
    }
    float revealUntil;
    int pendingStep=7;
    void Start(){if(GetComponent<DEF_ClinicFieldAid>()==null)gameObject.AddComponent<DEF_ClinicFieldAid>();if(GetComponent<DEF_ClinicMedicineDelivery>()==null)gameObject.AddComponent<DEF_ClinicMedicineDelivery>();Reveal(7,"Gather supplies, craft Medicine, then heal yourself from 50 to 100. This verifies the Clinic requirement for Defense. Start with Sickle for Herbs, Bucket for Water, and Crowbar for supplies.");}
    public bool CanReveal(MED_BuildingDefinition definition)=>pendingStep==0&&Step>=(definition==benchDefinition?9:10);
    public bool ProtectLearningCart=>!unloaded;
    public bool ReadyForTreatment=>Step>=10 && pendingStep==0;
    public static DEF_ClinicTraining For(Component source)
    {
        foreach(var t in FindObjectsByType<DEF_ClinicTraining>(FindObjectsSortMode.None))if(t.gameObject.scene==source.gameObject.scene)return t;
        return null;
    }
    public static bool Allow(Component source,int required,string action)
    {
        var t=For(source);if(t==null)return true;
        if(t.Step>=required&&t.pendingStep==0)return true;
        MED_PlayerHUD.ShowMessage(action+" is locked. Finish Clinic Step "+t.Step+" and read the next introduction first.");return false;
    }
    void OnEnable(){if(subscribed)return;subscribed=true;MED_Harvestable.HarvestCommitted+=Harvest;MED_SupplyCart.UnloadCommitted+=Unload;MED_Building.OnBuilt+=Built;MED_PrepBench.CraftCommitted+=Craft;}
    void OnDisable(){if(!subscribed)return;subscribed=false;MED_Harvestable.HarvestCommitted-=Harvest;MED_SupplyCart.UnloadCommitted-=Unload;MED_Building.OnBuilt-=Built;MED_PrepBench.CraftCommitted-=Craft;}
    void Harvest(MED_Harvestable source,MED_PlayerResources p,MED_ToolType tool,MED_ResourceType resource,int amount)
    {
        if(p!=patient||source.gameObject.scene!=gameObject.scene||amount<=0||pendingStep!=0)return;
        if(Step==7){harvested.Add(resource);if(harvested.Contains(MED_ResourceType.Herbs)&&harvested.Contains(MED_ResourceType.Water)&&(harvested.Contains(MED_ResourceType.Alcohol)||harvested.Contains(MED_ResourceType.Gauze)))Reveal(8,"Harvest the marked resource in the build grid. Grip UNLOAD once on the cart to receive a Clinic supply. The cart brings supplies to you; it does not deliver Medicine to Defense.");}
        else if(Step==8&&source.GetComponent<DEF_ClinicRandomSource>()!=null&&wildSpawner!=null&&source.GetComponent<DEF_ClinicRandomSource>().Owner==wildSpawner){randomFound=true;CheckEight();}
    }
    void Unload(MED_SupplyCart source,MED_PlayerResources p,MED_ResourceType resource,bool empty){if(p==patient&&source.gameObject.scene==gameObject.scene&&Step==8&&pendingStep==0){unloaded=true;CheckEight();}}
    void CheckEight(){if(randomFound&&unloaded)Reveal(9,"Place a PrepBench on green grid, then grip Medicine: 2 Herbs + 1 Water. Each craft makes one dose; healing yourself from 50 to 100 needs three.");}
    void Built(MED_Building b){if(Step==9&&pendingStep==0&&b.Definition==benchDefinition&&b.transform.IsChildOf(buildingsRoot)&&b.Cells.Length>0)placed=true;}
    void Craft(MED_PrepBench b,MED_PlayerResources p,MED_Recipe recipe){if(Step==9&&pendingStep==0&&placed&&p==patient&&recipe==medicineRecipe&&b.transform.IsChildOf(buildingsRoot))Reveal(10,"Craft three Medicine total. Place TreatmentStation, grip REFILL for each dose, then stand on the green pad and grip TREAT once. Stay there to heal yourself to 100.");}
    void Reveal(int next,string text){pendingStep=next;revealUntil=Time.unscaledTime+4;SetDirections(text);}
    void Update()
    {
        UpdateLearningMarker();
        if(pendingStep!=0&&Time.unscaledTime>=revealUntil){
            if(pendingStep==8&&(wildSpawner==null||!wildSpawner.EnsureLearningSource())){SetDirections("Random resource unavailable. Free a Clinic grid cell or restart the full run; Step8 remains locked.");revealUntil=Time.unscaledTime+1;return;}
            Step=pendingStep;pendingStep=0;}
        foreach(var mini in FindObjectsByType<MED_BuildingMini>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(mini.gameObject.scene==gameObject.scene){if(!mini.gameObject.activeSelf)mini.gameObject.SetActive(true);mini.ApplyLock();}
        if(Step==10&&pendingStep==0)
        {
            var readiness=GetComponent<DEF_ClinicReadiness>();
            string reason = readiness != null ? readiness.PendingReason : "Heal yourself to 100 with freshly crafted Medicine.";
            // Health alone never proves completion: always retain the authenticated source's reason.
            SetDirections(patient != null && patient.IsHealthFull ? reason :
                (readiness == null || readiness.station == null ? "Place a TreatmentStation model. " : "") +
                "Your health starts at 50. Three Medicine heals you to 100. Grip REFILL for each dose; stand on the pad, grip TREAT once and stay there.\n" + reason);
        }
        if(Step==7&&pendingStep==0)
            SetDirections(CompactInstruction + " Hold the tool with GRIP; touch its tip to the resource and swing or hold TRIGGER.");
        if(Step==8&&pendingStep==0)
            SetDirections((randomFound ? "Random resource harvested. " : "Find and harvest a randomly placed resource. ") +
                (unloaded ? "Cart DONE: one supply added to your Clinic inventory. No more unloading needed." : "Cart NEEDED: grip UNLOAD once to receive one Clinic supply. No loading or Defense delivery required."));
        if(Step==9&&pendingStep==0)
            SetDirections(placed ? "Grip the Medicine recipe card at your PrepBench: 2 Herbs + 1 Water. Wait for crafting to finish." :
                "Grip the PrepBench model on the shelf. Aim at a green grid location and release to build; then grip its Medicine recipe card.");
    }
    public void RestartFullRun(){Time.timeScale=1;SceneManager.LoadScene(gameObject.scene.path);}
}
