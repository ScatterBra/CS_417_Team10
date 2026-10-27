using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class DEF_RouteInspection
{
    [MenuItem("Defense Yard/MP2B/Inspect Station Route Geometry")]
    public static void Inspect()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode only");
        var scene=SceneManager.GetActiveScene();
        if(!scene.name.StartsWith("DEF_"))throw new Exception("Open isolated Defense integration scene");
        var report=new StringBuilder("Scene: "+scene.path+"\nWorld-space collider geometry; not a traversal pass.\n");
        foreach(var c in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Collider>(true)))
        {
            if(!c.enabled||!c.gameObject.activeInHierarchy||c.isTrigger)continue;
            var b=c.bounds;
            if(b.size.x<2 && b.size.z<2)continue;
            report.AppendLine($"{c.name} layer={c.gameObject.layer} min={b.min:F3} max={b.max:F3} type={c.GetType().Name}");
        }
        foreach(var t in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.IndexOf("teleport",StringComparison.OrdinalIgnoreCase)>=0))
            report.AppendLine($"TELEPORT {t.name} position={t.position:F3}");
        File.WriteAllText("../station-route-geometry.txt",report.ToString());
        Debug.Log("Station route bounds written to ../station-route-geometry.txt; no scene changes.");
    }
}
