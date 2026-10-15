using TMPro;
using UnityEngine;

public class SCV_PartsBinDisplay : MonoBehaviour
{
    [SerializeField] private SCV_ResourceManager resourceManager;
    [SerializeField] private TMP_Text partsText;

    private void Update()
    {
        partsText.text =
            "PARTS BIN\n" +
            "PARTS: " + resourceManager.parts;
    }
}