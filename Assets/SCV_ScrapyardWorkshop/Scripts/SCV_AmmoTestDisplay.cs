using TMPro;
using UnityEngine;

public class SCV_AmmoTestDisplay : MonoBehaviour
{
    [SerializeField] private SCV_AmmoTest ammoTest;
    [SerializeField] private TMP_Text ammoText;

    private void Update()
    {
        ammoText.text =
            "DEFENSE AMMO\n" +
            "LOADED: " +
            ammoTest.StoredAmmo + "/" +
            ammoTest.MaxAmmo;
    }
}