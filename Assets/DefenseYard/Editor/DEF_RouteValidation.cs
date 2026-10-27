using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
public static class DEF_RouteValidation
{
    [MenuItem("Defense Yard/MP2B/Validate Open Station Routes")]
    public static void Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode only");
        var s=SceneManager.GetActiveScene();var results=new List<string>();
        void Check(bool ok,string name){results.Add((ok?"PASS ":"FAIL ")+name);}
        var all=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)).Where(c=>c!=null).ToArray();
        bool desktop=s.name=="DEF_Full_Desktop_Demo";
        Check(all.OfType<XRDeviceSimulator>().Count()==0,"no classic simulator");
        Check(all.OfType<XRInteractionSimulator>().Count()==(desktop?1:0),"correct modern simulator count");
        Check(all.OfType<Unity.XR.CoreUtils.XROrigin>().Count()==1,"one XR Origin");
        var rig=all.OfType<Unity.XR.CoreUtils.XROrigin>().SingleOrDefault();
        var capsule=rig==null?null:rig.GetComponentInChildren<CharacterController>(true);
        Check(capsule!=null,"rig CharacterController available for clearance");
        var providers=all.OfType<TeleportationProvider>().Where(p=>p.isActiveAndEnabled).ToArray();
        Check(providers.Length==1,"one active teleport provider");
        Check(all.Any(p=>p.GetType().Name=="LocomotionMediator"&&p.isActiveAndEnabled),"active locomotion mediator");
        var rays=all.OfType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>().Where(r=>r.name.Contains("Teleport")).ToArray();
        Check(rays.Length>0&&rays.All(r=>(r.interactionLayers.value & unchecked((int)0x80000000))!=0 && (r.raycastMask.value&1)!=0),"teleport rays accept Teleport interaction layer and Default physics layer");
        var roots=s.GetRootGameObjects().Where(g=>g.name=="DEF connecting station routes").ToArray();
        Check(roots.Length==1,"one generated route root");
        if(roots.Length==1)
        {
            Physics.SyncTransforms();
            var solids=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger).ToArray();
            bool Ground(Vector3 p,out RaycastHit best)
            {
                best=default;bool found=false;float distance=float.MaxValue;
                foreach(var c in solids)if(c.Raycast(new Ray(p+Vector3.up*.35f,Vector3.down),out var hit,.7f)&&hit.distance<distance){found=true;best=hit;distance=hit.distance;}
                return found;
            }
            Check(Ground(new Vector3(29.7546f,0,3.7014f),out _),"recorded fall position now has ground");
            foreach(var c in roots[0].GetComponentsInChildren<BoxCollider>())
            {
                bool continuous=true,clear=true;var t=c.transform;
                int steps=Mathf.CeilToInt(t.lossyScale.z/.2f);
                for(int sample=0;sample<=steps;sample++)
                {
                    float z=Mathf.Lerp(-.5f,.5f,(float)sample/steps);
                    var p=t.TransformPoint(new Vector3(0,.5f,z));
                                        bool supported=Ground(p,out var hit);
                    float slope=supported?Vector3.Angle(hit.normal,Vector3.up):float.NaN;
                    if(!supported||slope>25)
                    {
                        continuous=false;
                        results.Add($"GROUND FAILURE {c.name} sample={sample}/{steps} localZ={z:R} world={p:F6} supported={supported} collider={(hit.collider?hit.collider.name:"none")} hit={hit.point:F6} normal={hit.normal:F6} slope={slope:R}");
                        // Record nearby inset probes for diagnosis only; these never change the verdict.
                        foreach(float offset in new[]{-.002f,.002f})
                        {
                            var probe=p+t.forward*offset;bool ok=Ground(probe,out var near);
                            results.Add($"DIAGNOSTIC offset={offset:R} supported={ok} collider={(near.collider?near.collider.name:"none")} normal={near.normal:F6}");
                        }
                        continue;
                    }
                    if(capsule==null){clear=false;continue;}
                    float radius=capsule.radius*Mathf.Max(capsule.transform.lossyScale.x,capsule.transform.lossyScale.z);
                    float height=Mathf.Max(capsule.height*capsule.transform.lossyScale.y,2*radius);
                    var bottom=hit.point+Vector3.up*(radius+.08f);
                    var top=bottom+Vector3.up*(height-2*radius);
                    foreach(var other in Physics.OverlapCapsule(bottom,top,radius,~0,QueryTriggerInteraction.Ignore))
                    {
                        if(other.gameObject.scene!=s||other==hit.collider||other.transform.IsChildOf(rig.transform))continue;
                        // CharacterController can step onto a nearby walkable top; verify both
                        // its configured step height and the entire capsule at the raised height.
                        float step=capsule.stepOffset*capsule.transform.lossyScale.y;
                        var bounds=other.bounds;
                        var topProbe=new Vector3(Mathf.Clamp(hit.point.x,bounds.min.x+.001f,bounds.max.x-.001f),hit.point.y+step+.02f,Mathf.Clamp(hit.point.z,bounds.min.z+.001f,bounds.max.z-.001f));
                        bool allowedStep=false;
                        if(other.Raycast(new Ray(topProbe,Vector3.down),out var stepHit,step+.04f))
                        {
                            float rise=stepHit.point.y-hit.point.y;
                            if(rise>0 && rise<=step && Vector3.Angle(stepHit.normal,Vector3.up)<=25)
                            {
                                var raisedBottom=bottom+Vector3.up*rise;var raisedTop=top+Vector3.up*rise;
                                allowedStep=!Physics.OverlapCapsule(raisedBottom,raisedTop,radius,~0,QueryTriggerInteraction.Ignore)
                                    .Any(block=>block.gameObject.scene==s&&!block.transform.IsChildOf(rig.transform));
                                if(allowedStep)results.Add($"STEP {c.name} at {hit.point:F4} onto={other.name} rise={rise:F4} configuredStep={step:F4} skin={capsule.skinWidth:F4}; raised capsule clear");
                            }
                        }
                        if(!allowedStep){clear=false;results.Add("OBSTRUCTION "+c.name+" at "+hit.point+" collider="+other.name+" configuredStep="+step);}
                    }
                }
                Check(continuous,c.name+" sampled ground and slope");Check(clear,c.name+" center-line entrance/head clearance");
                Check(c.GetComponent<TeleportationArea>()!=null,c.name+" teleport surface");
            }
            foreach(var p in new[]{new Vector3(-46.5f,.05f,7.5f),new Vector3(80,0,7.7f),new Vector3(44,0,7.5f),new Vector3(0,2.7f,22),new Vector3(0,2.7f,19),new Vector3(24,0,3.7f),new Vector3(0,0,30)})Check(Ground(p,out _),"join/entrance ground "+p);
        }
        var previous=s;var fixture=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        try{DEF_StationRoutes.Generate(fixture);int n=fixture.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BoxCollider>()).Count();DEF_StationRoutes.Generate(fixture);Check(fixture.GetRootGameObjects().Count(g=>g.name=="DEF connecting station routes")==1&&fixture.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BoxCollider>()).Count()==n,"repeat generation does not duplicate geometry");}
        finally{EditorSceneManager.CloseScene(fixture,true);SceneManager.SetActiveScene(previous);}
        File.WriteAllLines("../station-route-validation-"+s.name+".txt",results);Debug.Log(string.Join("\n",results));if(results.Any(x=>x.StartsWith("FAIL")))throw new Exception("Route validation failed; inspect report before Play/build");
    }
}
