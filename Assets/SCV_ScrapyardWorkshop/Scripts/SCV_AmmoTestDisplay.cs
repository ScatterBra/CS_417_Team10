using TMPro;
using UnityEngine;

public class SCV_AmmoTestDisplay : MonoBehaviour
{
    [SerializeField] private SCV_AmmoTest ammoTest;
    [SerializeField] private TMP_Text ammoText;

    private string feedbackMessage = "";
    private float feedbackUntil = 0f;

    private void Update()
    {
        ammoText.text =
            "DEFENSE AMMO\n" +
            "LOADED: " +
            ammoTest.StoredAmmo + "/" +
            ammoTest.MaxAmmo;

        if (Time.time < feedbackUntil)
        {
            ammoText.text += "\n" + feedbackMessage;
        }
    }

    public void ShowFeedback(string message)
    {
        feedbackMessage = message;
        feedbackUntil = Time.time + 2f;
    }
}