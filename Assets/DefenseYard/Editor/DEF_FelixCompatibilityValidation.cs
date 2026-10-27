using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class DEF_FelixCompatibilityValidation
{
    public static void Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode only");
        var results=new List<string>();var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        void Check(bool ok,string name)=>results.Add((ok?"PASS ":"FAIL ")+name);
        void Set(object o,string f,object v)=>o.GetType().GetField(f,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
        SCV_CraftedItem Kit(SCV_ResourceManager ledger,bool child=false)
        {
            var root=new GameObject("Compatibility kit");root.AddComponent<Rigidbody>().isKinematic=true;
            var obj=child?new GameObject("Nested marker"):root;if(child)obj.transform.SetParent(root.transform);
            var item=obj.AddComponent<SCV_CraftedItem>();Set(item,"itemType",SCV_CraftedItem.ItemType.RepairKit);item.SetSourceInventory(ledger);return item;
        }
        try
        {
            var ledger=new GameObject("Ledger").AddComponent<SCV_ResourceManager>();ledger.ResetRepairInventory(2);
            var other=new GameObject("Other ledger").AddComponent<SCV_ResourceManager>();other.ResetRepairInventory(1);
            var crusher=new GameObject("Crusher").AddComponent<SCV_Crusher>();Set(crusher,"resourceManager",ledger);
            var first=Kit(ledger);var exact=Kit(ledger,true);var root=exact.GetComponentInParent<Rigidbody>().gameObject;
            Check(exact.SourceInventory==ledger&&exact.SourceLedger==ledger&&exact.UnitId!=0,"Felix source API maps to ledger-owned identity");
            Check(exact.TryReserve()&&!exact.TryReserve()&&!crusher.TryRepairPhysical(exact)&&ledger.repairKits==2,"Felix reservation excludes transaction competitors");
            exact.ReleaseReservation();Set(crusher,"crusherCondition",0);
            Check(crusher.TryRepairPhysical(exact)&&crusher.CrusherCondition==30&&ledger.repairKits==1&&root==null&&first!=null,"broken crusher consumes exact nested kit root and preserves unrelated representation");
            Check(!crusher.TryRepairPhysical(exact)&&ledger.repairKits==1,"repeat crusher physical delivery cannot double consume");
            var foreign=Kit(other);Check(!crusher.TryRepairPhysical(foreign)&&other.repairKits==1&&ledger.repairKits==1,"crusher rejects foreign source without spending either ledger");
            Set(crusher,"crusherCondition",crusher.MaxCrusherCondition);Check(!crusher.TryRepairPhysical(first)&&first!=null&&ledger.repairKits==1,"full crusher preserves physical kit and ledger");
            Set(crusher,"crusherCondition",70);Check(crusher.TryRepair()&&crusher.CrusherCondition==100&&ledger.repairKits==0&&first==null,"crusher remote path uses same exact representation lifecycle");
            ledger.ResetRepairInventory(1);var prefab=new GameObject("Nested prefab fixture");var nested=new GameObject("Marker");nested.transform.SetParent(prefab.transform);var marker=nested.AddComponent<SCV_CraftedItem>();Set(marker,"itemType",SCV_CraftedItem.ItemType.RepairKit);
            var bench=new GameObject("Workbench").AddComponent<SCV_Workbench>();Set(bench,"resourceManager",ledger);SCV_CraftedItem receipt=null;bench.PhysicalItemCrafted+=item=>receipt=item;
            typeof(SCV_Workbench).GetMethod("SpawnItem",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(bench,new object[]{prefab,bench.transform});
            Check(receipt!=null&&receipt.SourceInventory==ledger&&receipt.UnitId!=0,"workbench discovers nested marker and emits bound physical craft receipt");
        }
        catch(Exception error){results.Add("FAIL exception "+error);}
        finally {EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);System.IO.File.WriteAllLines("../felix-compat-editmode-results.txt",results);Debug.Log(string.Join("\n",results));}
    }
}
