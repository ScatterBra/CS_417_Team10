using UnityEngine;

// Recording-only visible intake and Clinic-style contrast card. Transactions stay on the receiver.
public sealed class DEF_RecordingReceiverLabel : MonoBehaviour
{
    MED_WorldPanel panel;
    DEF_WorkshopRepairReceiver repair;
    DEF_WorkshopAmmoReceiver ammo;
    Material intakeMaterial;
    void Awake()
    {
        repair=GetComponent<DEF_WorkshopRepairReceiver>();ammo=GetComponent<DEF_WorkshopAmmoReceiver>();
        panel=MED_WorldPanel.Create("Physical delivery instructions and feedback",new Vector2(1.4f,.8f),1.1f,.85f,new Color(.65f,1,.85f),new Color(.1f,.85f,.6f));
        panel.Root.transform.SetParent(transform,false);panel.Root.transform.localPosition=Vector3.up*.85f;
        panel.Body.enableAutoSizing=true;panel.Body.fontSizeMin=.75f;panel.Body.fontSizeMax=.85f;
        panel.Body.overflowMode=TMPro.TextOverflowModes.Truncate;
        if(ammo!=null)
        {
            var box=GetComponent<BoxCollider>();var size=box.size;var center=box.center;
            intakeMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));intakeMaterial.color=new Color(.07f,.25f,.22f);
            void Part(string name,Vector3 offset,Vector3 dimensions)
            {
                var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.name=name;part.transform.SetParent(transform,false);part.transform.localPosition=center+offset;part.transform.localScale=dimensions;
                part.GetComponent<Renderer>().sharedMaterial=intakeMaterial;
                Destroy(part.GetComponent<Collider>());
            }
            Part("Ammo intake base",new Vector3(0,-size.y*.5f-.06f,0),new Vector3(size.x+.12f,.12f,size.z+.12f));
            Part("Ammo intake left rim",new Vector3(-size.x*.5f-.04f,-.2f,0),new Vector3(.08f,.4f,size.z+.12f));
            Part("Ammo intake right rim",new Vector3(size.x*.5f+.04f,-.2f,0),new Vector3(.08f,.4f,size.z+.12f));
            Part("Ammo intake back",new Vector3(0,-.2f,size.z*.5f+.04f),new Vector3(size.x,.4f,.08f));
            Part("Ammo intake pedestal",new Vector3(0,-.75f,0),new Vector3(.55f,.5f,.55f));
        }
        GetComponent<DEF_ContentGate>()?.Apply();
    }
    void LateUpdate()
    {
        var message=repair!=null?repair.LastMessage:ammo!=null?ammo.LastMessage:null;
        if(string.IsNullOrEmpty(message))message="Hold grip to carry. Release the crafted item here.";
        panel.SetText(repair!=null?"REPAIR KIT INTAKE":"CENTRAL AMMO INTAKE",message);
        var setup=FindAnyObjectByType<DEF_WorkshopRecordingSetup>();
        var rig=setup!=null&&setup.xrOrigin!=null?setup.xrOrigin.GetComponent<Unity.XR.CoreUtils.XROrigin>():null;
        var camera=rig!=null?rig.Camera:Camera.main;
        if(camera!=null)panel.Root.transform.rotation=Quaternion.LookRotation(panel.Root.transform.position-camera.transform.position);
    }
    void OnDestroy(){panel?.Destroy();if(intakeMaterial!=null)Destroy(intakeMaterial);}
}
