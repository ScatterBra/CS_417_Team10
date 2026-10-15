using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Base behavior for anything a tool can harvest. Holds a "remaining" MED_Stat.
public class MED_Harvestable : MonoBehaviour, MED_IToolTarget
{
    [SerializeField] MED_Stat stat = new MED_Stat();
    [SerializeField] MED_ResourceType resource = MED_ResourceType.Herbs;
    [SerializeField] int yieldPerHit = 1;
    [SerializeField] MED_ToolType requiredTool = MED_ToolType.Sickle;
    [SerializeField] float hitCooldown = 0.4f;
    [SerializeField] bool destroyOnEmpty = true;
    [SerializeField] Transform visualRoot;      // squashed on hit, shrunk on death
    [SerializeField] Transform messageAnchor;   // where floating text appears
    [SerializeField] MED_StatBar statBar;
    [SerializeField] GameObject puffPrefab;

    float nextHitTime;
    float nextMessageTime;
    bool dead;
    Coroutine squash;
    MED_GridManager grid;

    public MED_Stat Stat => stat;
    public MED_ResourceType Resource => resource;
    public MED_ToolType RequiredTool => requiredTool;
    public bool DestroyOnEmpty => destroyOnEmpty;
    public bool IsEmpty => stat.Value <= 0f;

    public event Action<MED_Harvestable> OnDepleted;
    public static event Action<MED_Harvestable> OnAnyDepleted;

    void Awake()
    {
        if (statBar != null) statBar.Bind(stat);
    }

    public void ConfigureResource(MED_ResourceType type) => resource = type;

    /// <summary>Remember which grid cells this object occupies so they are freed on death/destroy.</summary>
    public void AssignGrid(MED_GridManager g) => grid = g;

    public void Refill() => stat.Set(stat.Max);

    Vector3 MessagePosition => messageAnchor != null ? messageAnchor.position : transform.position + Vector3.up;

    public bool TryHarvest(MED_HarvestTool tool, MED_PlayerResources player)
    {
        if (dead) return false;
        if (stat.Value <= 0f)
        {
            Message("EMPTY", new Color(1f, 0.5f, 0.4f));
            return false;
        }
        if (tool == null || tool.ToolType != requiredTool)
        {
            Message($"Use the {requiredTool}", new Color(1f, 0.85f, 0.4f));
            return false;
        }
        if (Time.time < nextHitTime) return false;
        nextHitTime = Time.time + hitCooldown;

        stat.Add(-1f);
        if (player != null) player.Add(resource, yieldPerHit);
        MED_FloatingText.Spawn(MessagePosition, $"+{yieldPerHit} {resource}", new Color(0.6f, 1f, 0.6f));
        Feedback();
        tool.SendHaptic(0.5f, 0.1f);

        if (stat.Value <= 0f && destroyOnEmpty) StartCoroutine(Die());
        return true;
    }

    void Message(string text, Color c)
    {
        if (Time.time < nextMessageTime) return;
        nextMessageTime = Time.time + 1.5f;
        MED_FloatingText.Spawn(MessagePosition, text, c, 0.5f);
    }

    void Feedback()
    {
        if (squash != null) StopCoroutine(squash);
        squash = StartCoroutine(Squash());
        SpawnPuff(0.6f);
    }

    IEnumerator Squash()
    {
        var t = visualRoot != null ? visualRoot : transform;
        var rest = Vector3.one;
        const float duration = 0.18f;
        for (float e = 0f; e < duration; e += Time.deltaTime)
        {
            float k = Mathf.Sin(e / duration * Mathf.PI);
            t.localScale = Vector3.Lerp(rest, new Vector3(1.15f, 0.8f, 1.15f), k);
            yield return null;
        }
        t.localScale = rest;
        squash = null;
    }

    void SpawnPuff(float scale)
    {
        if (puffPrefab == null) return;
        var p = Instantiate(puffPrefab, transform.position + Vector3.up * 0.2f, Quaternion.identity);
        p.transform.localScale = Vector3.one * scale;
        Destroy(p, 1.5f);
    }

    IEnumerator Die()
    {
        dead = true;
        FreeCells();
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        OnDepleted?.Invoke(this);
        OnAnyDepleted?.Invoke(this);
        if (squash != null) StopCoroutine(squash);

        var t = visualRoot != null ? visualRoot : transform;
        const float duration = 0.25f;
        for (float e = 0f; e < duration; e += Time.deltaTime)
        {
            t.localScale = Vector3.one * Mathf.Max(0.01f, 1f - e / duration);
            yield return null;
        }
        SpawnPuff(1.2f);
        Destroy(gameObject);
    }

    void FreeCells()
    {
        if (grid != null) grid.Free(this);
    }

    void OnDestroy() => FreeCells();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => OnAnyDepleted = null;
}
