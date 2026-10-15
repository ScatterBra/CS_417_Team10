using System;
using System.Text;
using TMPro;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>Lists all player resources in one text field (e.g. on a wrist or station panel).</summary>
    public class PWR_ResourceHUD : MonoBehaviour
    {
        public TMP_Text label;
        public bool hideZero = false;

        private readonly StringBuilder sb = new StringBuilder();

        private void OnEnable()
        {
            if (PWR_PlayerResources.Instance) PWR_PlayerResources.Instance.OnResourceChanged += HandleChanged;
        }

        private void OnDisable()
        {
            if (PWR_PlayerResources.Instance) PWR_PlayerResources.Instance.OnResourceChanged -= HandleChanged;
        }

        private void Start()
        {
            // Subscribe again in case PWR_PlayerResources awoke after this component was enabled.
            if (PWR_PlayerResources.Instance)
            {
                PWR_PlayerResources.Instance.OnResourceChanged -= HandleChanged;
                PWR_PlayerResources.Instance.OnResourceChanged += HandleChanged;
            }
            Refresh();
        }

        private void HandleChanged(PWR_ResourceType type, int value, int delta) => Refresh();

        public void Refresh()
        {
            var res = PWR_PlayerResources.Instance;
            if (label == null || res == null) return;

            sb.Clear();
            foreach (PWR_ResourceType t in Enum.GetValues(typeof(PWR_ResourceType)))
            {
                int v = res.Get(t);
                if (hideZero && v == 0) continue;
                sb.Append(t).Append(": ").Append(v).Append('\n');
            }
            label.text = sb.ToString();
        }
    }
}
