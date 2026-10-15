using System.Collections;
using UnityEngine;

// Holds one toy model per slot. Respawns a fresh model when one is consumed.
public class MED_ModelShelf : MonoBehaviour
{
    [SerializeField] Transform[] slots;
    [SerializeField] MED_BuildingDefinition[] definitions;
    [SerializeField] MED_BuildingPlacer placer;

    MED_BuildingMini[] minis;

    public MED_BuildingPlacer Placer => placer;

    void Start()
    {
        minis = new MED_BuildingMini[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            var existing = slots[i].GetComponentInChildren<MED_BuildingMini>();
            if (existing != null)
            {
                minis[i] = existing;
                existing.Assign(this, i);
            }
        }
        RestockAll();
    }

    public void GetSlotPose(int index, out Vector3 position, out Quaternion rotation)
    {
        position = slots[index].position;
        rotation = slots[index].rotation;
    }

    /// <summary>Fills every empty slot with a fresh model.</summary>
    public void RestockAll()
    {
        if (minis == null) return;
        for (int i = 0; i < slots.Length; i++)
            if (minis[i] == null) Spawn(i);
    }

    /// <summary>Called by the placer after a model was turned into a building.</summary>
    public void OnMiniConsumed(MED_BuildingMini mini)
    {
        int i = mini.SlotIndex;
        if (i < 0 || i >= minis.Length || minis[i] != mini) return;
        minis[i] = null;
        Spawn(i);
    }

    void Spawn(int i)
    {
        var def = definitions[i];
        if (def == null || def.miniPrefab == null) return;
        var go = Instantiate(def.miniPrefab, slots[i]);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        var mini = go.GetComponent<MED_BuildingMini>();
        mini.Assign(this, i);
        minis[i] = mini;
        StartCoroutine(PopIn(go.transform));
    }

    IEnumerator PopIn(Transform t)
    {
        var target = t.localScale;
        const float duration = 0.25f;
        for (float e = 0f; e < duration && t != null; e += Time.deltaTime)
        {
            t.localScale = target * Mathf.SmoothStep(0.01f, 1f, e / duration);
            yield return null;
        }
        if (t != null) t.localScale = target;
    }
}
