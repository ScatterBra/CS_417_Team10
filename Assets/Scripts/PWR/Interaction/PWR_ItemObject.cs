using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// A physical item the player carries (battery, light bulb, charged cell). Crafted items are
    /// also counted as a player Resource while they exist; consuming one takes it back.
    /// </summary>
    public class PWR_ItemObject : MonoBehaviour
    {
        public PWR_ResourceType resource = PWR_ResourceType.Battery;

        [Tooltip("True for crafted items that were added to the player's resources.")]
        public bool countsAsResource = true;

        [Tooltip("Respawn at the start position if the item falls out of the world.")]
        public float fallLimitY = -3f;

        public bool Consumed { get; private set; }

        private Vector3 home;
        private Quaternion homeRotation;

        private void Start()
        {
            home = transform.position;
            homeRotation = transform.rotation;
        }

        private void Update()
        {
            if (transform.position.y > fallLimitY) return;
            if (TryGetComponent(out Rigidbody rb) && !rb.isKinematic) rb.linearVelocity = Vector3.zero;
            transform.SetPositionAndRotation(home, homeRotation);
        }

        /// <summary>Uses the item up: removes it from the player's resources and the world.</summary>
        public void Consume()
        {
            if (Consumed) return;
            Consumed = true;
            if (countsAsResource && PWR_PlayerResources.Instance != null)
                PWR_PlayerResources.Instance.Add(resource, -1);
            if (TryGetComponent(out PWR_Grabbable grabbable)) grabbable.ForceRelease();
            Destroy(gameObject);
        }
    }
}
