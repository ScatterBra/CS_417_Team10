using TMPro;
using UnityEngine;

public class SCV_MissionStatus : MonoBehaviour
{
    [SerializeField] private SCV_DamagedPanel damagedPanel;
    [SerializeField] private SCV_AmmoTest ammoTest;
    [SerializeField] private TMP_Text statusText;

    private bool missionCompleted = false;

    private void Update()
    {
        if (damagedPanel == null ||
            ammoTest == null ||
            statusText == null)
            return;

        bool defenseReady =
            damagedPanel.CurrentCondition >= damagedPanel.MaxCondition;

        bool ammoReady =
            ammoTest.StoredAmmo >= ammoTest.MaxAmmo;

        string missionStatus =
            defenseReady && ammoReady
                ? "OUTPOST READY"
                : "NOT READY";

        statusText.text =
            "RESTORE THE OUTPOST\n\n" +
            "DEFENSE: " +
            damagedPanel.CurrentCondition + "/" +
            damagedPanel.MaxCondition +
            "\nAMMO: " +
            ammoTest.StoredAmmo + "/" +
            ammoTest.MaxAmmo +
            "\n\nSTATUS: " +
            missionStatus;

        if (defenseReady && ammoReady && !missionCompleted)
        {
            missionCompleted = true;
            Debug.Log("OUTPOST RESTORED - MISSION COMPLETE");
        }
    }
}