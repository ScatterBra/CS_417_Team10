using UnityEngine;

// Visible local feedback for the simplified recording scene's physical intakes.
public sealed class DEF_RecordingReceiverLabel : MonoBehaviour
{
    TextMesh label;
    DEF_WorkshopRepairReceiver repair;
    DEF_WorkshopAmmoReceiver ammo;
    void Awake()
    {
        repair=GetComponent<DEF_WorkshopRepairReceiver>();ammo=GetComponent<DEF_WorkshopAmmoReceiver>();
        var go=new GameObject("Physical delivery instructions and feedback");go.transform.SetParent(transform,false);go.transform.localPosition=Vector3.up*.65f;
        label=go.AddComponent<TextMesh>();label.fontSize=40;label.characterSize=.009f;label.anchor=TextAnchor.MiddleCenter;
        GetComponent<DEF_ContentGate>()?.Apply();
    }
    void LateUpdate()
    {
        var message=repair!=null?repair.LastMessage:ammo!=null?ammo.LastMessage:null;
        if(string.IsNullOrEmpty(message))message="Hold grip to carry. Release the crafted item here.";
        var words=message.Split(' ');var body=new System.Text.StringBuilder();int width=0;
        foreach(var word in words){if(width+word.Length>44){body.Append('\n');width=0;}body.Append(word).Append(' ');width+=word.Length+1;}
        label.text=(repair!=null?"REPAIR KIT INTAKE":"AMMO BOX INTAKE")+"\n"+body;
        var camera=Camera.main;if(camera!=null)label.transform.rotation=Quaternion.LookRotation(label.transform.position-camera.transform.position);
    }
}
