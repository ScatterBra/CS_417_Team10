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
    void SetDirections(string text)
    {
        CurrentInstruction = text;
        if (directions != null && directions.text != text) directions.text = text;
    }
    readonly HashSet<MED_ResourceType> harvested=new();
    bool randomFound,unloaded,placed,subscribed;
    float revealUntil;
    int pendingStep=7;
    void Start(){Reveal(7,"CLINIC: Sickle harvests Herbs; Bucket collects Water; Crowbar opens supplies. Read this introduction before using tools.");}
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
        if(Step==7){harvested.Add(resource);if(harvested.Contains(MED_ResourceType.Herbs)&&harvested.Contains(MED_ResourceType.Water)&&(harvested.Contains(MED_ResourceType.Alcohol)||harvested.Contains(MED_ResourceType.Gauze)))Reveal(8,"Find and harvest a randomly placed resource. Unload the arriving cart before it leaves.");}
        else if(Step==8&&source.GetComponent<DEF_ClinicRandomSource>()!=null&&wildSpawner!=null&&source.GetComponent<DEF_ClinicRandomSource>().Owner==wildSpawner){randomFound=true;CheckEight();}
    }
    void Unload(MED_SupplyCart source,MED_PlayerResources p,MED_ResourceType resource,bool empty){if(p==patient&&source.gameObject.scene==gameObject.scene&&Step==8&&pendingStep==0){unloaded=true;CheckEight();}}
    void CheckEight(){if(randomFound&&unloaded)Reveal(9,"Place a PrepBench on a green valid grid location, then craft Medicine: 2 Herbs and 1 Water.");}
    void Built(MED_Building b){if(Step==9&&pendingStep==0&&b.Definition==benchDefinition&&b.transform.IsChildOf(buildingsRoot)&&b.Cells.Length>0)placed=true;}
    void Craft(MED_PrepBench b,MED_PlayerResources p,MED_Recipe recipe){if(Step==9&&pendingStep==0&&placed&&p==patient&&recipe==medicineRecipe&&b.transform.IsChildOf(buildingsRoot))Reveal(10,"Place the TreatmentStation model, stock it with crafted Medicine, then stand on its pad, grip TREAT once and stay on the pad until treatment finishes.");}
    void Reveal(int next,string text){pendingStep=next;revealUntil=Time.unscaledTime+4;SetDirections(text);}
    void Update()
    {
        if(pendingStep!=0&&Time.unscaledTime>=revealUntil){
            if(pendingStep==8&&(wildSpawner==null||!wildSpawner.EnsureLearningSource())){SetDirections("Random resource unavailable. Free a Clinic grid cell or restart the full run; Step8 remains locked.");revealUntil=Time.unscaledTime+1;return;}
            Step=pendingStep;pendingStep=0;}
        foreach(var mini in FindObjectsByType<MED_BuildingMini>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(mini.gameObject.scene==gameObject.scene){if(!mini.gameObject.activeSelf)mini.gameObject.SetActive(true);mini.ApplyLock();}
        if(Step==10&&pendingStep==0)
        {
            var readiness=GetComponent<DEF_ClinicReadiness>();
            string reason = readiness != null ? readiness.PendingReason : "Heal the Clinic patient to 100 with freshly crafted Medicine.";
            // Health alone never proves completion: always retain the authenticated source's reason.
            SetDirections(patient != null && patient.IsHealthFull ? reason :
                (readiness == null || readiness.station == null ? "Place a TreatmentStation model. " : "") +
                "50 to 100 needs 3 Medicine total. Grip REFILL for each unit; stand on the pad, grip TREAT once and stay there.\n" + reason);
        }
        if(Step==7&&pendingStep==0)
            SetDirections("Harvest Herbs with Sickle, Water with Bucket, and Alcohol or Gauze with Crowbar. Hold the tool with GRIP; touch its tip to the resource and swing or hold TRIGGER.");
        if(Step==8&&pendingStep==0)
            SetDirections((randomFound ? "Random resource harvested. " : "Find and harvest a randomly placed resource. ") +
                (unloaded ? "Cart unloaded." : "Grip a cart resource button to unload at least one item."));
        if(Step==9&&pendingStep==0)
            SetDirections(placed ? "Grip the Medicine recipe card at your PrepBench: 2 Herbs + 1 Water. Wait for crafting to finish." :
                "Grip the PrepBench model on the shelf. Aim at a green grid location and release to build; then grip its Medicine recipe card.");
    }
    public void RestartFullRun(){Time.timeScale=1;SceneManager.LoadScene(gameObject.scene.path);}
}
