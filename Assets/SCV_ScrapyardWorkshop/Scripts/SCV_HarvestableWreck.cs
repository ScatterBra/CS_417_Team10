using System.Collections;
using UnityEngine;

public class SCV_HarvestableWreck : MonoBehaviour
{
    [SerializeField] private SCV_ResourceManager resourceManager;

    [Header("Wreck Resources")]
    [SerializeField] private int scrapRemaining = 6;
    [SerializeField] private int scrapPerHarvest = 1;

    [Header("Salvage Respawn")]
    [SerializeField] private float respawnDelay = 7f;

    [Header("Visual Chunks")]
    [SerializeField] private GameObject chunk1;
    [SerializeField] private GameObject chunk2;
    [SerializeField] private GameObject chunk3;

    [SerializeField] private GameObject harvestCanvas;

    private Vector3 chunk1FullScale;
    private Vector3 chunk2FullScale;
    private Vector3 chunk3FullScale;

    private int startingScrap;
    private bool isRespawning = false;

    private void Awake()
    {
        startingScrap = scrapRemaining;

        if (chunk1 != null)
            chunk1FullScale = chunk1.transform.localScale;

        if (chunk2 != null)
            chunk2FullScale = chunk2.transform.localScale;

        if (chunk3 != null)
            chunk3FullScale = chunk3.transform.localScale;

        UpdateVisuals();
    }

    public void Harvest()
    {
        if (scrapRemaining <= 0)
        {
            if (isRespawning)
                Debug.Log("Waiting for new salvage...");
            else
                Debug.Log("This wreck has no scrap remaining.");

            return;
        }

        int amount = Mathf.Min(scrapPerHarvest, scrapRemaining);

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
                " is fully harvested. New salvage incoming..."
            );

            if (harvestCanvas != null)
                harvestCanvas.SetActive(false);

            if (!isRespawning)
                StartCoroutine(RespawnWreck());
        }
    }

    private IEnumerator RespawnWreck()
    {
        isRespawning = true;

        yield return new WaitForSeconds(respawnDelay);

        scrapRemaining = startingScrap;

        UpdateVisuals();

        if (harvestCanvas != null)
            harvestCanvas.SetActive(true);

        isRespawning = false;

        Debug.Log(
            "New salvage has arrived! Scrap available: " +
            scrapRemaining
        );
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