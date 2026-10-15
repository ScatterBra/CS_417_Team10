using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>Destroys this instance when the watched stat reaches its minimum (rubric: Death).</summary>
    [RequireComponent(typeof(PWR_StatBlock))]
    public class PWR_DeathOnZero : MonoBehaviour
    {
        public string statName = "Remaining";
        [Tooltip("Optional effect spawned where the instance dies.")]
        public GameObject deathEffect;

        private PWR_StatBlock statBlock;

        private void Awake() => statBlock = GetComponent<PWR_StatBlock>();
        private void OnEnable() => statBlock.OnStatChanged += HandleChanged;
        private void OnDisable() => statBlock.OnStatChanged -= HandleChanged;

        private void HandleChanged(string stat, float value, float delta)
        {
            if (stat != statName || delta >= 0f) return;
            if (!statBlock.GetStat(statName).IsEmpty) return;

            if (deathEffect) Instantiate(deathEffect, transform.position, transform.rotation);
            Destroy(gameObject);
        }
    }
}
