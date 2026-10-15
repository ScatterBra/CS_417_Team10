using TMPro;
using UnityEngine;

public class SCV_CrusherDisplay : MonoBehaviour
{
    [SerializeField] private SCV_Crusher crusher;
    [SerializeField] private SCV_ResourceManager resourceManager;
    [SerializeField] private TMP_Text statusText;

    private void Update()
    {
        if (crusher.IsProcessing)
        {
            statusText.text =
                "CRUSHER\n" +
                "PROCESSING...";
        }
        else if (resourceManager.scrap < 2)
        {
            statusText.text =
                "CRUSHER\n" +
                "NEED 2 SCRAP";
        }
        else
        {
            statusText.text =
                "CRUSHER\n" +
                "READY\n" +
                "2 SCRAP -> 1 PART";
        }
    }
}