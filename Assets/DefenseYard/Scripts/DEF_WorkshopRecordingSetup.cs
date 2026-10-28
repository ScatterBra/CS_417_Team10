using UnityEngine;
// Opt-in recording scene configuration; never creates items or completion evidence.
[DefaultExecutionOrder(-500)]
public sealed class DEF_WorkshopRecordingSetup : MonoBehaviour
{
    public DEF_HUD hud;
    public Canvas interfaceCanvas;
    public Transform xrOrigin;
    float nextCanvasScan;
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
        if(xrOrigin!=null)foreach(var b in xrOrigin.GetComponentsInChildren<MonoBehaviour>(true))
            if(b!=null && b.GetType().Name=="JumpProvider")b.enabled=false;
    }
    void OnDestroy(){DEF_Turret.turretCost=40;DEF_Turret.boomTurretCost=70;DEF_Barricade.cost=20;int[] prices={40,70,20,50};for(int i=0;i<prices.Length;i++)DEF_HUD.Prices[i]=prices[i];}
}


