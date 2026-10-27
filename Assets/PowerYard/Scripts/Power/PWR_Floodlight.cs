using System;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// The room's floodlight. Its Brightness drains every second and is refilled by the neighboring
    /// battery bank (Neighbor Interaction). The bulb is a separate instance whose BulbLife stat
    /// wears down while lit; when BulbLife hits zero the bulb is destroyed (Death). A crafted
    /// light bulb dropped into the socket installs a new one.
    /// </summary>
    [RequireComponent(typeof(PWR_StatBlock), typeof(PWR_PowerNode))]
    public class PWR_Floodlight : MonoBehaviour, PWR_ISocketOwner
    {
        public string brightnessStat = "Brightness";
        public Light lamp;
        public float maxIntensity = 6f;

        [Header("Bulb")]
        public Transform bulbMount;
        public GameObject bulbPrefab;
        public PWR_StatBlock bulb;
        public string bulbLifeStat = "BulbLife";
        public float bulbWearPerSecond = 0.3f;

        public static event Action<PWR_Floodlight> BulbBroken;
        public static event Action<PWR_Floodlight> BulbInstalled;

        public bool HasBulb => bulb != null;
        public bool IsLit => HasBulb && LightFraction > 0.05f;
        /// <summary>A replacement bulb has been installed (the original one only fails once).</summary>
        public bool BulbReplaced => HasBulb && bulbUnbreakable;

        /// <summary>0..1 of how bright the lamp currently is.</summary>
        public float LightFraction => HasBulb ? stats.GetNormalized(brightnessStat) : 0f;

        private PWR_StatBlock stats;
        private PWR_PowerNode node;
        private bool hadBulb;
        private bool bulbUnbreakable;

        private void Awake()
        {
            stats = GetComponent<PWR_StatBlock>();
            node = GetComponent<PWR_PowerNode>();
            hadBulb = HasBulb;
            if (TryGetComponent(out PWR_Interactable interactable))
                interactable.Used += held =>
                {
                    var item = held != null ? held.GetComponentInParent<PWR_ItemObject>() : null;
                    var socket = GetComponentInChildren<PWR_ItemSocket>();
                    if (item != null && socket != null && socket.TryInsert(item)) return;
                    PWR_Feedback.Show(transform.position + Vector3.up * 2f, UseText(item), PWR_Feedback.Info);
                };
        }

        /// <summary>What happens when the player uses the lamp (with or without an item in hand).</summary>
        private string UseText(PWR_ItemObject item)
        {
            if (item != null && item.resource != PWR_ResourceType.LightBulb)
                return $"The lamp takes a Light Bulb, not a {PWR_Names.Of(item.resource)}.";
            if (item != null && HasBulb)
                return "The lamp already has a working bulb. Keep the spare for when it burns out.";
            if (!HasBulb)
                return "The lamp has no bulb. Craft a Light Bulb (1 Glass + 2 Copper Wire) and drop it in.";
            return IsLit ? "Powered by a nearby Battery Bank."
                : "No power. Place a charged Battery Bank within 2 cells of the lamp.";
        }

        private void Update()
        {
            node.acceptingPower = HasBulb;
            if (lamp != null) lamp.intensity = maxIntensity * LightFraction;

            if (bulb != null && !bulbUnbreakable)
                bulb.SetRate(bulbLifeStat, IsLit ? -bulbWearPerSecond : 0f);

            if (hadBulb && !HasBulb) BulbBroken?.Invoke(this);
            hadBulb = HasBulb;
        }

        /// <summary>Burns the bulb out now (the level director calls this at 80% charge).</summary>
        public void BreakBulb()
        {
            if (bulb == null || bulbUnbreakable) return;
            bulb.Set(bulbLifeStat, 0f); // PWR_DeathOnZero on the bulb destroys it
        }

        public bool CanAcceptItem(PWR_ItemObject item) => !HasBulb && item.resource == PWR_ResourceType.LightBulb;

        public void OnItemAccepted(PWR_ItemObject item)
        {
            var go = Instantiate(bulbPrefab, bulbMount != null ? bulbMount : transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            bulb = go.GetComponent<PWR_StatBlock>();
            // The replacement bulb lasts for the rest of the level, so the bulb only fails once.
            bulbUnbreakable = true;
            bulb.SetRate(bulbLifeStat, 0f);
            hadBulb = true;
            PWR_Feedback.Show(transform.position + Vector3.up * 2f, "New bulb installed", PWR_Feedback.Good);
            BulbInstalled?.Invoke(this);
        }
    }
}
