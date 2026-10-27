using System;
using System.Collections.Generic;
using UnityEngine;

// Crafting station on the placed Prep Bench. Each bench instance works independently.
public class MED_PrepBench : MonoBehaviour
{
    public enum BenchState { Idle, Preparing }

    [SerializeField] MED_Stat prepProgress = new MED_Stat("PrepProgress", 0f, 0f, 1f);
    [SerializeField] MED_Recipe[] recipes;
    [SerializeField] float maxUseDistance = 3.5f;
    [SerializeField] MED_StatBar statBar;
    [SerializeField] Transform workingProp;   // pestle: bobs and spins while Preparing
    [SerializeField] Transform messageAnchor;

    [SerializeField] MED_PlayerResources player;
    MED_CraftTicket paidCraft;
    bool starting;
    Vector3 propRestPosition;
    Quaternion propRestRotation;
    bool propCached;

    public BenchState State { get; private set; } = BenchState.Idle;
    public MED_Recipe CurrentRecipe { get; private set; }
    public MED_Stat PrepProgress => prepProgress;
    public IReadOnlyList<MED_Recipe> Recipes => recipes;
    public bool IsIdle => State == BenchState.Idle;

    /// <summary>Raised when the bench switches between Idle and Preparing.</summary>
    public static event Action<MED_PrepBench,MED_PlayerResources,MED_Recipe> CraftCommitted;
    public event Action StateChanged;

    MED_PlayerResources Player
    {
        get
        {
            if (player == null) player = FindAnyObjectByType<MED_PlayerResources>();
            return player;
        }
    }

    Vector3 MessagePosition => messageAnchor != null ? messageAnchor.position : transform.position + Vector3.up * 2f;

    void Start()
    {
        if (workingProp != null)
        {
            propRestPosition = workingProp.localPosition;
            propRestRotation = workingProp.localRotation;
            propCached = true;
        }
        if (statBar != null)
        {
            statBar.Bind(prepProgress);
            statBar.SetAlpha(0.25f);
        }
    }

    /// <summary>True if the player owns every input (ignores distance and bench state).</summary>
    public bool CanAfford(MED_Recipe recipe, out string problem)
    {
        problem = null;
        if(recipe==null || recipe.inputs==null || recipe.inputs.Count==0){problem="Recipe needs valid ingredients";return false;}
        var p = Player;
        foreach (var input in Combine(recipe.inputs))
        {
            int have = p != null ? p.Get(input.Key) : 0;
            if (have < input.Value)
            {
                problem = $"Need {input.Value} {input.Key} (have {have})";
                return false;
            }
        }
        return true;
    }

    public bool TryStartRecipe(MED_Recipe recipe)
    {
        if(!DEF_ClinicTraining.Allow(this,9,"Clinic action"))return false;
        if (recipe == null) return false;
        if (starting || State == BenchState.Preparing) return Fail("Bench is busy");
        if (recipes == null || Array.IndexOf(recipes,recipe)<0 || recipe.duration<=0f || float.IsNaN(recipe.duration) || float.IsInfinity(recipe.duration)) return Fail("Recipe unavailable");
        if (!IsPlayerNear()) return Fail("Too far");
        if (!CanAfford(recipe, out var problem)) return Fail(problem);

        starting=true;
        try { if(Player==null || !Player.TryPayRecipe(this,recipe,out paidCraft))return Fail("Craft failed: inputs unavailable. Gather the recipe ingredients and retry."); }
        finally {starting=false;}

        CurrentRecipe = recipe;
        prepProgress.Set(0f);
        SetState(BenchState.Preparing);
        return true;
    }

    void Update()
    {
        if (State != BenchState.Preparing) return;

        // Euler integration of the PrepProgress stat: progress += rate * dt, with rate = 1 / duration.
        prepProgress.Add((1f / CurrentRecipe.duration) * Time.deltaTime);
        AnimateProp();

        if (prepProgress.Value >= 1f) Complete();
    }

    void Complete()
    {
        if(State!=BenchState.Preparing || paidCraft==null)return;
        var recipe = CurrentRecipe;
        var ticket=paidCraft;
        var p = ticket.Player;
        if(p==null || !p.CommitRecipe(ticket))return;
        paidCraft=null;
        if(CraftCommitted!=null)foreach(Action<MED_PrepBench,MED_PlayerResources,MED_Recipe> listener in CraftCommitted.GetInvocationList())try{listener(this,p,recipe);}catch(Exception e){Debug.LogException(e);}
        float lift = 0f;
        foreach (var output in recipe.outputs)
        {
            MED_FloatingText.Spawn(MessagePosition + Vector3.up * lift, $"+{output.amount} {output.type}", new Color(0.6f, 1f, 0.6f));
            lift += 0.2f;
        }
        CurrentRecipe = null;
        prepProgress.Set(0f);
        SetState(BenchState.Idle);
    }

    void SetState(BenchState state)
    {
        State = state;
        if (statBar != null) statBar.SetAlpha(state == BenchState.Preparing ? 0.9f : 0.25f);
        if (state == BenchState.Idle && propCached)
        {
            workingProp.localPosition = propRestPosition;
            workingProp.localRotation = propRestRotation;
        }
        StateChanged?.Invoke();
    }

    void AnimateProp()
    {
        if (!propCached) return;
        float t = Time.time;
        workingProp.localPosition = propRestPosition + Vector3.up * (0.03f * Mathf.Sin(t * 8f));
        workingProp.localRotation = propRestRotation * Quaternion.Euler(0f, t * 240f, 0f);
    }

    bool IsPlayerNear()
    {
        var cam = Camera.main;
        Vector3 pos;
        if (cam != null) pos = cam.transform.position;
        else if (Player != null) pos = Player.transform.position;
        else return false;
        var d = pos - transform.position;
        d.y = 0f;
        return d.magnitude <= maxUseDistance;
    }

    bool Fail(string message)
    {
        MED_FloatingText.Spawn(MessagePosition, message, new Color(1f, 0.6f, 0.4f), 0.5f);
        return false;
    }

    static Dictionary<MED_ResourceType, int> Combine(List<MED_ResourceCost> costs)
    {
        var d = new Dictionary<MED_ResourceType, int>();
        foreach (var c in costs)
            d[c.type] = (d.TryGetValue(c.type, out var n) ? n : 0) + c.amount;
        return d;
    }
}
