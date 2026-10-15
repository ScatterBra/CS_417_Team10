using UnityEngine;

public class SCV_PartsOutputDisplay : MonoBehaviour
{
    [SerializeField] private SCV_ResourceManager resourceManager;

    [Header("Parts Visual Stages")]
    [SerializeField] private GameObject partsStage1;
    [SerializeField] private GameObject partsStage2;
    [SerializeField] private GameObject partsStage3;

    private void OnEnable()
    {
        if (resourceManager != null)
            resourceManager.OnResourcesChanged += UpdatePartsVisuals;
    }

    private void Start()
    {
        UpdatePartsVisuals();
    }

    private void OnDisable()
    {
        if (resourceManager != null)
            resourceManager.OnResourcesChanged -= UpdatePartsVisuals;
    }

    private void UpdatePartsVisuals()
    {
        if (resourceManager == null)
            return;

        int parts = resourceManager.parts;

        if (partsStage1 != null)
            partsStage1.SetActive(parts >= 1);

        if (partsStage2 != null)
            partsStage2.SetActive(parts >= 2);

        if (partsStage3 != null)
            partsStage3.SetActive(parts >= 3);
    }
}