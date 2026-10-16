using System;
using UnityEngine;

[Serializable]
public class MED_Stat
{
    public string Name;
    [SerializeField] float value;
    public float Min;
    public float Max = 100f;

    public event Action<MED_Stat> OnChanged;

    public float Value => value;
    public float Normalized => Max > Min ? Mathf.Clamp01((value - Min) / (Max - Min)) : 0f;

    public MED_Stat() { }

    public MED_Stat(string name, float value, float min, float max)
    {
        Name = name;
        Min = min;
        Max = max;
        this.value = Mathf.Clamp(value, min, max);
    }

    public void Add(float amount) => Set(value + amount);

    public void Set(float newValue)
    {
        newValue = Mathf.Clamp(newValue, Min, Max);
        if (Mathf.Approximately(newValue, value)) return;
        value = newValue;
        OnChanged?.Invoke(this);
    }
}
