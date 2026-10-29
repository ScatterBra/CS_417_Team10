using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class DEF_StationRoutes
{
    const string Root="DEF connecting station routes";
    [MenuItem("Defense Yard/MP2B/Apply Station Routes To Open Copy")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode only");
        var s=SceneManager.GetActiveScene();
        if(!new[]{"DEF_Full_Desktop_Demo","DEF_Clinic_Integration_Staging","Deadzone_Final"}.Contains(s.name))throw new Exception("Open an isolated full integration copy");
        Generate(s);Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);
    }
    public static void Generate(Scene s)
    {
        foreach(var old in s.GetRootGameObjects().Where(g=>g.name==Root))UnityEngine.Object.DestroyImmediate(old);
        var root=new GameObject(Root);SceneManager.MoveGameObjectToScene(root,s);
        void Walk(string name,Vector3 a,Vector3 b,float width)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root.transform);
            var delta=b-a;g.transform.position=(a+b)*.5f-Vector3.up*.1f;
            g.transform.rotation=Quaternion.LookRotation(delta,Vector3.up);g.transform.localScale=new Vector3(width,.2f,delta.magnitude+.15f);
            var area=g.AddComponent<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea>();
            area.interactionLayers = unchecked((int)0x80000000);
            area.filterSelectionByHitNormal=true;area.upNormalToleranceDegrees=25;
        }
        // Survey 2026-10-27: north doors Power x[-47.44,-45.605], Clinic x[79,81].
        // Defense fence top 2.223m: a walking bridge clears it without changing owner walls.
        Walk("Station spine",new Vector3(-46.5f,0,30),new Vector3(80,0,30),4);
        Walk("Power door",new Vector3(-46.5f,.05f,6.8f),new Vector3(-46.5f,.05f,30),1.4f);
        Walk("Clinic door",new Vector3(80,0,6.8f),new Vector3(80,0,30),1.5f);
        Walk("Workshop north exit",new Vector3(44,0,5),new Vector3(44,0,30),2);
        Walk("Workshop west approach",new Vector3(32,0,3.7f),new Vector3(24,0,3.7f),4);
        Walk("West approach to spine",new Vector3(24,0,3.7f),new Vector3(24,0,30),4);
                // Cover the inclined end cap with the flat landing. The former tip projected
        // above the landing and its vertical ray hit the 71-degree end face.
        // 0.2m longitudinal overlap and a 0.05m height allowance keep that cap buried.
        Walk("Defense bridge outside ramp",new Vector3(0,0,30),new Vector3(0,2.65f,22.2f),3);
        Walk("Defense fence overpass",new Vector3(0,2.7f,22.4f),new Vector3(0,2.7f,19),3);
        Walk("Defense bridge inside ramp",new Vector3(0,2.7f,19),new Vector3(0,0,10),3);
        foreach(var x in new[]{-46.5f,0,44,80})
        {
            var sign=new GameObject("Station directions");sign.transform.SetParent(root.transform);sign.transform.position=new Vector3(x,1.7f,32);
            var text=sign.AddComponent<TextMesh>();text.text="POWER <-   DEFENSE 0   WORKSHOP 44   CLINIC 80 ->\nFollow the connected walkways";text.characterSize=.12f;text.fontSize=40;text.anchor=TextAnchor.MiddleCenter;
        }

    }
}
