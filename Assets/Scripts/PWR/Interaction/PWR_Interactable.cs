using System;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// Something the player can "use": point and pull the trigger in VR, press E on desktop,
    /// or pull the trigger while holding it. Other components subscribe to <see cref="Used"/>.
    /// </summary>
    public class PWR_Interactable : MonoBehaviour
    {
        [Tooltip("Shown in prompts, e.g. \"Refuel\", \"Draw oil\".")]
        public string verb = "Use";
        public float cooldown = 0.25f;

        /// <summary>Argument: the held item used on this object (null when used empty-handed).</summary>
        public event Action<GameObject> Used;

        private float lastUse = -10f;

        public bool Use(GameObject heldItem = null)
        {
            if (!isActiveAndEnabled || Time.time - lastUse < cooldown) return false;
            lastUse = Time.time;
            Used?.Invoke(heldItem);
            return true;
        }
    }
}
