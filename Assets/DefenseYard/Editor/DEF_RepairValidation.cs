using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

public static class DEF_RepairValidation
{
    [MenuItem("Defense Yard/MP2B/Run Repair Edit Mode Checks")]
    public static void Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode only");
        var results=new List<string>();var roots=new List<GameObject>();var old=DEF_GameState.I;
        var previousScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var testScene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(testScene);
        void Check(bool ok,string name)=>results.Add((ok?"PASS ":"FAIL ")+name);
        GameObject Make(string name){var go=new GameObject(name){hideFlags=HideFlags.HideInHierarchy};roots.Add(go);return go;}
        void Set(object obj,string field,object value)=>obj.GetType().GetField(field,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(obj,value);
        try
        {
            var state=Make("Repair test state").AddComponent<DEF_GameState>();DEF_GameState.I=state;
            var ledger=Make("Repair ledger").AddComponent<SCV_ResourceManager>();
            var other=Make("Other repair ledger").AddComponent<SCV_ResourceManager>();other.ResetRepairInventory(1);
            SCV_CraftedItem Kit(SCV_ResourceManager source)
            {
                var go=Make("RepairKit fixture");go.AddComponent<BoxCollider>();go.AddComponent<Rigidbody>().useGravity=false;
                var item=go.AddComponent<SCV_CraftedItem>();Set(item,"itemType",SCV_CraftedItem.ItemType.RepairKit);
                if(!item.BindLedger(source))throw new Exception("Kit fixture registration failed");return item;
            }
            var turret=Make("Target turret").AddComponent<DEF_Turret>();turret.health=40;
            var intake=Make("Target repair receiver").AddComponent<DEF_WorkshopRepairReceiver>();intake.ledger=ledger;intake.target=turret;
            var competitor=Make("Competing receiver").AddComponent<DEF_WorkshopRepairReceiver>();competitor.ledger=ledger;competitor.target=turret;
            ledger.ResetRepairInventory(1);var first=Kit(ledger);int receipts=0;float receiptBefore=0,receiptAfter=0;
            intake.PhysicalRepairAccepted+=(target,before,after,id)=>{receipts++;receiptBefore=before;receiptAfter=after;};
            Check(intake.TryDeliver(first)&&ledger.repairKits==0&&turret.health==80&&first==null&&receipts==1&&receiptBefore==40&&receiptAfter==80,"physical turret repair commits exact kit ledger and positive HP receipt");
            var successMessage=intake.LastMessage;
            var idleCollider=Make("Ambient collider after success").AddComponent<BoxCollider>();
            typeof(DEF_WorkshopRepairReceiver).GetMethod("OnTriggerStay",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(intake,new object[]{idleCollider});
            Check(!string.IsNullOrEmpty(successMessage)&&intake.LastMessage==successMessage,"ambient overlap preserves active successful repair feedback");
            Check(!intake.TryDeliver(first)&&!competitor.TryDeliver(first)&&turret.health==80&&receipts==1,"duplicate and competing receiver cannot repeat consumed repair");
            ledger.ResetRepairInventory(1);var full=Kit(ledger);turret.health=100;
            Check(!intake.TryDeliver(full)&&ledger.repairKits==1&&full!=null,"full health preserves exact kit and ledger");
            turret.health=0;Check(!intake.TryDeliver(full)&&ledger.repairKits==1&&full!=null,"dead target preserves kit");
            turret.health=95;Check(intake.TryDeliver(full)&&turret.health==100&&receiptAfter-receiptBefore==5,"near-full target records only actual positive HP delta");
            ledger.ResetRepairInventory(1);var wrongLedger=Kit(other);turret.health=50;
            Check(!intake.TryDeliver(wrongLedger)&&ledger.repairKits==1&&other.repairKits==1,"wrong source ledger rejected without consuming either area");
            var wrong=Make("Ammo fixture").AddComponent<SCV_CraftedItem>();Check(!intake.TryDeliver(wrong)&&ledger.repairKits==1,"wrong item rejected");
            void Trigger(string callback, Collider collider)=>typeof(DEF_WorkshopRepairReceiver).GetMethod(callback,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(intake,new object[]{collider});
            Set(intake,"feedbackExpires",Time.unscaledTime-1f);
            var ambient=Make("Unrelated floor or hand collider").AddComponent<BoxCollider>();
            Trigger("OnTriggerEnter",ambient);Trigger("OnTriggerStay",ambient);
            Check(intake.LastMessage==null&&ledger.repairKits==1,"ambient colliders do not create repair rejection feedback");
            var wrongCollider=wrong.gameObject.AddComponent<BoxCollider>();
            Trigger("OnTriggerEnter",wrongCollider);
            Check(intake.LastMessage!=null&&intake.LastMessage.Contains("not a RepairKit"),"actual wrong crafted item still reports rejection");
            Set(intake,"feedbackExpires",Time.unscaledTime-1f);Trigger("OnTriggerStay",wrongCollider);
            Check(intake.LastMessage==null&&ledger.repairKits==1,"unchanged overlap does not refresh expired wrong-item rejection");
            intake.feedbackText=Make("Receiver feedback fixture").AddComponent<TextMeshPro>();
            intake.feedbackText.text="expired feedback";
            typeof(DEF_WorkshopRepairReceiver).GetMethod("Update",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(intake,null);
            Check(intake.feedbackText.text==string.Empty,"expired optional TMP feedback clears on Update");
            Trigger("OnTriggerExit",wrongCollider);Trigger("OnTriggerEnter",wrongCollider);
            Check(intake.LastMessage!=null,"new presentation after exit reports rejection again");
            Trigger("OnTriggerExit",wrongCollider);
            var stale=Kit(ledger);
            ledger.enabled=false;Check(!intake.TryDeliver(stale)&&ledger.repairKits==1&&stale!=null,"offline ledger preserves kit and target");ledger.enabled=true;
            turret.enabled=false;Check(!intake.TryDeliver(stale)&&ledger.repairKits==1,"offline target preserves kit");turret.enabled=true;
            float preciseHealth=100000000f;Check(!ledger.TryRepairPhysical(stale,turret,ref preciseHealth,200000000f,0.001f,out _)&&ledger.repairKits==1,"unrepresentable HP increase rejects without consuming kit");
            ledger.ResetRepairInventory(1);
            Check(!intake.TryDeliver(stale)&&stale!=null&&ledger.repairKits==1,"reset invalidates old kit identity");
            var missing=Kit(ledger);UnityEngine.Object.DestroyImmediate(missing.gameObject);
            Check(DEF_RepairDelivery.TryRepair(ledger,null,turret,13,false,out _,out _)&&turret.health==63&&ledger.repairKits==0,"remote missing-representation repair preserves configured amount");
            Check(!DEF_RepairDelivery.TryRepair(ledger,null,turret,13,false,out _,out _)&&turret.health==63,"repeated remote action cannot spend absent unit");
            ledger.ResetRepairInventory(1);var remote=Kit(ledger);
            Check(DEF_RepairDelivery.TryRepair(ledger,null,turret,10,false,out _,out _)&&remote==null&&wrongLedger!=null&&other.repairKits==1,"remote retires only source ledger kit not another area");
            var barrier=Make("Barricade target").AddComponent<DEF_Barricade>();barrier.health=20;barrier.repairAmount=17;intake.target=barrier;
            ledger.ResetRepairInventory(1);Check(intake.TryDeliver(Kit(ledger))&&barrier.health==37,"physical barricade preserves target-specific repair amount");
            var trap=Make("Shock target").AddComponent<DEF_ShockTrap>();trap.health=20;intake.target=trap;
            ledger.ResetRepairInventory(1);Check(intake.TryDeliver(Kit(ledger))&&trap.health==60,"physical shock repair supported");
            ledger.ResetRepairInventory(1);var orphan=Kit(ledger);UnityEngine.Object.DestroyImmediate(trap.gameObject);
            Check(!intake.TryDeliver(orphan)&&ledger.repairKits==1&&orphan!=null,"destroyed target rejects before consumption");
            intake.target=turret;turret.health=20;
            bool blocked=false,later=false;Action reenter=()=>{blocked=!competitor.TryDeliver(orphan)&&!ledger.SpendRepairKit(1)&&!ledger.ResetRepairInventory(9);};
            Action throws=()=>throw new Exception("intentional repair test listener");Action tail=()=>later=true;
            ledger.OnResourcesChanged+=reenter;ledger.OnResourcesChanged+=throws;ledger.OnResourcesChanged+=tail;
            Check(intake.TryDeliver(orphan)&&blocked&&later&&ledger.repairKits==0&&turret.health==60,"reentry blocked and notification exceptions isolated after repair commit");
            ledger.OnResourcesChanged-=reenter;ledger.OnResourcesChanged-=throws;ledger.OnResourcesChanged-=tail;
            ledger.ResetRepairInventory(1);var held=Kit(ledger);var grab=held.gameObject.AddComponent<XRGrabInteractable>();typeof(XRGrabInteractable).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(grab,null);
            var manager=Make("Repair XRI manager").AddComponent<XRInteractionManager>();var hand=Make("Repair interactor").AddComponent<XRDirectInteractor>();typeof(XRDirectInteractor).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(hand,null);
            hand.interactionManager=manager;grab.interactionManager=manager;manager.RegisterInteractor((IXRInteractor)hand);manager.RegisterInteractable((IXRInteractable)grab);
            manager.SelectEnter((IXRSelectInteractor)hand,(IXRSelectInteractable)grab);
            Check(grab.isSelected&&!intake.TryDeliver(held)&&!DEF_RepairDelivery.TryRepair(ledger,null,turret,10,false,out _,out _)&&ledger.repairKits==1,"held kit excluded from physical and remote transfers (XRI lifecycle fixture)");
            Trigger("OnTriggerEnter",held.GetComponent<BoxCollider>());
            Check(ledger.repairKits==1&&held!=null,"held overlap does not consume a kit");
            manager.SelectExit((IXRSelectInteractor)hand,(IXRSelectInteractable)grab);
            Trigger("OnTriggerStay",held.GetComponent<BoxCollider>());
            Check(held==null&&ledger.repairKits==0&&turret.health==100,"release while overlapping retries and repairs once");
            ledger.ResetRepairInventory(1);turret.health=50;var multi=Kit(ledger);var child=Make("second kit collider");child.transform.SetParent(multi.transform,false);var c2=child.AddComponent<BoxCollider>();
            typeof(DEF_WorkshopRepairReceiver).GetMethod("OnTriggerEnter",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(intake,new object[]{multi.GetComponent<BoxCollider>()});typeof(DEF_WorkshopRepairReceiver).GetMethod("OnTriggerStay",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(intake,new object[]{c2});
            Check(ledger.repairKits==0&&turret.health==90,"duplicate collider callback entry/stay consumes one kit (not a physics simulation)");
            var panel=Make("Workshop repair panel").AddComponent<SCV_DamagedPanel>();Set(panel,"resourceManager",ledger);
            var teaching=Make("Workshop repair teaching receiver").AddComponent<SCV_RepairReceiver>();Set(teaching,"damagedPanel",panel);int teachingReceipts=0;teaching.PhysicalRepairAccepted+=id=>teachingReceipts++;
            ledger.ResetRepairInventory(2);var teachKit=Kit(ledger);var remoteKit=Kit(ledger);
            Check(teaching.TryReceive(teachKit)&&panel.CurrentCondition==70&&teachingReceipts==1,"Workshop teaching publishes actual physical repair receipt");
            Check(panel.TryRepair()&&panel.CurrentCondition==100&&teachingReceipts==1&&remoteKit==null,"Workshop remote repair preserves teaching distinction and exact representation");
            var link=Make("Remote repair showcase terminal").AddComponent<SCV_DefenseSupplyLink>();
            Set(link,"resourceManager",ledger);Set(link,"repairAmount",9f);link.transform.position=new Vector3(1000,0,1000);turret.transform.position=link.transform.position;turret.health=50;
            ledger.ResetRepairInventory(1);var linkKit=Kit(ledger);link.RepairNearestDefense();
            Check(turret.health==59&&linkKit==null&&ledger.repairKits==0,"showcase remote button preserves terminal range and serialized repair amount");
            link.RepairNearestDefense();Check(turret.health==59&&ledger.repairKits==0,"repeated showcase button cannot double consume its unit");
            // Production progression fixture: only designated physical receiver may advance repair.
            var gate=state.gameObject.AddComponent<DEF_Progression>();var presenter=Make("Repair presenter").AddComponent<DEF_ProgressionPresenter>();
            presenter.introductionText=Make("Repair intro").AddComponent<TextMeshPro>();presenter.goalText=Make("Repair goal").AddComponent<TextMeshPro>();
            presenter.failureText=Make("Transient failure").AddComponent<TextMeshPro>();
            Set(gate,"presenter",presenter);Set(gate,"repairLedger",ledger);
            presenter.Present(gate);presenter.ShowFailure("test failure");Set(presenter,"failureExpires",Time.unscaledTime-1f);
            presenter.ShowFailure("test failure");typeof(DEF_ProgressionPresenter).GetMethod("Update",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(presenter,null);
            Check(presenter.failureText.text==string.Empty,"global repeated failure expires without refreshing its display");
            Set(presenter,"lastFailureCall",Time.unscaledTime-1f);presenter.ShowFailure("test failure");
            Check(presenter.failureText.text=="test failure","same explicit failure displays again after a quiet gap");
            presenter.ShowFailure("different failure");Check(presenter.failureText.text=="different failure","distinct global failure remains visible");
            gate.State.AcceptPrerequisite(DEF_ProgressionState.Prerequisite.Power6,1,"p");gate.State.AcceptPrerequisite(DEF_ProgressionState.Prerequisite.Clinic10,1,"c");gate.State.AcceptPrerequisite(DEF_ProgressionState.Prerequisite.Workshop14,1,"w");
            typeof(DEF_Progression).GetMethod("Placed",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(gate,new object[]{turret});
            gate.State.CompleteIntroduction(1,1,gate.State.Introduction);
            Check(!turret.TryRepair()&&gate.State.Current==DEF_ProgressionState.Stage.Repair,"scrap repair cannot satisfy configured physical-kit progression");
            var designated=turret.GetComponentInChildren<DEF_WorkshopRepairReceiver>();ledger.ResetRepairInventory(1);
            Check(designated.TryDeliver(Kit(ledger))&&gate.State.Current==DEF_ProgressionState.Stage.AmmoIntroduction,"designated exact physical kit positive repair advances to ammo introduction");
            Check(presenter.failureText.text==string.Empty,"successful progression clears the old global failure display");
        }
        catch(Exception error){results.Add("FAIL exception "+error);}
        finally{foreach(var go in roots)if(go!=null)UnityEngine.Object.DestroyImmediate(go);DEF_GameState.I=old;UnityEditor.SceneManagement.EditorSceneManager.CloseScene(testScene,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(previousScene);System.IO.File.WriteAllLines("../repair-editmode-results.txt",results);Debug.Log(string.Join("\n",results));}
    }
}
