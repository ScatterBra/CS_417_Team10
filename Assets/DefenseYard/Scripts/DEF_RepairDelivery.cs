using UnityEngine;

public static class DEF_RepairDelivery
{
    public static float AmountFor(MonoBehaviour target) => target is DEF_Barricade barrier ? barrier.repairAmount : 40f;
    public static bool TryRepair(SCV_ResourceManager ledger,SCV_CraftedItem kit,MonoBehaviour target,
        float amount,bool physical,out float before,out float after)
    {
        before=after=0;
        var gate=DEF_Progression.Current;
        if(gate!=null)
        {
            if(!physical)return gate.Reject("repair remotely", "physical kit delivery is required", "Release a crafted RepairKit at the damaged defense receiver.");
            if(!gate.Check(DEF_Action.Repair))return false;
            var action=target is DEF_ShockTrap?DEF_Action.Shock:target is DEF_Barricade?DEF_Action.Barricade:target is DEF_Turret t && t.explosive?DEF_Action.Missile:DEF_Action.BasicSentry;
            if(!gate.Check(action))return false;
        }
        if(ledger==null || target==null || !target.isActiveAndEnabled)return false;
        float delta;bool ok;
        if(target is DEF_Turret turret)
        {
            before=turret.health;
            ok=physical ? ledger.TryRepairPhysical(kit,turret,ref turret.turretIntegrity,turret.maxHealth,amount,out delta):
                ledger.TryRepairRemote(turret,ref turret.turretIntegrity,turret.maxHealth,amount,out delta);
        }
        else if(target is DEF_Barricade barrier)
        {
            before=barrier.health;
            ok=physical ? ledger.TryRepairPhysical(kit,barrier,ref barrier.barricadeIntegrity,barrier.maxHealth,amount,out delta):
                ledger.TryRepairRemote(barrier,ref barrier.barricadeIntegrity,barrier.maxHealth,amount,out delta);
        }
        else if(target is DEF_ShockTrap trap)
        {
            before=trap.health;
            ok=physical ? ledger.TryRepairPhysical(kit,trap,ref trap.health,trap.maxHealth,amount,out delta):
                ledger.TryRepairRemote(trap,ref trap.health,trap.maxHealth,amount,out delta);
        }
        else return false;
        after=before+(ok?delta:0);
        // Gameplay receipt uses committed HP values. Optional audiovisual feedback cannot undo inventory.
        if(ok && target!=null)
        {
            try
            {
                float maximum=target is DEF_Turret t?t.maxHealth:target is DEF_Barricade b?b.maxHealth:((DEF_ShockTrap)target).maxHealth;
                DEF_Effects.ApplyDamage(target.gameObject,after/maximum);
                if(Application.isPlaying)DEF_Audio.Play("repair",target.transform.position,.6f);
            }
            catch(System.Exception error){Debug.LogWarning("Repair committed; presentation failed: "+error.Message);}
        }
        return ok;
    }
}
