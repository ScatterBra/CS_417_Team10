using System;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// Rubric: Shatter Affordance. Hitting the crate with the wrench lowers its Integrity;
    /// at zero the crate is replaced by harvestable coal chunks.
    /// </summary>
    [RequireComponent(typeof(PWR_StatBlock))]
    public class PWR_Shatterable : MonoBehaviour
    {
        public string statName = "Integrity";
        public GameObject piecePrefab;
        public int pieceCount = 2;
        public float minImpactSpeed = 1.0f;
        public float hitCooldown = 0.35f;

        public static event Action<PWR_Shatterable> AnyShattered;

        private PWR_StatBlock stats;
        private float lastHit = -10f;
        private Vector3 baseScale;
        private float punch;
        private bool shattered;

        private void Awake()
        {
            stats = GetComponent<PWR_StatBlock>();
            baseScale = transform.localScale;
            if (TryGetComponent(out PWR_Interactable interactable))
                interactable.Used += held =>
                {
                    if (held != null && held.GetComponentInParent<PWR_Wrench>() != null) Hit();
                    else PWR_Feedback.Show(transform.position + Vector3.up, "Too sturdy to open by hand. Grab the Wrench from the Workbench and hit the crate.", PWR_Feedback.Info);
                };
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.GetComponentInParent<PWR_Wrench>() == null) return;
            if (collision.relativeVelocity.magnitude < minImpactSpeed) return;
            Hit();
        }

        public void Hit()
        {
            if (shattered || Time.time - lastHit < hitCooldown) return;
            lastHit = Time.time;
            stats.Add(statName, -1f);
            punch = 1f;
            PWR_Feedback.Show(transform.position + Vector3.up, "Crack!", PWR_Feedback.Info);
            if (stats.Get(statName) <= 0f) Shatter();
        }

        private void Shatter()
        {
            shattered = true;
            for (int i = 0; i < pieceCount && piecePrefab != null; i++)
            {
                Vector3 offset = new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), 0.25f + 0.15f * i,
                    UnityEngine.Random.Range(-0.25f, 0.25f));
                Instantiate(piecePrefab, transform.position + offset, UnityEngine.Random.rotation);
            }
            AnyShattered?.Invoke(this);
            Destroy(gameObject);
        }

        private void Update()
        {
            if (punch <= 0f) return;
            punch = Mathf.Max(0f, punch - Time.deltaTime * 5f);
            transform.localScale = baseScale * (1f - 0.12f * Mathf.Sin(punch * Mathf.PI));
        }
    }
}
