using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

// Prepared source checks. Run only with the rest of the authorized Edit Mode suites.
public static class DEF_WalkthroughValidation
{
    public static void Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode only");
        var results=new List<string>();var oldState=DEF_GameState.I;var oldWave=DEF_WaveManager.I;var oldPF=DEF_Pathfinder.I;
        var oldActors=DEF_Zombie.all;DEF_Zombie.all=new List<DEF_Zombie>();
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        object Get(object o,string f)=>o.GetType().GetField(f,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
        void Set(object o,string f,object v)=>o.GetType().GetField(f,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v);
        void Call(object o,string f,params object[] a)=>o.GetType().GetMethod(f,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,a);
        void Check(bool ok,string name)=>results.Add((ok?"PASS ":"FAIL ")+name);
        try {
            var grid=new GameObject("Grid").AddComponent<DEF_GridManager>();
            var pf=new GameObject("Pathfinder").AddComponent<DEF_Pathfinder>();DEF_Pathfinder.I=pf;
            Set(pf,"grid",grid);Set(pf,"blocked",new bool[grid.width,grid.depth]);Set(pf,"ready",true);
            var state=new GameObject("State").AddComponent<DEF_GameState>();DEF_GameState.I=state;
            var gate=state.gameObject.AddComponent<DEF_Progression>();
            var presenter=new GameObject("Presenter").AddComponent<DEF_ProgressionPresenter>();
            presenter.goalText=new GameObject("Goal").AddComponent<TextMeshPro>();presenter.introductionText=new GameObject("Intro").AddComponent<TextMeshPro>();Set(gate,"presenter",presenter);
            var waves=new GameObject("Waves").AddComponent<DEF_WaveManager>();DEF_WaveManager.I=waves;
            var s=gate.State;
            foreach(DEF_ProgressionState.Prerequisite p in Enum.GetValues(typeof(DEF_ProgressionState.Prerequisite)))s.AcceptPrerequisite(p,1,p.ToString());
            s.Place(1,1,"sentry");s.CompleteIntroduction(1,1,s.Introduction);s.Repair(1,1,"sentry",10);
            s.CompleteIntroduction(1,1,s.Introduction);s.Refill(1,1,10,"box");s.CompleteIntroduction(1,1,s.Introduction);
            s.BeginWave(1,1);s.ClearWave(1,1,1,150);s.CompleteIntroduction(1,1,s.Introduction);waves.phase=DEF_WaveManager.Phase.Active;state.waveNumber=2;
            var zombie=new GameObject("Walker").AddComponent<DEF_Zombie>();zombie.transform.position=grid.TileCenter(10,3);
            Set(zombie,"grid",grid);Set(zombie,"pf",pf);zombie.Repath();
            ((List<DEF_Zombie>)Get(waves,"attemptActors")).Add(zombie);
            var bar=new GameObject("Barricade").AddComponent<DEF_Barricade>();
            ((List<MonoBehaviour>)Get(gate,"advancedDefenses")).Add(bar);
            Call(gate,"BarricadeIntercept",bar,zombie);
            Check(!(bool)Get(s,"routeControlled"),"attack interception cannot award route mastery");
            Call(zombie,"CaptureRoute",bar);pf.SetBlocked(10,6,true);pf.Register(new Vector2Int(10,6),bar.gameObject);Call(zombie,"RepathForBarricade",bar,new Vector2Int(10,6));
            Check(Get(zombie,"detourRoute")!=null && !(bool)Get(s,"routeControlled"),"causal route change alone does not award mastery");
            var detour=(HashSet<Vector2Int>)Get(zombie,"detourRoute");var old=(HashSet<Vector2Int>)Get(zombie,"oldRoute");
            Vector2Int exclusive=default;bool found=false;foreach(var cell in detour)if(!old.Contains(cell)){exclusive=cell;found=true;break;}
            Check(found,"blocked original route produces a distinct walkable detour");
            var end=grid.TileCenter(exclusive.x,exclusive.y);
            Call(zombie,"RecordDetourMovement",end,end);
            Check(!(bool)Get(s,"routeControlled"),"zero movement cannot satisfy route evidence");
            zombie.transform.position=end-Vector3.right*.6f;zombie.transform.rotation=Quaternion.LookRotation(Vector3.right);
            Call(zombie,"MoveToward",end,1f);
            Check((bool)Get(s,"routeControlled"),"movement on new route awards route evidence");
            Check(Get(zombie,"detourCause")==null,"route receipt consumed once");
            // Re-arm a real route change, then invalidate its captured session before movement.
            zombie.transform.position=grid.TileCenter(10,3);pf.SetBlocked(10,6,false);zombie.Repath();Call(zombie,"CaptureRoute",bar);pf.SetBlocked(10,6,true);Call(zombie,"RepathForBarricade",bar,new Vector2Int(10,6));
            s.NewSession();Call(zombie,"RecordDetourMovement",end-Vector3.right,end);
            Check(!(bool)Get(s,"routeControlled") && Get(zombie,"detourCause")==null,"session reset rejects pending movement receipt");
            var hand=new GameObject("Right Controller");var near=hand.AddComponent<NearFarInteractor>();var teleport=new GameObject("Teleport").AddComponent<XRRayInteractor>();teleport.transform.SetParent(hand.transform);
            var input=new GameObject("Defense input").AddComponent<DEF_VRInput>();input.rayInteractor=teleport;
            DEF_IntegrationStaging.BindDefenseAim(input,hand.transform);
            Check(input.nearFarInteractor==near && input.rayInteractor==null && teleport.enabled,"binding uses Near/Far without disabling teleport");
            near.enabled=false;Check(!input.TryGetAimHit(out _),"inactive Near/Far cannot fall back to teleport");
            var canvas=new GameObject("Defense Interface",typeof(RectTransform)).AddComponent<Canvas>();
            DEF_IntegrationStaging.EnsureTrackedUI(canvas);DEF_IntegrationStaging.EnsureTrackedUI(canvas);
            Check(canvas.GetComponents<TrackedDeviceGraphicRaycaster>().Length==1,"tracked UI raycaster is added idempotently");
        }
        catch(Exception e){results.Add("FAIL exception "+e);}
        finally {
            EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);
            DEF_GameState.I=oldState;DEF_WaveManager.I=oldWave;DEF_Pathfinder.I=oldPF;DEF_Zombie.all=oldActors;
            System.IO.File.WriteAllLines("../walkthrough-editmode-results.txt",results);Debug.Log(string.Join("\n",results));
        }
    }
}
