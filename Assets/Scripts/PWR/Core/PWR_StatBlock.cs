using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// Holds all Stats of an instance. Every change goes through Set/Add so listeners
    /// (labels, death, tutorials) are notified. Passive stats are integrated in Update:
    ///     value += ratePerSecond * Time.deltaTime
    /// </summary>
    public class PWR_StatBlock : MonoBehaviour
    {
        public List<PWR_Stat> stats = new List<PWR_Stat>();

        [Tooltip("Pauses all passive integration.")]
        public bool passiveEnabled = true;

        /// <summary>(statName, newValue, delta)</summary>
        public event Action<string, float, float> OnStatChanged;

        private readonly Dictionary<string, PWR_Stat> lookup = new Dictionary<string, PWR_Stat>();

        public IReadOnlyList<PWR_Stat> Stats => stats;

        private void Awake() => RebuildLookup();

        private void OnValidate() => RebuildLookup();

        private void RebuildLookup()
        {
            lookup.Clear();
            foreach (var s in stats)
            {
                if (s == null || string.IsNullOrEmpty(s.name)) continue;
                lookup[s.name] = s;
            }
        }

        private void Update()
        {
            if (!passiveEnabled) return;
            float dt = Time.deltaTime;
            for (int i = 0; i < stats.Count; i++)
            {
                var s = stats[i];
                if (s.ratePerSecond == 0f) continue;
                float delta = s.ratePerSecond * dt;
                if (s.usePassiveFloor && delta < 0f)
                    delta = Mathf.Max(delta, Mathf.Min(0f, s.passiveFloor - s.value));
                if (delta != 0f) Add(s.name, delta);
            }
        }

        /// <summary>Adds a stat (used by the editor builder). Replaces an existing one with the same name.</summary>
        public PWR_Stat AddStat(string statName, float value, float min, float max, float ratePerSecond = 0f)
        {
            stats.RemoveAll(s => s != null && s.name == statName);
            var stat = new PWR_Stat(statName, value, min, max, ratePerSecond);
            stats.Add(stat);
            RebuildLookup();
            return stat;
        }

        public bool Has(string statName) => lookup.ContainsKey(statName);

        public PWR_Stat GetStat(string statName)
        {
            if (lookup.Count == 0 && stats.Count > 0) RebuildLookup();
            lookup.TryGetValue(statName, out var s);
            return s;
        }

        public float Get(string statName) => GetStat(statName)?.value ?? 0f;
        public float GetMax(string statName) => GetStat(statName)?.max ?? 0f;
        public float GetNormalized(string statName) => GetStat(statName)?.Normalized ?? 0f;
        public float GetSpace(string statName) => GetStat(statName)?.Space ?? 0f;

        /// <summary>Adds delta (clamped). Returns the amount actually applied.</summary>
        public float Add(string statName, float delta)
        {
            var s = GetStat(statName);
            if (s == null)
            {
                Debug.LogWarning($"[PWR] {name} has no stat '{statName}'", this);
                return 0f;
            }
            float old = s.value;
            s.value = Mathf.Clamp(s.value + delta, s.min, s.max);
            float applied = s.value - old;
            if (applied != 0f) OnStatChanged?.Invoke(s.name, s.value, applied);
            return applied;
        }

        public void Set(string statName, float value)
        {
            var s = GetStat(statName);
            if (s == null) return;
            Add(statName, value - s.value);
        }

        public void SetRate(string statName, float ratePerSecond)
        {
            var s = GetStat(statName);
            if (s != null) s.ratePerSecond = ratePerSecond;
        }
    }
}
