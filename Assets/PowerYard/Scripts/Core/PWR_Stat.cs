using System;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// One named, clamped value on an instance (e.g. "CoalLevel", "Charge", "BulbLife").
    /// ratePerSecond != 0 makes PWR_StatBlock advance it passively each frame (Euler integration).
    /// </summary>
    [Serializable]
    public class PWR_Stat
    {
        public string name = "Stat";
        public float value;
        public float min = 0f;
        public float max = 100f;

        [Tooltip("Passive change per second (Euler integration). Negative = drains over time.")]
        public float ratePerSecond;

        [Tooltip("Passive drain never pushes the value below this (scripts can still set it lower).")]
        public bool usePassiveFloor;
        public float passiveFloor;

        public float Normalized => Mathf.Approximately(max, min) ? 0f : Mathf.InverseLerp(min, max, value);
        public bool IsEmpty => value <= min + 0.0001f;
        public bool IsFull => value >= max - 0.0001f;
        public float Space => max - value;

        public PWR_Stat() { }

        public PWR_Stat(string name, float value, float min, float max, float ratePerSecond = 0f)
        {
            this.name = name;
            this.value = value;
            this.min = min;
            this.max = max;
            this.ratePerSecond = ratePerSecond;
        }
    }
}
