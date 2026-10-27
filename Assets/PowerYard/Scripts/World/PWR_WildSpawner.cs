using System.Collections.Generic;
using UnityEngine;

namespace Deadzone.Power
{
    /// <summary>
    /// Rubric: Wild Instances. When the level starts, oil barrels and wooden crates are dealt at
    /// random onto the shared spawn points, and scrap is scattered at random positions inside the
    /// scrap zones.
    /// </summary>
    public class PWR_WildSpawner : MonoBehaviour
    {
        [Header("Barrels and crates share these points")]
        public List<Transform> spawnPoints = new List<Transform>();
        public GameObject barrelPrefab;
        public int barrelCount = 2;
        public GameObject cratePrefab;
        public int crateCount = 3;

        [Header("Scrap")]
        public GameObject scrapPrefab;
        public int scrapCount = 20;
        [Tooltip("Boxes (BoxCollider bounds, collider may be disabled) inside which scrap is scattered.")]
        public List<BoxCollider> scrapZones = new List<BoxCollider>();

        [Tooltip("Hide the editor markers on the spawn points at runtime.")]
        public bool hideMarkers = true;

        [Tooltip("Off when PWR_Progression releases barrels/crates and scrap later.")]
        public bool spawnOnStart = true;

        public int Seed { get; private set; }

        private bool barrelsSpawned, scrapSpawned;

        private void Start()
        {
            Seed = Random.Range(int.MinValue, int.MaxValue);
            if (hideMarkers)
                foreach (var p in spawnPoints)
                    if (p != null) p.gameObject.SetActive(false);
            if (!spawnOnStart) return;
            SpawnBarrelsAndCrates();
            SpawnScrap();
        }

        public void SpawnBarrelsAndCrates()
        {
            if (barrelsSpawned) return;
            barrelsSpawned = true;
            var types = new List<GameObject>();
            for (int i = 0; i < barrelCount; i++) types.Add(barrelPrefab);
            for (int i = 0; i < crateCount; i++) types.Add(cratePrefab);
            Shuffle(types);

            var points = new List<Transform>(spawnPoints);
            points.RemoveAll(p => p == null);
            Shuffle(points);

            for (int i = 0; i < types.Count && i < points.Count; i++)
            {
                if (types[i] == null) continue;
                var p = points[i];
                Vector3 pos = GroundAt(p.position);
                Instantiate(types[i], pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            }
        }

        public void SpawnScrap()
        {
            if (scrapSpawned || scrapPrefab == null || scrapZones.Count == 0) return;
            scrapSpawned = true;
            for (int i = 0; i < scrapCount; i++)
            {
                var zone = scrapZones[Random.Range(0, scrapZones.Count)];
                if (zone == null) continue;
                Vector3 c = zone.transform.TransformPoint(zone.center);
                Vector3 s = Vector3.Scale(zone.size, zone.transform.lossyScale) * 0.5f;
                Vector3 pos = c + new Vector3(Random.Range(-s.x, s.x), 0f, Random.Range(-s.z, s.z));
                Instantiate(scrapPrefab, GroundAt(pos) + Vector3.up * 0.1f, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            }
        }

        private static Vector3 GroundAt(Vector3 pos)
        {
            var grid = PWR_GridManager.Instance;
            float y = grid != null ? grid.transform.position.y : 0f;
            return new Vector3(pos.x, y, pos.z);
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
