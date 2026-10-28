using UnityEngine;

public class SCV_HarvestableWreck : MonoBehaviour
{
    [Header("Wreck Resources")]
    [SerializeField] private int scrapRemaining = 6;
    [SerializeField] private int scrapPerHarvest = 1;

    [Header("Visual Chunks")]
    [SerializeField] private GameObject chunk1;
    [SerializeField] private GameObject chunk2;
    [SerializeField] private GameObject chunk3;

    [SerializeField] private GameObject harvestCanvas;

    private SCV_ResourceManager resourceManager;
    private SCV_SalvageSpawner salvageSpawner;

    private Vector3 chunk1FullScale;
    private Vector3 chunk2FullScale;
    private Vector3 chunk3FullScale;

    private SCV_ProgressionManager progressionManager;

    public int ScrapRemaining => scrapRemaining;

    private void Awake()
    {
        if (chunk1 != null)
            chunk1FullScale = chunk1.transform.localScale;

        if (chunk2 != null)
            chunk2FullScale = chunk2.transform.localScale;

        if (chunk3 != null)
            chunk3FullScale = chunk3.transform.localScale;

        UpdateVisuals();
    }

    public void Initialize(
        SCV_ResourceManager manager,
        SCV_SalvageSpawner spawner)
    {
        if (DEF_Progression.Current != null && DEF_Progression.Current.workshopRecording) { scrapRemaining = 12; scrapPerHarvest = 2; }
        resourceManager = manager;
        salvageSpawner = spawner;

        progressionManager =
            FindFirstObjectByType<SCV_ProgressionManager>();

        UpdateVisuals();
    }

    public void Harvest()
    {
        if (scrapRemaining <= 0)
            return;

        if (resourceManager == null)
        {
            Debug.LogError(
                gameObject.name +
                " does not have a Resource Manager."
            );
            return;
        }

        int amount = Mathf.Min(
            scrapPerHarvest,
            scrapRemaining
        );

        scrapRemaining -= amount;
        resourceManager.AddScrap(amount);

        Debug.Log(
            gameObject.name +
            " harvested. Scrap remaining: " +
            scrapRemaining
        );

        UpdateVisuals();

        if (scrapRemaining <= 0)
        {
            Debug.Log(
                gameObject.name +
                " depleted and destroyed."
            );

            if (harvestCanvas != null)
                harvestCanvas.SetActive(false);

            if (progressionManager != null)
                progressionManager.FirstWreckDepleted();

            if (salvageSpawner != null)
                salvageSpawner.NotifyWreckDepleted(this);

            Destroy(gameObject);
        }
    }

    private void UpdateVisuals()
    {
        // Chunk 1 represents scrap 1-2
        SetChunkVisual(
            chunk1,
            chunk1FullScale,
            scrapRemaining >= 2,
            scrapRemaining == 1
        );

        // Chunk 2 represents scrap 3-4
        SetChunkVisual(
            chunk2,
            chunk2FullScale,
            scrapRemaining >= 4,
            scrapRemaining == 3
        );

        // Chunk 3 represents scrap 5-6
        SetChunkVisual(
            chunk3,
            chunk3FullScale,
            scrapRemaining >= 6,
            scrapRemaining == 5
        );
    }

    private void SetChunkVisual(
        GameObject chunk,
        Vector3 fullScale,
        bool full,
        bool half)
    {
        if (chunk == null)
            return;

        if (full)
        {
            chunk.SetActive(true);
            chunk.transform.localScale = fullScale;
        }
        else if (half)
        {
            chunk.SetActive(true);

            chunk.transform.localScale = new Vector3(
                fullScale.x,
                fullScale.y * 0.5f,
                fullScale.z
            );
        }
        else
        {
            chunk.SetActive(false);
        }
    }

    [ContextMenu("Harvest Once")]
    private void TestHarvest()
    {
        Harvest();
    }
}
