using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class DEF_WaveManager : MonoBehaviour
{
    public enum Phase { Idle, Spawning, Active, Cleared }

    public Phase phase = Phase.Idle;
    public int baseZombies = 4;
    public int extraPerWave = 3;
    public float spawnInterval = 1.2f;
    public float hpScalePerWave = 0.15f;

    int toSpawn = 0;
    float spawnTimer = 0f;
    List<Vector3> spawnPoints = new List<Vector3>();

    void Start()
    {
        var grid = FindObjectOfType<DEF_GridManager>();
        if (grid == null) return;
        for (int i = 2; i < grid.width - 2; i += 4)
        {
            spawnPoints.Add(grid.TileCenter(i, 1) + Vector3.up * 0.5f);
            spawnPoints.Add(grid.TileCenter(i, grid.depth - 2) + Vector3.up * 0.5f);
        }
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame)
        {
            if (phase == Phase.Idle || phase == Phase.Cleared) StartWave();
        }

        if (phase == Phase.Spawning)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f && toSpawn > 0)
            {
                spawnTimer = spawnInterval;
                SpawnOne();
                toSpawn--;
                if (toSpawn <= 0) phase = Phase.Active;
            }
        }
        else if (phase == Phase.Active)
        {
            if (DEF_Zombie.all.Count == 0)
            {
                phase = Phase.Cleared;
            }
            else
            {
                var s = DEF_GameState.I;
                if (s != null && s.playerHealth <= 0f) phase = Phase.Idle;
            }
        }
    }

    public void StartWave()
    {
        var s = DEF_GameState.I;
        if (s == null) return;
        if (s.playerHealth <= 0f) s.playerHealth = s.maxHealth;
        s.waveNumber++;
        int threatBonus = 0;
        var tm = DEF_ThreatMeter.I;
        if (tm != null) threatBonus = Mathf.FloorToInt(tm.threat / 10f);
        toSpawn = baseZombies + (s.waveNumber - 1) * extraPerWave + threatBonus;
        spawnTimer = 0.5f;
        phase = Phase.Spawning;
    }

    void SpawnOne()
    {
        if (spawnPoints.Count == 0) return;
        Vector3 p = spawnPoints[Random.Range(0, spawnPoints.Count)];
        var s = DEF_GameState.I;
        float scale = 1f + (s != null ? (s.waveNumber - 1) * hpScalePerWave : 0f);
        DEF_Zombie.Spawn(p, scale);
    }

    void OnGUI()
    {
        var s = DEF_GameState.I;
        if (s == null) return;
        int rx = Screen.width - 250;
        GUI.Box(new Rect(rx, 10, 240, 150), "Wave Control");
        GUI.Label(new Rect(rx + 10, 35, 220, 20), "Phase: " + phase);
        GUI.Label(new Rect(rx + 10, 55, 220, 20), "Zombies: " + DEF_Zombie.all.Count);
        GUI.Label(new Rect(rx + 10, 75, 220, 20), "Scrap: " + s.scrap);
        var tm = DEF_ThreatMeter.I;
        if (tm != null) GUI.Label(new Rect(rx + 10, 95, 220, 20), "Threat: " + (int)tm.threat);
        if (phase == Phase.Idle || phase == Phase.Cleared)
        {
            if (GUI.Button(new Rect(rx + 10, 120, 220, 25), "Start Wave [Space]")) StartWave();
        }
        GUI.Label(new Rect(rx, 170, 240, 80),
            "T turret (40) | Shift+T boom (70)\nB barricade (20) | R repair (10)\nH harvest ammo crate");
    }
}
