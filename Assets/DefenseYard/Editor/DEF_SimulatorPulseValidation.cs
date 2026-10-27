using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class DEF_SimulatorPulseValidation
{
    [MenuItem("Defense Yard/MP2B/Validate Simulator Pulse Cancellation")]
    public static void Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode only");
        var panel=ScriptableObject.CreateInstance<DEF_SimulatorInputPanel>();
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;var type=panel.GetType();
        object Get(string n)=>type.GetField(n,flags).GetValue(panel);
        void Set(string n,object value)=>type.GetField(n,flags).SetValue(panel,value);
        var lines=new System.Collections.Generic.List<string>();
        void Check(bool ok,string n){lines.Add((ok?"PASS ":"FAIL ")+n);}
        try
        {
            Check((int)Get("movementDurationIndex")==0,"default duration is half second");
            Check(string.Join(",",(double[])Get("movementDurations"))=="0.5,2,5","bounded duration choices");
            int called=0;Set("movementPending",true);Set("movementActive",true);Set("pendingInput",(Action)(()=>called++));
            type.GetMethod("CancelMovement",flags).Invoke(panel,null);
            Check(!(bool)Get("movementPending")&&!(bool)Get("movementActive"),"cancel clears active and pending movement");
            Check(Get("pendingInput")==null&&called==0,"cancel cannot execute deferred movement");
            type.GetMethod("CancelMovement",flags).Invoke(panel,null);
            Check(Get("pendingInput")==null&&called==0,"repeated cancel cannot restart movement");
        }
        finally{UnityEngine.Object.DestroyImmediate(panel);}
        File.WriteAllLines("../simulator-pulse-validation.txt",lines);Debug.Log(string.Join("\n",lines));
        if(lines.Exists(x=>x.StartsWith("FAIL")))throw new Exception("Pulse checks failed");
    }
}
