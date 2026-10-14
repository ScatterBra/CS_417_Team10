using System;
using System.Collections;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// Rubric: Harvesting Affordance. A player interaction gathers from this instance —
    /// either reducing one of its Stats (OnUse) or destroying it (OnPickup) — and the
    /// player's Resource increases.
    /// </summary>
    public class PWR_Harvestable : MonoBehaviour
    {
        public enum Mode
        {
            OnUse,    // pull the trigger on it (oil barrel, electrolyte tank)
            OnPickup, // picking it up collects it (scrap, coal chunks)
        }

        public Mode mode = Mode.OnUse;
        public PWR_ResourceType resource = PWR_ResourceType.CrudeOil;
        public int amountPerHarvest = 1;

        [Tooltip("Stat reduced by each harvest (OnUse). Leave empty for none.")]
        public string statName = "";
        public float statCostPerHarvest = 1f;

        public static event Action<PWR_Harvestable> AnyHarvested;

        private PWR_StatBlock stats;
        private bool consumed;

        private void Awake()
        {
            stats = GetComponent<PWR_StatBlock>();
            if (TryGetComponent(out PWR_Interactable interactable)) interactable.Used += _ => Harvest();
            if (mode == Mode.OnPickup && TryGetComponent(out PWR_Grabbable grabbable))
                grabbable.Picked += _ => Harvest();
        }

        public bool Harvest()
        {
            if (consumed) return false;
            var res = PWR_PlayerResources.Instance;
            if (res == null) return false;

            if (mode == Mode.OnUse && !string.IsNullOrEmpty(statName) && stats != null)
            {
                if (stats.Get(statName) < statCostPerHarvest - 0.001f)
                {
                    PWR_Feedback.Show(transform.position + Vector3.up, "Empty", PWR_Feedback.Bad);
                    return false;
                }
                stats.Add(statName, -statCostPerHarvest);
            }

            res.Add(resource, amountPerHarvest);
            PWR_Feedback.Show(transform.position + Vector3.up * 0.8f,
                $"+{amountPerHarvest} {PWR_Names.Of(resource)}", PWR_Feedback.Good);
            AnyHarvested?.Invoke(this);

            if (mode == Mode.OnPickup)
            {
                consumed = true;
                StartCoroutine(DestroyAfterRelease());
            }
            return true;
        }

        private IEnumerator DestroyAfterRelease()
        {
            // Let the grab event finish before tearing the object down.
            yield return null;
            if (TryGetComponent(out PWR_Grabbable grabbable)) grabbable.ForceRelease();
            Destroy(gameObject);
        }
    }
}
