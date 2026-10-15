using TMPro;
using UnityEngine;

public class SCV_AmmoStorageDisplay : MonoBehaviour
{
    [SerializeField] private SCV_ResourceManager resourceManager;
    [SerializeField] private TMP_Text ammoText;

    private void Update()
    {
        ammoText.text =
            "AMMO STORAGE\n" +
            "AMMO: " + resourceManager.ammo;
    }
}