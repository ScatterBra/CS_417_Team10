using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class DEF_WaveManager : MonoBehaviour
{
    public enum Phase { Idle, Spawning, Active, Cleared }

    public static DEF_WaveManager I;

    public Phase phase = Phase.Idle;
    public int baseZombies = 4;
    public int extraPerWave = 2;
    public float spawnInterval = 2f;
    public float hpScalePerWave = 0.1f;
    public float stallTimeout = 40f;
    public float threat = 0f;
    public float threatPerSecond = 0.08f;
    public float maxThreat = 100f;
    public DEF_ApproachPath approachPath;
    bool wasDead = false;

    int spawnedThisWave;
    int toSpawn = 0;
    float spawnTimer = 0f;
    int lastCount = 0;
    float stallTimer = 0f;
    float stragglerTimer;
    List<Vector3> spawnPoints = new List<Vector3>();
    List<Vector2Int> portalCells = new List<Vector2Int>();

    void Awake()
    {
        I = this;
    }

    public void AddThreat(float amount)
    {
        threat = Mathf.Min(maxThreat, threat + amount);
    }

    void Start()
    {
        // Give a first-time PC player time to read controls and place defenses.
        stragglerTimer = Random.Range(45f, 60f);
        var grid = FindFirstObjectByType<DEF_GridManager>();
        if (grid == null) return;
        for (int i = 2; i < grid.width - 2; i += 4)
        {
            spawnPoints.Add(grid.TileCenter(i, 1));
            portalCells.Add(new Vector2Int(i, 1));
            spawnPoints.Add(grid.TileCenter(i, grid.depth - 2));
            portalCells.Add(new Vector2Int(i, grid.depth - 2));
            spawnPoints.Add(grid.TileCenter(1, i));
            portalCells.Add(new Vector2Int(1, i));
            spawnPoints.Add(grid.TileCenter(grid.width - 2, i));
            portalCells.Add(new Vector2Int(grid.width - 2, i));
        }
        var pf = DEF_Pathfinder.I;
        SeedCrates();
    }

    List<Vector3> MakeTrail(Vector2Int portal, DEF_GridManager grid)
    {
        var points = new List<Vector3>();
        if (approachPath == null) return points;
        bool side = portal.x == 1 || portal.x == grid.width - 2;
        int inward = (side ? portal.x > grid.width / 2 : portal.y > grid.depth / 2) ? -1 : 1;
        Vector3 entry = grid.TileCenter(portal.x, portal.y);
        foreach (var offset in approachPath.waypoints)
        {
            Vector3 local = side ? new Vector3(inward * offset.y, 0, offset.x) : new Vector3(offset.x, 0, inward * offset.y);
            points.Add(entry + grid.transform.TransformVector(local));
        }
        return points;
    }

    void Update()
    {
        if (DEF_HUD.TrainingActive || DEF_GameState.I == null) return;
        if (DEF_GameState.I.playerHealth <= 0) { if (!wasDead) DEF_Audio.PlayUI("game_over", .8f); wasDead = true; toSpawn = 0; phase = Phase.Idle; return; }
        threat = Mathf.Min(maxThreat, threat + threatPerSecond * Time.deltaTime);

        stragglerTimer -= Time.deltaTime;
        if (stragglerTimer <= 0f)
        {
            stragglerTimer = Random.Range(25f, 45f);
            if ((phase == Phase.Idle || phase == Phase.Cleared)
                && spawnPoints.Count > 0 && DEF_Zombie.all.Count < 6)
            {
                SpawnAtPortal(Random.Range(0, spawnPoints.Count), 1f);
            }
        }

        var s = DEF_GameState.I;
        bool dead = s != null && s.playerHealth <= 0f;
        if (dead && !wasDead) DEF_Audio.PlayUI("game_over", 0.8f);
        wasDead = dead;

        var kb = Keyboard.current;
        if (!DEF_VRInput.Active && kb != null && kb.spaceKey.wasPressedThisFrame)
        {
            if (phase == Phase.Idle || phase == Phase.Cleared) StartWave();
        }

        dead = s != null && s.playerHealth <= 0f;

        if (phase == Phase.Spawning)
        {
            if (dead)
            {
                toSpawn = 0;
                phase = Phase.Idle;
            }
            else
            {
                spawnTimer -= Time.deltaTime;
                if (spawnTimer <= 0f && toSpawn > 0)
                {
                    spawnTimer = spawnInterval;
                    SpawnOne();
                    toSpawn--;
                    if (toSpawn <= 0)
                    {
                        phase = Phase.Active;
                        lastCount = DEF_Zombie.all.Count;
                        stallTimer = 0f;
                    }
                }
            }
        }
        else if (phase == Phase.Active)
        {
            int count = DEF_Zombie.all.Count;
            if (dead)
            {
                phase = Phase.Idle;
            }
            else if (count == 0)
            {
                phase = Phase.Cleared;
                s.scrap = Mathf.Min(s.maxScrap, s.scrap + 30);
                s.ammo = Mathf.Min(s.maxAmmo, s.ammo + 15);
                foreach (var crate in FindObjectsByType<DEF_AmmoCrate>(FindObjectsSortMode.None)) crate.Restock();
                DEF_HUD.Instance?.Notify("WAVE CLEARED  /  +30 scrap · +15 ammo · Crates restocked");
                DEF_Audio.PlayUI("victory", 0.6f);
            }
            else if (count < lastCount)
            {
                lastCount = count;
                stallTimer = 0f;
            }
            else
            {
                stallTimer += Time.deltaTime;
                if (stallTimer >= stallTimeout)
                {
                    for (int i = DEF_Zombie.all.Count - 1; i >= 0; i--)
                    {
                        var z = DEF_Zombie.all[i];
                        if (z != null) z.Repath();
                    }
                    stallTimer = 0f;
                }
            }
        }
    }

    public void StartWave()
    {
        var s = DEF_GameState.I;
        if (s == null || DEF_HUD.BlockingWorldInput || (phase != Phase.Idle && phase != Phase.Cleared)) return;
        if (DEF_HUD.TrainingActive && !DEF_HUD.Instance.ReadyForWave) return;
        if (DEF_HUD.TrainingActive) DEF_HUD.Instance.FinishTraining();
        spawnedThisWave = 0;
        if (s.playerHealth <= 0f) ResetRun(s);
        s.waveNumber++;
        int threatBonus = Mathf.FloorToInt(threat / 25f);
        toSpawn = baseZombies + (s.waveNumber - 1) * extraPerWave + threatBonus;
        spawnTimer = 0.5f;
        lastCount = 0;
        stallTimer = 0f;
        phase = Phase.Spawning;
        DEF_HUD.Instance?.Notify("WAVE " + s.waveNumber.ToString("00") + " INCOMING  /  Defend all four sides");
        DEF_Audio.PlayUI("wave_start", 0.7f);
        if (spawnPoints.Count > 0)
            DEF_Tutorials.Show("wave", Camera.main != null ? Camera.main.transform.position + Camera.main.transform.forward * 3f - Vector3.up : transform.position, "WAVE ALARM\nInfected approach from all four sides. Turn and defend each window.");
    }

    void ResetRun(DEF_GameState s)
    {
        for (int i = DEF_Zombie.all.Count - 1; i >= 0; i--)
        {
            var z = DEF_Zombie.all[i];
            if (z != null) Destroy(z.gameObject);
        }
        s.playerHealth = s.maxHealth;
        s.ammo = s.maxAmmo;
        s.waveNumber = 0;
        threat = 0f;
        SeedCrates();
    }

    void SeedCrates()
    {
        var grid = FindFirstObjectByType<DEF_GridManager>();
        var pf = DEF_Pathfinder.I;
        if (grid == null) return;
        foreach (var c in FindObjectsByType<DEF_AmmoCrate>(FindObjectsSortMode.None))
        {
            c.ClearCell();
            Destroy(c.gameObject);
        }
        Vector2Int bc = pf != null ? pf.BaseCell() : new Vector2Int(grid.width / 2, grid.depth / 2);
        int placed = 0;
        int guard = 0;
        while (placed < 3 && guard < 200)
        {
            guard++;
            int x = Random.Range(2, grid.width - 2);
            int z = Random.Range(2, grid.depth - 2);
            if (Mathf.Abs(x - bc.x) < 4 && Mathf.Abs(z - bc.y) < 4) continue;
            if (pf != null && pf.IsBlocked(x, z)) continue;
            DEF_AmmoCrate.SpawnAt(grid.TileCenter(x, z) + Vector3.up * 0.1f, 40);
            placed++;
        }
    }

    void SpawnOne()
    {
        if (spawnPoints.Count == 0) return;
        int pi = Random.Range(0, spawnPoints.Count);
        var s = DEF_GameState.I;
        float scale = 1f + (s != null ? (s.waveNumber - 1) * hpScalePerWave : 0f);
        SpawnAtPortal(pi, scale);
        spawnedThisWave++;
    }

    void SpawnAtPortal(int pi, float hpScale)
    {
        var grid = FindFirstObjectByType<DEF_GridManager>();
        Vector2Int portal = portalCells[pi];
        List<Vector3> trail = null;
        if (grid != null) trail = MakeTrail(portal, grid);
        int wave = DEF_GameState.I != null ? DEF_GameState.I.waveNumber : 0;
        var kind = wave >= 3 && spawnedThisWave % 5 == 0 ? DEF_Zombie.Kind.Brute :
            wave >= 2 && spawnedThisWave % 3 == 1 ? DEF_Zombie.Kind.Runner : DEF_Zombie.Kind.Walker;
        DEF_Zombie.Spawn(spawnPoints[pi], hpScale, trail, kind);
    }

}
