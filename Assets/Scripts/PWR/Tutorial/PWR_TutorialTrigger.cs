using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// Fires a one-time tutorial for this object. Proximity mode measures distance to the
    /// main camera (works with an XR rig, no player collider needed). Manual mode is for
    /// event-driven tutorials: call Trigger() from another script.
    /// </summary>
    public class PWR_TutorialTrigger : MonoBehaviour
    {
        public enum Mode { Proximity, Manual }

        public string tutorialId = "Generator";
        [TextArea(2, 5)] public string message = "Pull the trigger here to load fuel.";
        public Mode mode = Mode.Proximity;
        public float radius = 2.5f;
        public Vector3 offset = new Vector3(0f, 1.8f, 0f);

        private bool done;

        private void Update()
        {
            if (done || mode != Mode.Proximity) return;
            var mgr = PWR_TutorialManager.Instance;
            if (mgr != null && mgr.HasShown(tutorialId))
            {
                done = true;
                return;
            }
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 d = cam.transform.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude <= radius * radius) Trigger();
        }

        public void Trigger()
        {
            if (done) return;
            var mgr = PWR_TutorialManager.Instance;
            if (mgr == null) return;
            mgr.ShowOnce(tutorialId, message, transform, offset);
            done = mgr.HasShown(tutorialId);
        }

        private void OnDrawGizmosSelected()
        {
            if (mode != Mode.Proximity) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
