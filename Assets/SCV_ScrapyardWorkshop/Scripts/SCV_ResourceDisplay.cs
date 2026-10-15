using TMPro;
using UnityEngine;

public class SCV_ResourceDisplay : MonoBehaviour
{
    [SerializeField] private SCV_ResourceManager resourceManager;
    [SerializeField] private TMP_Text resourceText;

    private void Update()
    {
        resourceText.text =
            "SCRAP: " + resourceManager.scrap +
            "\nPARTS: " + resourceManager.parts +
            "\nAMMO: " + resourceManager.ammo +
            "\nREPAIR KITS: " + resourceManager.repairKits;
    }
}