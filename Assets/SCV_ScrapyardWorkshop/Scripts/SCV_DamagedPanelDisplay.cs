using TMPro;
using UnityEngine;

public class SCV_DamagedPanelDisplay : MonoBehaviour
{
    [SerializeField] private SCV_DamagedPanel damagedPanel;
    [SerializeField] private TMP_Text conditionText;

    private string feedbackMessage = "";
    private float feedbackUntil = 0f;

    private void Update()
    {
        conditionText.text =
            "DEFENSE PANEL\n" +
            "CONDITION: " +
            damagedPanel.CurrentCondition + "/" +
            damagedPanel.MaxCondition;

        if (Time.time < feedbackUntil)
        {
            conditionText.text += "\n" + feedbackMessage;
        }
    }

    public void ShowFeedback(string message)
    {
        feedbackMessage = message;
        feedbackUntil = Time.time + 2f;
    }
}