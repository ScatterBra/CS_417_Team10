using TMPro;
using UnityEngine;

public class SCV_MissionStatus : MonoBehaviour
{
    [SerializeField] private SCV_DamagedPanel damagedPanel;
    [SerializeField] private SCV_AmmoTest ammoTest;
    [SerializeField] private TMP_Text statusText;

    private bool missionCompleted = false;
    bool integrated;
    void Start() { integrated = DEF_IntegratedXRPresentation.IsIntegratedScene(gameObject.scene); }

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

        if (integrated)
        {
            statusText.text = "WORKSHOP PRACTICE\n\nPANEL: " + damagedPanel.CurrentCondition + "/" + damagedPanel.MaxCondition +
                "\nAMMO STORAGE: " + ammoTest.StoredAmmo + "/" + ammoTest.MaxAmmo +
                "\n\nFilling these targets is optional.\nFor Defense: craft Ammo + RepairKit and physically deliver either to a practice intake.";
            return; // Standalone completion text is not the integrated Defense prerequisite.
        }
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