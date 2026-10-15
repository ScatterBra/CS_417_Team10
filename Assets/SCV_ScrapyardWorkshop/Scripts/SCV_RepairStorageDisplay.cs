using TMPro;
using UnityEngine;

public class SCV_RepairStorageDisplay : MonoBehaviour
{
    [SerializeField] private SCV_ResourceManager resourceManager;
    [SerializeField] private TMP_Text repairText;

    private void Update()
    {
        repairText.text =
            "REPAIR STORAGE\n" +
            "KITS: " + resourceManager.repairKits;
    }
}