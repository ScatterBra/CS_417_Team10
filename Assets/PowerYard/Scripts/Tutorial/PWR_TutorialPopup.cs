using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>Keeps a popup next to its anchor without inheriting the anchor's scale.</summary>
    public class PWR_TutorialPopup : MonoBehaviour
    {
        public Transform anchor;
        public Vector3 offset;

        public void LateUpdate()
        {
            if (anchor == null)
            {
                Destroy(gameObject);
                return;
            }
            transform.position = anchor.position + offset;
        }
    }
}
