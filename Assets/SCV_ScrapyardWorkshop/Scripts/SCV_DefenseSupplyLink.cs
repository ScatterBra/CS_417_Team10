using UnityEngine;
using TMPro;

public class SCV_DefenseSupplyLink : MonoBehaviour
{
    [SerializeField] private SCV_ResourceManager resourceManager;

    [Header("Ammo Delivery")]
    [SerializeField] private int defenseAmmoPerBox = 10;
    [SerializeField] private DEF_WorkshopAmmoReceiver ammoIntake;

    [SerializeField] private TMP_Text feedbackText;

    [Header("Scrap Delivery")]
    [SerializeField] private int scrapTransferAmount = 10;

    [Header("Repair")]
    [SerializeField] private float repairAmount = 40f;
    [SerializeField] private float repairRange = 8f;

    [Header("Feedback")]
    [SerializeField] private float feedbackDuration = 3f;

    private string feedbackMessage = "";
    private float feedbackEndTime = -1f;

    public string FeedbackMessage
    {
        get
        {
            if (Time.time <= feedbackEndTime)
                return feedbackMessage;

            return "DEFENSE SUPPLY LINK READY";
        }
    }

    private void SetFeedback(string message)
    {
        feedbackMessage = message;
        feedbackEndTime = Time.time + feedbackDuration;
    }

    // CONNECTION #1
    // SCV crafted Ammo -> Defense Yard ammo
    public void DeliverAmmo()
    {
        if (!DEF_Progression.Allows(DEF_Action.RemoteAmmo)) { SetFeedback(DEF_Progression.Current.LastFailure); return; }
        var state = DEF_GameState.I;
        if (state == null || !state.isActiveAndEnabled)
        { SetFeedback("Cannot remotely deliver ammo: Defense is offline. Restore the Defense connection and retry."); return; }
        if (resourceManager == null || !resourceManager.isActiveAndEnabled)
        { SetFeedback("Cannot remotely deliver ammo: Workshop ledger is offline. Restore its connection and retry."); return; }
        if (defenseAmmoPerBox <= 0 || state.ammo < 0 || state.maxAmmo <= 0)
        { SetFeedback("Cannot remotely deliver ammo: transfer settings are invalid. Correct amount/capacity and retry."); return; }
        if (state.ammo >= state.maxAmmo)
        { SetFeedback("Cannot remotely deliver ammo: Defense is full. Use Defense ammo before retrying."); return; }
        if (resourceManager.ammo < 1)
        { SetFeedback("Cannot remotely deliver ammo: no Workshop ammo remains. Craft ammo at the workbench."); return; }
        if (!resourceManager.TryTransferRemoteAmmo(ref state.ammo, state.maxAmmo, defenseAmmoPerBox, out int added))
        { SetFeedback("Cannot remotely deliver ammo: inventory is busy or its boxes are held/inactive. Release a box or finish the current delivery, then retry."); return; }
        SetFeedback("Remote ammo delivered: +" + added + " Defense rounds; 1 Workshop inventory unit consumed.");
    }
    // Optional physical confirmation is distinct from the existing remote button.
    public void DeliverPresentedAmmo()
    {
        if (ammoIntake == null)
        { SetFeedback("Cannot physically deliver ammo: intake is not connected. Connect the Defense intake and present a crafted box."); return; }
        ammoIntake.DeliverPresented();
        SetFeedback(ammoIntake.LastMessage);
    }
    // CONNECTION #2
    // SCV harvested Scrap -> Defense Yard building Scrap
    public void DeliverScrap()
    {
        DEF_GameState state = DEF_GameState.I;

        if (state == null)
        {
            SetFeedback(
                "<color=#FF4444>DEFENSE SYSTEM OFFLINE</color>"
            );
            return;
        }

        if (state.scrap + scrapTransferAmount > state.maxScrap)
        {
            SetFeedback(
                "<color=#FF4444>DEFENSE SCRAP STORAGE FULL</color>"
            );
            return;
        }

        if (!resourceManager.SpendScrap(scrapTransferAmount))
        {
            SetFeedback(
                "<color=#FF4444>NOT ENOUGH WORKSHOP SCRAP\n" +
                "NEED " + scrapTransferAmount + " SCRAP</color>"
            );
            return;
        }

        state.scrap += scrapTransferAmount;

        SetFeedback(
            "<color=#66FF66>SCRAP DELIVERED\n+" +
            scrapTransferAmount +
            " DEFENSE SCRAP</color>"
        );

        Debug.Log(
            "SCV Scrap supplied Defense Yard: +" +
            scrapTransferAmount
        );
    }

    // CONNECTION #3
    // SCV Repair Kit -> Daryl defense prefab
    public void RepairNearestDefense()
    {
        if (DEF_Progression.Current != null)
        { SetFeedback("Cannot remotely repair in this training scene. Deliver a physical RepairKit to the damaged defense."); return; }
        var found = FindNearestDamagedDefense();
        if (found == null)
        { SetFeedback("Cannot remotely repair: no damaged defense is within the terminal's repair range. Place or repair a damaged defense within that range."); return; }
        MonoBehaviour target = found.GetComponent<DEF_Turret>();
        if(target==null)target=found.GetComponent<DEF_Barricade>();
        if(target==null)target=found.GetComponent<DEF_ShockTrap>();
        if(!DEF_RepairDelivery.TryRepair(resourceManager,null,target,repairAmount,false,out float before,out float after))
        { SetFeedback("Cannot remotely repair: target or kit inventory is unavailable, full, held or busy. Release a kit, craft if needed, and use a live damaged target."); return; }
        DEF_HUD.Instance?.RecordRepair();
        SetFeedback("Remote repair applied: +"+(after-before)+" integrity; one Workshop RepairKit consumed.");
    }
    private GameObject FindNearestDamagedDefense()
    {
        GameObject closest = null;
        float closestDistance = repairRange;

        foreach (
            DEF_Turret turret in
            FindObjectsByType<DEF_Turret>(
                FindObjectsSortMode.None
            )
        )
        {
            if (turret.health >= turret.maxHealth)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                turret.transform.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = turret.gameObject;
            }
        }

        foreach (
            DEF_Barricade barricade in
            FindObjectsByType<DEF_Barricade>(
                FindObjectsSortMode.None
            )
        )
        {
            if (barricade.health >= barricade.maxHealth)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                barricade.transform.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = barricade.gameObject;
            }
        }

        foreach (
            DEF_ShockTrap trap in
            FindObjectsByType<DEF_ShockTrap>(
                FindObjectsSortMode.None
            )
        )
        {
            if (trap.health >= trap.maxHealth)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                trap.transform.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = trap.gameObject;
            }
        }

        return closest;
    }

    private void Update()
    {
        if (feedbackText != null)
            feedbackText.text = FeedbackMessage;
    }
}
