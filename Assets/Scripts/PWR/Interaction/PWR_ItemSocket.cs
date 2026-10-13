using UnityEngine;

namespace Deadzone.Power
{
    public interface PWR_ISocketOwner
    {
        bool CanAcceptItem(PWR_ItemObject item);
        void OnItemAccepted(PWR_ItemObject item);
    }

    /// <summary>
    /// Trigger zone that takes a matching item (battery into the charger, bulb into the lamp).
    /// The owner in a parent decides whether it is accepted right now.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PWR_ItemSocket : MonoBehaviour
    {
        public PWR_ResourceType accepts = PWR_ResourceType.Battery;

        private PWR_ISocketOwner owner;

        private void Awake()
        {
            owner = GetComponentInParent<PWR_ISocketOwner>();
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other) => TryInsert(other.GetComponentInParent<PWR_ItemObject>());

        private void OnTriggerStay(Collider other) => TryInsert(other.GetComponentInParent<PWR_ItemObject>());

        /// <summary>Inserts the item if it matches and the owner accepts it. Returns true on success.</summary>
        public bool TryInsert(PWR_ItemObject item)
        {
            if (item == null || item.Consumed || item.resource != accepts || owner == null) return false;
            if (!owner.CanAcceptItem(item)) return false;
            owner.OnItemAccepted(item);
            item.Consume();
            return true;
        }
    }
}
