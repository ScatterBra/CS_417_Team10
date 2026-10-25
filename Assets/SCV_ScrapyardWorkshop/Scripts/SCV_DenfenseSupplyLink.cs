using UnityEngine;
using TMPro;

public class SCV_DefenseSupplyLink : MonoBehaviour
{
    [SerializeField] private SCV_ResourceManager resourceManager;

    [Header("Ammo Delivery")]
    [SerializeField] private int defenseAmmoPerBox = 10;

    [Header("Scrap Delivery")]
    [SerializeField] private int scrapTransferAmount = 10;

    [Header("Repair")]
    [SerializeField] private float repairAmount = 40f;
    [SerializeField] private float repairRange = 8f;

    [Header("Feedback")]
    [SerializeField] private TMP_Text feedbackText;
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

    // ---------------------------------------------------------
    // CONNECTION #1
    // SCV crafted Ammo Box -> Defense Yard ammo
    // ---------------------------------------------------------
    public void DeliverAmmo()
    {
        DEF_GameState state = DEF_GameState.I;

        if (state == null)
        {
            SetFeedback(
                "<color=#FF4444>DEFENSE SYSTEM OFFLINE</color>"
            );

            return;
        }

        if (state.ammo >= state.maxAmmo)
        {
            SetFeedback(
                "<color=#FF4444>DEFENSE AMMO FULL\n" +
                "USE AMMO BEFORE DELIVERING MORE</color>"
            );

            return;
        }

        // Find and reserve the exact physical Ammo Box
        // belonging to this Workshop inventory.
        SCV_CraftedItem ammoBox =
            ReservePhysicalItem(
                SCV_CraftedItem.ItemType.Ammo
            );

        if (ammoBox == null)
        {
            SetFeedback(
                "<color=#FF4444>NO AVAILABLE AMMO BOX\n" +
                "CRAFT ONE AT THE WORKBENCH</color>"
            );

            return;
        }

        // Spend the matching inventory resource.
        if (!resourceManager.SpendAmmo(1))
        {
            ammoBox.ReleaseReservation();

            SetFeedback(
                "<color=#FF4444>NO CRAFTED AMMO\n" +
                "CRAFT AMMO AT THE WORKBENCH</color>"
            );

            return;
        }

        int amountAdded = Mathf.Min(
            defenseAmmoPerBox,
            state.maxAmmo - state.ammo
        );

        state.ammo += amountAdded;

        // Destroy the SAME physical box that was reserved.
        DestroyPhysicalItem(ammoBox);

        SetFeedback(
            "<color=#66FF66>1 AMMO BOX DELIVERED\n+" +
            amountAdded +
            " DEFENSE AMMO</color>"
        );

        Debug.Log(
            "SCV Ammo supplied Defense Yard: +" +
            amountAdded
        );
    }

    // ---------------------------------------------------------
    // CONNECTION #2
    // SCV harvested Scrap -> Defense Yard Scrap
    // ---------------------------------------------------------
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
                "NEED " +
                scrapTransferAmount +
                " SCRAP</color>"
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

    // ---------------------------------------------------------
    // CONNECTION #3
    // SCV Repair Kit -> damaged Defense Yard prefab
    // ---------------------------------------------------------
    public void RepairNearestDefense()
    {
        GameObject target =
            FindNearestDamagedDefense();

        if (target == null)
        {
            SetFeedback(
                "<color=#FF4444>NO DAMAGED DEFENSE NEARBY\n" +
                "MOVE CLOSER TO A DAMAGED DEFENSE</color>"
            );

            return;
        }

        // Reserve the exact physical Repair Kit belonging
        // to this Workshop inventory.
        SCV_CraftedItem repairKit =
            ReservePhysicalItem(
                SCV_CraftedItem.ItemType.RepairKit
            );

        if (repairKit == null)
        {
            SetFeedback(
                "<color=#FF4444>NO AVAILABLE REPAIR KIT\n" +
                "CRAFT ONE AT THE WORKBENCH</color>"
            );

            return;
        }

        // Spend the matching Workshop resource.
        if (!resourceManager.SpendRepairKit(1))
        {
            repairKit.ReleaseReservation();

            SetFeedback(
                "<color=#FF4444>NO REPAIR KIT AVAILABLE\n" +
                "CRAFT ONE AT THE WORKBENCH</color>"
            );

            return;
        }

        DEF_Turret turret =
            target.GetComponent<DEF_Turret>();

        if (turret != null)
        {
            turret.health = Mathf.Min(
                turret.health + repairAmount,
                turret.maxHealth
            );

            DEF_Effects.ApplyDamage(
                target,
                turret.health / turret.maxHealth
            );
        }

        DEF_Barricade barricade =
            target.GetComponent<DEF_Barricade>();

        if (barricade != null)
        {
            barricade.health = Mathf.Min(
                barricade.health + repairAmount,
                barricade.maxHealth
            );

            DEF_Effects.ApplyDamage(
                target,
                barricade.health / barricade.maxHealth
            );
        }

        DEF_ShockTrap shockTrap =
            target.GetComponent<DEF_ShockTrap>();

        if (shockTrap != null)
        {
            shockTrap.health = Mathf.Min(
                shockTrap.health + repairAmount,
                shockTrap.maxHealth
            );

            DEF_Effects.ApplyDamage(
                target,
                shockTrap.health / shockTrap.maxHealth
            );
        }

        DEF_HUD.Instance?.RecordRepair();

        DEF_Audio.Play(
            "repair",
            target.transform.position,
            0.6f
        );

        // Destroy the SAME Repair Kit that was reserved.
        DestroyPhysicalItem(repairKit);

        SetFeedback(
            "<color=#66FF66>REPAIR KIT APPLIED\n" +
            "DEFENSE RESTORED</color>"
        );

        Debug.Log(
            "SCV Repair Kit repaired " +
            target.name
        );
    }

    // ---------------------------------------------------------
    // Finds an unreserved physical crafted item that came
    // from THIS Workshop's ResourceManager.
    // ---------------------------------------------------------
    private SCV_CraftedItem ReservePhysicalItem(
        SCV_CraftedItem.ItemType type)
    {
        SCV_CraftedItem[] items =
            FindObjectsByType<SCV_CraftedItem>(
                FindObjectsSortMode.None
            );

        foreach (SCV_CraftedItem item in items)
        {
            if (item == null)
                continue;

            if (item.Type != type)
                continue;

            if (item.SourceInventory != resourceManager)
                continue;

            if (item.TryReserve())
                return item;
        }

        return null;
    }

    // ---------------------------------------------------------
    // Destroy the complete physical prefab.
    // Uses the Rigidbody root when possible.
    // ---------------------------------------------------------
    private void DestroyPhysicalItem(
        SCV_CraftedItem item)
    {
        if (item == null)
            return;

        Rigidbody rb =
            item.GetComponentInParent<Rigidbody>();

        if (rb != null)
        {
            Destroy(rb.gameObject);
        }
        else
        {
            Destroy(item.gameObject);
        }
    }

    // ---------------------------------------------------------
    // Find nearest damaged Daryl defense within repairRange.
    // ---------------------------------------------------------
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
        {
            feedbackText.text =
                FeedbackMessage;
        }
    }
}