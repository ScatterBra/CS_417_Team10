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
    void Craft(MED_PrepBench b,MED_PlayerResources p,MED_Recipe recipe){if(Step==9&&pendingStep==0&&placed&&p==patient&&recipe==medicineRecipe&&b.transform.IsChildOf(buildingsRoot))Reveal(10,"Stock the TreatmentStation with crafted Medicine. Stand on its pad and treat until patient health reaches 100.");}
    void Reveal(int next,string text){pendingStep=next;revealUntil=Time.unscaledTime+4;if(directions)directions.text=text;}
    void Update()
    {
        if(pendingStep!=0&&Time.unscaledTime>=revealUntil){
            if(pendingStep==8&&(wildSpawner==null||!wildSpawner.EnsureLearningSource())){if(directions)directions.text="Random resource unavailable. Free a Clinic grid cell or restart the full run; Step8 remains locked.";revealUntil=Time.unscaledTime+1;return;}
            Step=pendingStep;pendingStep=0;}
        foreach(var mini in FindObjectsByType<MED_BuildingMini>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(mini.gameObject.scene==gameObject.scene){if(!mini.gameObject.activeSelf)mini.gameObject.SetActive(true);mini.ApplyLock();}
        if(directions&&Step==10&&pendingStep==0){var readiness=GetComponent<DEF_ClinicReadiness>();if(readiness!=null)directions.text=readiness.PendingReason;}
        if(directions&&Step==7&&pendingStep==0)directions.text="CLINIC: Harvest Herbs with Sickle, Water with Bucket, and supplies with Crowbar.";
    }
    public void RestartFullRun(){Time.timeScale=1;SceneManager.LoadScene(gameObject.scene.path);}
}
