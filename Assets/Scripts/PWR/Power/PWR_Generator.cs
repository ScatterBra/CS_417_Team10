using System;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// A generator burns one fuel type into Energy over time (rubric: Passive Stat Gain, Euler
    /// integration) and its PWR_PowerNode pushes that Energy to the neighboring battery bank.
    /// Refueling is a Caretaking interaction: pull the trigger on it to spend one fuel resource
    /// and raise its fuel stat.
    /// </summary>
    [RequireComponent(typeof(PWR_StatBlock), typeof(PWR_PowerNode))]
    public class PWR_Generator : MonoBehaviour
    {
        public string displayName = "Coal Generator";
        public string fuelStat = "CoalLevel";
        public PWR_ResourceType fuelResource = PWR_ResourceType.Coal;

        [Tooltip("Fuel stat gained per unit of fuel resource (1 unit of fuel stat burns per second).")]
        public float fuelPerUnit = 25f;
        public float burnPerSecond = 1f;
        public string energyStat = "Energy";
        public float energyPerSecond = 6f;

        [Header("Feedback")]
        public Material blastMaterial;
        public Light runningLight;

        public static event Action<PWR_Generator> AnyIgnited;
        public static event Action<PWR_Generator> AnyRefueled;

        public bool IsRunning { get; private set; }

        private PWR_StatBlock stats;

        private void Awake()
        {
            stats = GetComponent<PWR_StatBlock>();
            if (TryGetComponent(out PWR_Interactable interactable)) interactable.Used += _ => Refuel();
        }

        public bool Refuel()
        {
            var res = PWR_PlayerResources.Instance;
            Vector3 fbPos = transform.position + Vector3.up * 1.4f;
            if (res == null) return false;
            if (res.Get(fuelResource) < 1)
            {
                PWR_Feedback.Show(fbPos, $"Needs {PWR_Names.Of(fuelResource)}", PWR_Feedback.Bad);
                return false;
            }
            if (stats.GetSpace(fuelStat) < fuelPerUnit - 0.01f)
            {
                PWR_Feedback.Show(fbPos, "Tank is full", PWR_Feedback.Info);
                return false;
            }
            res.Add(fuelResource, -1);
            stats.Add(fuelStat, fuelPerUnit);
            PWR_Feedback.Show(fbPos, $"+{fuelPerUnit:0} {fuelStat}", PWR_Feedback.Good);
            AnyRefueled?.Invoke(this);
            return true;
        }

        private void Update()
        {
            float fuel = stats.Get(fuelStat);
            bool canRun = fuel > 0f && stats.GetSpace(energyStat) > 0.01f;
            if (canRun)
            {
                float dt = Time.deltaTime;
                float burned = -stats.Add(fuelStat, -burnPerSecond * dt);
                float fraction = burnPerSecond * dt > 0f ? burned / (burnPerSecond * dt) : 0f;
                stats.Add(energyStat, energyPerSecond * dt * fraction);
            }

            if (canRun && !IsRunning)
            {
                PWR_BlastEffect.Spawn(transform.position + Vector3.up * 0.03f, 1.6f, blastMaterial);
                AnyIgnited?.Invoke(this);
            }
            IsRunning = canRun;
            if (runningLight != null) runningLight.enabled = IsRunning;
        }
    }
}
