using System;
using UnityEngine;

// INTEGRATION NOTE: at integration, this component's Health is to be replaced by the
// Defense Yard's player health component (one health value in the combined game).
public class MED_PlayerResources : MonoBehaviour
{
    public const int DefaultStartingHealth = 50;

    [SerializeField] int maxHealth = 100;

    int[] amounts;

    public event Action<MED_ResourceType, int> OnResourceChanged;

    public int Health => Get(MED_ResourceType.Health);
    public int MaxHealth => maxHealth;
    public bool IsHealthFull => Health >= maxHealth;

    void Awake()
    {
        EnsureInitialized();
    }

    void EnsureInitialized()
    {
        if (amounts != null) return;
        amounts = new int[Enum.GetValues(typeof(MED_ResourceType)).Length];
        amounts[(int)MED_ResourceType.Health] = Mathf.Clamp(DefaultStartingHealth, 0, maxHealth);
    }

    public int Get(MED_ResourceType type)
    {
        EnsureInitialized();
        return amounts[(int)type];
    }

    public void Add(MED_ResourceType type, int amount)
    {
        Set(type, Get(type) + amount);
    }

    public bool TrySpend(MED_ResourceType type, int amount)
    {
        if (amount < 0 || Get(type) < amount) return false;
        Set(type, Get(type) - amount);
        return true;
    }

    public void SetHealth(int value)
    {
        Set(MED_ResourceType.Health, value);
    }

    public void ResetHealth()
    {
        SetHealth(DefaultStartingHealth);
    }

    void Set(MED_ResourceType type, int value)
    {
        EnsureInitialized();
        value = Mathf.Max(0, value);
        if (type == MED_ResourceType.Health) value = Mathf.Min(value, maxHealth);
        if (amounts[(int)type] == value) return;
        amounts[(int)type] = value;
        OnResourceChanged?.Invoke(type, value);
    }

    [ContextMenu("Debug: Health -10")]
    void DebugHealthMinus10() => Add(MED_ResourceType.Health, -10);

    [ContextMenu("Debug: Health +10")]
    void DebugHealthPlus10() => Add(MED_ResourceType.Health, 10);

    [ContextMenu("Debug: Add 5 of every resource")]
    void DebugAddFiveOfEvery()
    {
        foreach (MED_ResourceType t in Enum.GetValues(typeof(MED_ResourceType)))
            Add(t, 5);
    }
}
