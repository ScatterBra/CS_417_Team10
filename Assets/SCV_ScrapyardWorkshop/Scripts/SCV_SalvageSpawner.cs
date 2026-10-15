using TMPro;
using UnityEngine;

public class SCV_SalvageSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private SCV_ResourceManager resourceManager;

    [SerializeField]
    private TMP_Text statusText;

    [Header("Salvage Prefabs")]
    [SerializeField]
    private SCV_HarvestableWreck[] salvagePrefabs;

    [Header("Spawn Locations")]
    [SerializeField]
    private Transform[] spawnPoints;

    [Header("Arrival Timing")]
    [SerializeField]
    private float minRespawnDelay = 8f;

    [SerializeField]
    private float maxRespawnDelay = 12f;

    [Header("Runtime Stats")]
    [SerializeField]
    private float arrivalTimer = 0f;

    private SCV_HarvestableWreck currentWreck;
    private bool waitingForSalvage = false;

    public float ArrivalTimer => arrivalTimer;

    private void Start()
    {
        SpawnRandomSalvage();
    }

    private void Update()
    {
        if (waitingForSalvage)
        {
            // Euler integration:
            // timer changes continuously over time.
            arrivalTimer -= Time.deltaTime;

            if (arrivalTimer < 0f)
                arrivalTimer = 0f;

            UpdateStatusText();

            if (arrivalTimer <= 0f)
            {
                SpawnRandomSalvage();
            }
        }
        else
        {
            UpdateStatusText();
        }
    }

    public void NotifyWreckDepleted(
        SCV_HarvestableWreck wreck)
    {
        if (wreck != currentWreck)
            return;

        currentWreck = null;

        arrivalTimer = Random.Range(
            minRespawnDelay,
            maxRespawnDelay
        );

        waitingForSalvage = true;

        Debug.Log(
            "Salvage depleted. New salvage arriving in " +
            arrivalTimer.ToString("F1") +
            " seconds."
        );
    }

    private void SpawnRandomSalvage()
    {
        if (salvagePrefabs == null ||
            salvagePrefabs.Length == 0)
        {
            Debug.LogError(
                "No salvage prefabs assigned."
            );
            return;
        }

        if (spawnPoints == null ||
            spawnPoints.Length == 0)
        {
            Debug.LogError(
                "No salvage spawn points assigned."
            );
            return;
        }

        int prefabIndex = Random.Range(
            0,
            salvagePrefabs.Length
        );

        int spawnIndex = Random.Range(
            0,
            spawnPoints.Length
        );

        SCV_HarvestableWreck prefab =
            salvagePrefabs[prefabIndex];

        Transform spawnPoint =
            spawnPoints[spawnIndex];

        currentWreck = Instantiate(
            prefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        currentWreck.Initialize(
            resourceManager,
            this
        );

        waitingForSalvage = false;
        arrivalTimer = 0f;

        Debug.Log(
            "New salvage arrived: " +
            currentWreck.gameObject.name +
            " at " +
            spawnPoint.gameObject.name
        );

        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        if (statusText == null)
            return;

        if (waitingForSalvage)
        {
            statusText.text =
                "SALVAGE YARD\n" +
                "NEXT ARRIVAL: " +
                arrivalTimer.ToString("F1") +
                "s";
        }
        else if (currentWreck != null)
        {
            statusText.text =
                "SALVAGE YARD\n" +
                "SCRAP REMAINING: " +
                currentWreck.ScrapRemaining;
        }
        else
        {
            statusText.text =
                "SALVAGE YARD\n" +
                "WAITING...";
        }
    }
}