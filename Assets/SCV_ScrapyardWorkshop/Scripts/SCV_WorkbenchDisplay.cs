using TMPro;
using UnityEngine;

public class SCV_WorkbenchDisplay : MonoBehaviour
{
    [SerializeField] private SCV_Workbench workbench;
    [SerializeField] private TMP_Text statusText;

    private void Update()
    {
        statusText.text =
            "WORKBENCH\n\n" +
            "AMMO\n" +
            workbench.AmmoPartsCost + " PART -> " +
            workbench.AmmoProduced + " AMMO\n\n" +

            "REPAIR KIT\n" +
            workbench.RepairPartsCost + " PARTS -> " +
            workbench.RepairKitsProduced + " KIT\n\n" +

            workbench.LastMessage;
    }
}