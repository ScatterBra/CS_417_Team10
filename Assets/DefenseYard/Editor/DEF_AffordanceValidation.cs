using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
public static class DEF_AffordanceValidation
{
    public static void Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode only");
        var results=new List<string>();var old=DEF_GameState.I;var oldWave=DEF_WaveManager.I;var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        void Check(bool ok,string name)=>results.Add((ok?"PASS ":"FAIL ")+name);
        void Set(object o,string f,object v)=>o.GetType().GetField(f,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
        try
        {
            var state=new GameObject("Affordance state").AddComponent<DEF_GameState>();DEF_GameState.I=state;
            var gate=state.gameObject.AddComponent<DEF_Progression>();var presenter=new GameObject("Presenter").AddComponent<DEF_ProgressionPresenter>();
            presenter.goalText=new GameObject("Goal").AddComponent<TextMeshPro>();presenter.introductionText=new GameObject("Introduction").AddComponent<TextMeshPro>();Set(gate,"presenter",presenter);
            var waves=new GameObject("Waves").AddComponent<DEF_WaveManager>();DEF_WaveManager.I=waves;
            foreach(var phase in new[]{DEF_WaveManager.Phase.Spawning,DEF_WaveManager.Phase.Active})
            {waves.phase=phase;waves.StartWave();Check(waves.phase==phase&&gate.LastFailure.Contains("already active")&&gate.LastFailure.Contains("Clear the current enemies"),"specific repeated-wave reason/remedy in "+phase);}
            waves.phase=DEF_WaveManager.Phase.Idle;
            var row=new GameObject("Resource row");var ammo=new GameObject("Ammo",typeof(RectTransform)).AddComponent<UnityEngine.UI.Text>();ammo.transform.SetParent(row.transform);var label=new GameObject("Ammo label");label.transform.SetParent(row.transform);var scrap=new GameObject("Scrap");scrap.transform.SetParent(row.transform);
            DEF_HUD.SetResourceVisibility(ammo,"Ammo label",false);Check(!ammo.gameObject.activeSelf&&!label.activeSelf&&scrap.activeSelf,"hiding ammo value and label preserves shared-row scrap");
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);var turret=obj.AddComponent<DEF_Turret>();turret.explosive=true;var content=obj.GetComponent<DEF_ContentGate>();content.Apply();
            Check(obj.GetComponent<Renderer>().forceRenderingOff&&!obj.GetComponent<Collider>().enabled,"preplaced missile render and collider hidden before unlock");
            Check(!DEF_Tutorials.Allowed("boom")&&!DEF_Tutorials.Allowed("repair")&&!DEF_Tutorials.Allowed("tile"),"future tutorial affordances rejected before prerequisites");
            Check(DEF_Tutorials.RepairAdvice.Contains("RepairKit")&&!DEF_Tutorials.RepairAdvice.Contains("scrap"),"configured repair guidance teaches physical kit");
            foreach(DEF_ProgressionState.Prerequisite p in Enum.GetValues(typeof(DEF_ProgressionState.Prerequisite)))gate.State.AcceptPrerequisite(p,gate.State.Session,p.ToString());
            Check(!DEF_RepairDelivery.TryRepair(null,null,null,40,false,out _,out _)&&gate.LastFailure.Contains("physical kit"),"direct remote repair helper cannot bypass physical progression");
            gate.State.Place(gate.State.Session,gate.State.Attempt,"fixture");
            Check(!DEF_Tutorials.Allowed("repair")&&!gate.IsAllowed(DEF_Action.Repair),"repair remains inaccessible during repair introduction");
            gate.State.CompleteIntroduction(gate.State.Session,gate.State.Attempt,gate.State.Introduction);
            Check(DEF_Tutorials.Allowed("repair")&&!DEF_Tutorials.Allowed("harvest"),"physical repair tutorial opens while legacy crate tutorial stays closed");
            gate.State.Repair(gate.State.Session,gate.State.Attempt,"fixture",10);gate.State.CompleteIntroduction(gate.State.Session,gate.State.Attempt,gate.State.Introduction);
            gate.State.Refill(gate.State.Session,gate.State.Attempt,10,"fixture");gate.State.CompleteIntroduction(gate.State.Session,gate.State.Attempt,gate.State.Introduction);gate.State.BeginWave(gate.State.Session,gate.State.Attempt);
            var trap=new GameObject("Preplaced shock").AddComponent<DEF_ShockTrap>();waves.phase=DEF_WaveManager.Phase.Active;
            Check(!trap.CanDischarge,"preplaced Shock cannot bypass advanced threshold in first wave");
            gate.State.ClearWave(gate.State.Session,gate.State.Attempt,1,100);
            Check(!gate.IsAllowed(DEF_Action.Shock)&&!DEF_Tutorials.Allowed("shock"),"advanced mechanics remain locked during pair introduction");
            gate.State.CompleteIntroduction(gate.State.Session,gate.State.Attempt,gate.State.Introduction);
            Check(trap.CanDischarge&&DEF_Tutorials.Allowed("shock")&&!content.Visible,"advanced pair unlock does not reveal Missile");
            gate.State.RecordControl(gate.State.Session,gate.State.Attempt,"route",false);gate.State.RecordControl(gate.State.Session,gate.State.Attempt,"stun",true);content.Apply();
            Check(!content.Visible&&obj.GetComponent<Renderer>().forceRenderingOff,"Missile stays hidden during its introduction");
            gate.State.CompleteIntroduction(gate.State.Session,gate.State.Attempt,gate.State.Introduction);content.Apply();
            Check(!obj.GetComponent<Renderer>().forceRenderingOff&&obj.GetComponent<Collider>().enabled&&DEF_Tutorials.Allowed("boom"),"Missile presentation restored only after introduction");
            gate.BeginNewSession();content.Apply();Check(obj.GetComponent<Renderer>().forceRenderingOff&&!DEF_Tutorials.Allowed("boom"),"new session re-locks preplaced content and tutorials");
            UnityEngine.Object.DestroyImmediate(gate);content.Apply();
            Check(!obj.GetComponent<Renderer>().forceRenderingOff&&obj.GetComponent<Collider>().enabled&&DEF_Tutorials.RepairAdvice.Contains("10 scrap")&&DEF_Tutorials.Allowed("harvest"),"explicit legacy mode restores original presentation and scrap guidance");
        }
        catch(Exception error){results.Add("FAIL exception "+error);}
        finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);DEF_GameState.I=old;DEF_WaveManager.I=oldWave;System.IO.File.WriteAllLines("../affordance-editmode-results.txt",results);Debug.Log(string.Join("\n",results));}
    }
}

