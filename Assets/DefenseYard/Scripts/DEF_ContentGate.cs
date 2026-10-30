using UnityEngine;
using System.Collections.Generic;

// Keeps preplaced Defense-owned presentation/interaction hidden without disabling the observer.
[DefaultExecutionOrder(10000)]
public sealed class DEF_ContentGate : MonoBehaviour
{
    readonly Dictionary<Renderer,bool> rendering = new();
    readonly Dictionary<Collider,bool> collision = new();
    readonly Dictionary<Canvas,bool> canvases = new();
    public bool Visible
    {
        get
        {
            var p=DEF_Progression.Current;if(p==null)return true;
            if(TryGetComponent(out DEF_WorkshopRepairReceiver _))return p.IsAllowed(DEF_Action.Repair);
            // Keep a learned intake findable after failure; receiver still validates permission before spending.
            if(TryGetComponent(out DEF_WorkshopAmmoReceiver _))return p.State.Current>=DEF_ProgressionState.Stage.Ammo;
            if(TryGetComponent(out DEF_AmmoCrate _))return p.IsAllowed(DEF_Action.AmmoCrate);
            if(TryGetComponent(out DEF_ShockTrap _))return p.IsAllowed(DEF_Action.Shock);
            if(TryGetComponent(out DEF_Barricade _))return p.IsAllowed(DEF_Action.Barricade);
            if(TryGetComponent(out DEF_Turret turret))return p.IsAllowed(turret.explosive?DEF_Action.Missile:DEF_Action.BasicSentry);
            return false;
        }
    }
    void LateUpdate(){Apply();}
    public void Apply()
    {
        bool show=Visible;
        foreach(var r in GetComponentsInChildren<Renderer>(true))
        {
            if(r.GetComponentInParent<DEF_ContentGate>()!=this)continue;
            if(!show){if(!rendering.ContainsKey(r))rendering[r]=r.forceRenderingOff;r.forceRenderingOff=true;}
        }
        foreach(var c in GetComponentsInChildren<Collider>(true))
        {
            if(c.GetComponentInParent<DEF_ContentGate>()!=this)continue;
            if(!show){if(!collision.ContainsKey(c))collision[c]=c.enabled;c.enabled=false;}
        }
        foreach(var c in GetComponentsInChildren<Canvas>(true))
        {
            if(c.GetComponentInParent<DEF_ContentGate>()!=this)continue;
            if(!show){if(!canvases.ContainsKey(c))canvases[c]=c.enabled;c.enabled=false;}
        }
        if(show)Restore();
    }
    void Restore()
    {
        foreach(var pair in rendering)if(pair.Key!=null)pair.Key.forceRenderingOff=pair.Value;
        foreach(var pair in collision)if(pair.Key!=null)pair.Key.enabled=pair.Value;
        foreach(var pair in canvases)if(pair.Key!=null)pair.Key.enabled=pair.Value;
        rendering.Clear();collision.Clear();canvases.Clear();
    }
    void OnDisable(){Restore();}
}
