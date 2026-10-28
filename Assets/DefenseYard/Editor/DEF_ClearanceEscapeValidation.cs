using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Native geometry checks; these do not grant progression or replace live traversal.
public static class DEF_ClearanceEscapeValidation
{
    [MenuItem("Defense Yard/MP2B/Validate Barricade Clearance Escape")]
    public static void Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode only");
        var previousScene=SceneManager.GetActiveScene();var previousPathfinder=DEF_Pathfinder.I;
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var results=new List<string>();int failures=0;
        void Check(bool ok,string name){results.Add((ok?"PASS ":"FAIL ")+name);if(!ok)failures++;}
        try
        {
            var grid=new GameObject("Clearance test grid").AddComponent<DEF_GridManager>();grid.width=grid.depth=20;grid.tileSize=2f;
            var pathfinder=new GameObject("Clearance test pathfinder").AddComponent<DEF_Pathfinder>();
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(DEF_Pathfinder).GetField("grid",flags).SetValue(pathfinder,grid);
            typeof(DEF_Pathfinder).GetField("blocked",flags).SetValue(pathfinder,new bool[20,20]);
            typeof(DEF_Pathfinder).GetField("ready",flags).SetValue(pathfinder,true);
            var center=grid.TileCenter(6,6);pathfinder.SetBlocked(6,6,true);
            Check(new Vector2(1.2f,1.2f).magnitude>1.6f,"corner overlap outside center melee range");
            foreach(float radius in new[]{.35f,.43f})foreach(float sign in new[]{-1f,1f})
            {
                Vector3 P(float x,float z)=>center+new Vector3(x*sign,0,z*sign);
                bool Clear(float ax,float az,float bx,float bz)=>pathfinder.CorridorClear(P(ax,az),P(bx,bz),radius);
                string label=" radius="+radius+" sign="+sign;
                grid.WorldToTile(P(1.2f,1.2f),out int x,out int z);
                Check(x!=6&&z!=6,"corner outside raw cell"+label);
                Check(Clear(1.2f,1.2f,1.22f,1.22f),"small outward corner step"+label);
                Check(Clear(1.2f,0,1.22f,0),"small outward side step"+label);
                Check(Clear(0,1.2f,0,1.22f),"small outward z step"+label);
                Check(Clear(1.2f,1.2f,2,2),"outward corner exit"+label);
                Check(!Clear(1.2f,1.2f,1.18f,1.18f),"inward corner rejected"+label);
                Check(!Clear(1.2f,0,1.18f,0),"inward side rejected"+label);
                Check(!Clear(1.2f,1.2f,1.22f,1.18f),"mixed inward corner rejected"+label);
                Check(!Clear(1.2f,0,1.2f,.02f),"tangential-only rejected"+label);
                Check(!Clear(1.2f,1.2f,1.2f,1.2f),"stationary rejected"+label);
                Check(!Clear(1.2f,1.2f,0,0),"raw cell entry rejected"+label);
                Check(!Clear(1.2f,1.2f,-2,-2),"raw cell crossing rejected"+label);
                Check(Clear(1.6f,0,1.5f,0),"normal clear unchanged"+label);
                Check(!Clear(1.6f,0,1.2f,0),"new margin entry rejected"+label);
                Check(!Clear(-2,0,2,0),"normal blocked unchanged"+label);
            }
            pathfinder.SetBlocked(8,7,true);
            Check(!pathfinder.CorridorClear(center+new Vector3(1.2f,0,1.2f),center+new Vector3(6,0,2),.43f),"second occupied cell still blocks");
            pathfinder.SetBlocked(8,7,false);
            Check(pathfinder.CorridorClear(center+new Vector3(1.2f,0,1.2f),center+new Vector3(6,0,2),.43f),"removed second obstacle clears");
            Check(pathfinder.FindPath(new Vector2Int(7,7),pathfinder.BaseCell()).Count>0,"real alternate grid path remains");
            var walker=new GameObject("Native corner movement fixture").AddComponent<DEF_Zombie>();
            typeof(DEF_Zombie).GetField("pf",flags).SetValue(walker,pathfinder);
            typeof(DEF_Zombie).GetField("grid",flags).SetValue(walker,grid);
            walker.transform.position=center+new Vector3(1.2f,0,1.2f);
            walker.transform.rotation=Quaternion.LookRotation(new Vector3(1,0,1));
            var start=walker.transform.position;
            var move=typeof(DEF_Zombie).GetMethod("MoveToward",flags);
            for(int i=0;i<30;i++)move.Invoke(walker,new object[]{center+new Vector3(3,0,3),.02f});
            Check(Vector3.Distance(start,walker.transform.position)>.4f,"actual zombie movement escapes an existing corner clearance overlap");
            Check(typeof(DEF_Zombie).GetField("detourCause",flags).GetValue(walker)==null,"overlap fixture has no barricade cause and cannot manufacture detour evidence");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene,true);DEF_Pathfinder.I=previousPathfinder;SceneManager.SetActiveScene(previousScene);
            System.IO.File.WriteAllLines("../clearance-escape-results.txt",results);Debug.Log(string.Join("\n",results));
        }
        if(failures>0)throw new Exception("Barricade clearance escape checks failed: "+failures);
    }
}
