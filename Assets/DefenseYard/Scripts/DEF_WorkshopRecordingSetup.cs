using UnityEngine;
// Opt-in recording scene configuration; never creates items or completion evidence.
[DefaultExecutionOrder(-500)]
public sealed class DEF_WorkshopRecordingSetup : MonoBehaviour
{
    public DEF_HUD hud;
    public Canvas interfaceCanvas;
    public Transform xrOrigin;
    public Canvas popupCanvas;
    public UnityEngine.UI.Button transferScrapButton;
    public bool OpenShopFromHead()
    {
        var rig=xrOrigin.GetComponent<Unity.XR.CoreUtils.XROrigin>();
        if(popupCanvas==null||rig.Camera==null)return false;
        var forward=Vector3.ProjectOnPlane(rig.Camera.transform.forward,Vector3.up).normalized;
        if(forward.sqrMagnitude<.5f)forward=xrOrigin.forward;
        var position=rig.Camera.transform.position+forward*1.35f-Vector3.up*.08f;
        var rotation=Quaternion.LookRotation(forward,Vector3.up);
        bool Blocks(Collider c)=>!c.transform.IsChildOf(xrOrigin)&&c.GetComponentInParent<SCV_CraftedItem>()==null;
        if(System.Array.Exists(Physics.OverlapBox(position,new Vector3(.8f,.45f,.03f),rotation,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore),Blocks)||
           System.Array.Exists(Physics.RaycastAll(rig.Camera.transform.position,forward,1.35f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore),hit=>Blocks(hit.collider)))
        {hud.Notify("Shop needs clear space in front of you. Face the open walkway and press A again.");return false;}
        popupCanvas.worldCamera=rig.Camera;
        popupCanvas.transform.SetPositionAndRotation(position,rotation);
        return true;
    }
    public void TransferWorkshopScrap()
    {
        SCV_DefenseSupplyLink link=null;
        foreach(var candidate in FindObjectsByType<SCV_DefenseSupplyLink>())if(candidate.gameObject.scene==gameObject.scene){if(link!=null){hud.Notify("Transfer unavailable: duplicate Workshop links.");return;}link=candidate;}
        if(link==null){hud.Notify("Finish Workshop crafting before transferring scrap.");return;}
        link.DeliverScrap();hud.Notify(link.FeedbackMessage);
    }
    float nextCanvasScan;
    public string BalanceLabel
    {
        get {var ledger=FindAnyObjectByType<SCV_ResourceManager>();return "DEFENSE: "+(DEF_GameState.I!=null?DEF_GameState.I.scrap:0)+" SCRAP   |   WORKSHOP: "+(ledger!=null?ledger.scrap:0)+" SCRAP";}
    }
    readonly System.Collections.Generic.Dictionary<SCV_CraftedItem,Vector3> safeItemPositions=new();
    void FixedUpdate()
    {
        // Recover the same paid representation after an accidental throw off the route.
        // Never create inventory, replace identities, or rescue a held/claimed item.
        foreach(var item in FindObjectsByType<SCV_CraftedItem>())
        {
            if(item.gameObject.scene!=gameObject.scene)continue;
            // Delivery supplies are dropped, not thrown; snap turning must not launch them.
            var grab=item.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if(grab!=null)grab.throwOnDetach=false;
            if(item.Consumed||item.IsReserved||item.IsHeld)continue;
            var body=item.GetComponent<Rigidbody>();if(body==null)continue;
            if(Physics.Raycast(body.position+Vector3.up*.1f,Vector3.down,out var hit,2f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<SCV_CraftedItem>()==null&&hit.point.y>-.2f)
                safeItemPositions[item]=hit.point+Vector3.up*.35f;
            if(body.position.y< -2f&&safeItemPositions.TryGetValue(item,out var safe))
            {body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.position=safe;hud.Notify("Dropped Workshop supply recovered on the walkway. Its original craft identity is preserved.");}
        }
    }
    void LateUpdate()
    {
        if(Time.unscaledTime<nextCanvasScan)return;
        nextCanvasScan=Time.unscaledTime+.25f;
        foreach(var receiver in FindObjectsByType<DEF_WorkshopRepairReceiver>())
            if(receiver.gameObject.scene==gameObject.scene && receiver.GetComponent<DEF_RecordingReceiverLabel>()==null)receiver.gameObject.AddComponent<DEF_RecordingReceiverLabel>();
        foreach(var receiver in FindObjectsByType<DEF_WorkshopAmmoReceiver>())
            if(receiver.gameObject.scene==gameObject.scene && receiver.GetComponent<DEF_RecordingReceiverLabel>()==null)receiver.gameObject.AddComponent<DEF_RecordingReceiverLabel>();
        var origin=xrOrigin!=null?xrOrigin.GetComponent<Unity.XR.CoreUtils.XROrigin>():null;
        if(origin==null || origin.Camera==null)return;
        foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            if(canvas.gameObject.scene==gameObject.scene && canvas.renderMode==RenderMode.WorldSpace && canvas.worldCamera!=origin.Camera)
                canvas.worldCamera=origin.Camera;
    }
    void Awake()
    {
        int[] prices={4,20,5,15}; for(int i=0;i<prices.Length;i++)DEF_HUD.Prices[i]=prices[i];
        DEF_Turret.turretCost=4; DEF_Turret.boomTurretCost=20; DEF_Barricade.cost=5;
        if(hud!=null)hud.movementSafeMenus=true;
        if(transferScrapButton!=null)transferScrapButton.onClick.AddListener(TransferWorkshopScrap);
        foreach(var attach in xrOrigin.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Attachment.InteractionAttachController>(true))
        {attach.useManipulationInput=false;attach.manipulationInput.inputSourceMode=UnityEngine.XR.Interaction.Toolkit.Inputs.Readers.XRInputValueReader.InputSourceMode.Unused;}
        if(xrOrigin!=null)foreach(var b in xrOrigin.GetComponentsInChildren<MonoBehaviour>(true))
            if(b!=null && b.GetType().Name=="JumpProvider")b.enabled=false;
    }
    void OnDestroy(){DEF_Turret.turretCost=40;DEF_Turret.boomTurretCost=70;DEF_Barricade.cost=20;int[] prices={40,70,20,50};for(int i=0;i<prices.Length;i++)DEF_HUD.Prices[i]=prices[i];}
}


