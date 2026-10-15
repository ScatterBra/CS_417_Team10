using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A placed building. Pivot is at the footprint center on the floor. The building's "front" is local -Z, so a
// building placed with the player's head yaw faces the player.
public class MED_Building : MonoBehaviour
{
    static readonly Dictionary<MED_BuildingDefinition, int> counts = new Dictionary<MED_BuildingDefinition, int>();

    [SerializeField] MED_BuildingDefinition definition;
    [SerializeField] float popDuration = 0.3f;

    bool registered;

    public MED_BuildingDefinition Definition => definition;
    public Vector2Int[] Cells { get; private set; } = Array.Empty<Vector2Int>();
    public int QuarterTurns { get; private set; }

    /// <summary>Raised after a building is placed (after cells are occupied).</summary>
    public static event Action<MED_Building> OnBuilt;
    /// <summary>Raised when a placed building is destroyed (e.g. Clear All).</summary>
    public static event Action<MED_Building> OnRemoved;

    public static int CountOf(MED_BuildingDefinition def) => def != null && counts.TryGetValue(def, out var n) ? n : 0;

    public static int TotalCount
    {
        get { int t = 0; foreach (var n in counts.Values) t += n; return t; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        counts.Clear();
        OnBuilt = null;
        OnRemoved = null;
    }

    public void Initialize(MED_BuildingDefinition def, Vector2Int[] cells, int quarterTurns)
    {
        definition = def;
        Cells = cells;
        QuarterTurns = quarterTurns;
        registered = true;
        counts[def] = CountOf(def) + 1;
        OnBuilt?.Invoke(this);
        if (popDuration > 0f) StartCoroutine(Pop());
    }

    IEnumerator Pop()
    {
        var target = transform.localScale;
        const float c1 = 1.70158f, c3 = c1 + 1f;
        for (float t = 0f; t < popDuration; t += Time.deltaTime)
        {
            float x = t / popDuration - 1f;
            float s = 1f + c3 * x * x * x + c1 * x * x; // ease-out-back
            transform.localScale = target * Mathf.Max(0.01f, s);
            yield return null;
        }
        transform.localScale = target;
    }

    void OnDestroy()
    {
        if (!registered) return;
        counts[definition] = Mathf.Max(0, CountOf(definition) - 1);
        OnRemoved?.Invoke(this);
    }
}
