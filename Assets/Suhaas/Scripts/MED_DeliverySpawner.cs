using TMPro;
using UnityEngine;

// Schedules random supply-cart deliveries (never more than one cart at a time) and drives the "DELIVERIES" sign.
public class MED_DeliverySpawner : MonoBehaviour
{
    [SerializeField] MED_DeliveryPath path;
    [SerializeField] GameObject cartPrefab;
    [SerializeField] Transform doorMarker;
    [SerializeField] TextMeshPro statusLabel;
    [SerializeField] float firstMin = 15f;
    [SerializeField] float firstMax = 25f;
    [SerializeField] float intervalMin = 40f;
    [SerializeField] float intervalMax = 70f;

    MED_SupplyCart cart;
    bool cartActive;
    float nextTime;
    int shownSeconds = -1;
    string shownText;

    public bool CartPresent => cartActive;

    Vector3 DoorwayPosition => (doorMarker != null ? doorMarker.position : transform.position) + Vector3.up * 1.8f;

    void Start()
    {
        nextTime = Time.time + Random.Range(firstMin, firstMax);
        RefreshSign(true);
    }

    void Update()
    {
        if (cartActive && cart == null) OnCartFinished(null);   // destroyed by something else
        if (!cartActive && Time.time >= nextTime) Spawn();
        RefreshSign(false);
    }

    /// <summary>Test hook: calls a delivery right now unless a cart is already here.</summary>
    public bool SpawnNow()
    {
        if (cartActive)
        {
            MED_Messages.Fail(DoorwayPosition, MED_Messages.CartAlreadyHere);
            return false;
        }
        Spawn();
        return true;
    }

    void Spawn()
    {
        if (path == null || cartPrefab == null) return;
        var go = Instantiate(cartPrefab, path.GetPointAtDistance(0f), Quaternion.LookRotation(path.GetTangentAtDistance(0f)));
        cart = go.GetComponent<MED_SupplyCart>();
        cart.Finished += OnCartFinished;
        cart.StateChanged += () => RefreshSign(true);
        cartActive = true;
        cart.Init(path);
        MED_FloatingText.Spawn(DoorwayPosition, "DELIVERY ARRIVING", new Color(1f, 0.9f, 0.4f), 0.7f);
        RefreshSign(true);
    }

    void OnCartFinished(MED_SupplyCart finished)
    {
        if (!cartActive) return;
        cartActive = false;
        cart = null;
        nextTime = Time.time + Random.Range(intervalMin, intervalMax);
        RefreshSign(true);
    }

    void RefreshSign(bool force)
    {
        if (statusLabel == null) return;
        string line;
        int secs = -1;
        if (!cartActive)
        {
            secs = Mathf.Max(0, Mathf.CeilToInt(nextTime - Time.time));
            line = $"Next delivery in {secs / 60}:{secs % 60:00}";
        }
        else if (cart == null) line = "Cart arriving...";
        else
        {
            switch (cart.State)
            {
                case MED_SupplyCart.CartState.Arriving: line = "Cart arriving..."; break;
                case MED_SupplyCart.CartState.Waiting: line = "Cart at bay - unload!"; break;
                default: line = "Cart leaving"; break;
            }
        }
        if (!force && secs == shownSeconds && line == shownText) return;
        shownSeconds = secs;
        shownText = line;
        statusLabel.text = line;
    }
}
