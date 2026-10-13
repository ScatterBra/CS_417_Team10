using TMPro;
using UnityEngine;

public class SCV_DamagedPanelDisplay : MonoBehaviour
{
    [SerializeField] private SCV_DamagedPanel damagedPanel;
    [SerializeField] private TMP_Text conditionText;

    private void Update()
    {
        conditionText.text =
            "DEFENSE PANEL\n" +
            "CONDITION: " +
            damagedPanel.CurrentCondition + "/" +
            damagedPanel.MaxCondition;
    }
}