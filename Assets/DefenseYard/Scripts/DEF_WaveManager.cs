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
    public bool AttemptFailed { get; private set; }
    public string LastFailure { get; private set; }
    readonly List<DEF_Zombie> attemptActors = new();
    readonly List<DEF_Projectile> attemptProjectiles = new();
    int expectedSpawns;
    public bool OwnsActor(DEF_Zombie actor)=>actor!=null && attemptActors.Contains(actor);
    public bool OwnsProjectile(DEF_Projectile projectile)=>projectile!=null && attemptProjectiles.Contains(projectile);
    public void TrackProjectile(DEF_Projectile projectile) { if(projectile!=null) attemptProjectiles.Add(projectile); }
    void FailAttempt(string reason)
    {
        AttemptFailed=true; toSpawn=0; phase=Phase.Idle;
        foreach(var actor in attemptActors)if(actor!=null)actor.gameObject.SetActive(false);
        foreach(var projectile in attemptProjectiles)if(projectile!=null)projectile.gameObject.SetActive(false);
        LastFailure="Cannot complete wave: "+reason+". Correct the wave setup if needed, then start again to retry Defense preparation.";
        DEF_HUD.Instance?.Notify(LastFailure);
        DEF_Progression.Current?.Reject("complete wave",reason,"Start again to reset this Defense attempt; team prerequisites remain earned.");
    }
    public bool TryRetryDefense()
    {
        var p=DEF_Progression.Current; var state=DEF_GameState.I;
        if(p==null || state==null || (!AttemptFailed && state.playerHealth>0))return false;
        if(p.workshopRecording){p.BeginNewSession();return true;}
        if(p.DeferReset(false))return true;
        ResetOwnedWave();
        p.ResetDefenseAttempt();
        DEF_HUD.Instance?.Notify("Defense retry ready: base and Defense budget restored. Place a sentry, then deliver fresh repair and ammo supplies. Team prerequisites retained.");
        return true;
    }
    internal static void Retire(GameObject go)
    {
        if(go==null)return;
        go.SetActive(false);
        if(Application.isPlaying)Destroy(go);else DestroyImmediate(go);
    }
    internal void RetireProjectiles()
    {
        var retired=attemptProjectiles.ToArray();
        attemptProjectiles.Clear(); // Revoke evidence before deferred destruction or callbacks.
        foreach(var projectile in retired)if(projectile!=null){projectile.Disarm();Retire(projectile.gameObject);}
    }
    internal void ResetOwnedWave()
    {
        foreach(var z in attemptActors)if(z!=null){DEF_Zombie.all.Remove(z);Retire(z.gameObject);}
        RetireProjectiles();
        attemptActors.Clear();attemptProjectiles.Clear();
        phase=Phase.Idle;toSpawn=spawnedThisWave=expectedSpawns=lastCount=0;
        spawnTimer=stallTimer=threat=0;stragglerTimer=60;wasDead=false;AttemptFailed=false;
    }
    DEF_Zombie.Kind KindFor(int wave,int index) => wave>=3 && index%5==0 ? DEF_Zombie.Kind.Brute :
        wave>=2 && index%3==1 ? DEF_Zombie.Kind.Runner : DEF_Zombie.Kind.Walker;
    public bool ValidateSpawnConfiguration(int wave,int quota,out string reason)
    {
        reason=null;
        if(quota<=0 || spawnPoints.Count==0 || spawnPoints.Count!=portalCells.Count)
            reason="spawn points or enemy quota are missing";
        else if(!float.IsFinite(spawnInterval) || spawnInterval<=0 || !float.IsFinite(hpScalePerWave) || hpScalePerWave<0)
            reason="spawn timing or health scaling is invalid";
        else
        {
            foreach(var point in spawnPoints)if(!float.IsFinite(point.x)||!float.IsFinite(point.y)||!float.IsFinite(point.z)){reason="a spawn point is invalid";break;}
            for(int i=0;reason==null && i<quota;i++)
            {
                var kind=KindFor(wave,i);
                var prefab=Resources.Load<GameObject>(kind==DEF_Zombie.Kind.Walker?"Prefabs/Zombie":"Prefabs/Zombie_"+kind);
                if(prefab==null || prefab.GetComponent<DEF_Zombie>()==null)reason="a required enemy prefab is missing or invalid";
            }
        }
        return reason==null;
    }

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
        if(DEF_Progression.Current!=null && (AttemptFailed || DEF_GameState.I.playerHealth<=0))
        {
            if(!AttemptFailed)FailAttempt("the base was destroyed");
            if(!DEF_VRInput.Active && Keyboard.current!=null && Keyboard.current.spaceKey.wasPressedThisFrame)StartWave();
            return;
        }
        if (DEF_Progression.Current != null && !DEF_Progression.Current.IsAllowed(DEF_Action.Combat))
        {
            if (!DEF_VRInput.Active && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) StartWave();
            return;
        }
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
            StartWave();
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
                    if(!SpawnOne()){FailAttempt("an enemy could not spawn");return;}
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
            else if (count == 0 && spawnedThisWave > 0 && spawnedThisWave == expectedSpawns && toSpawn == 0 && !AttemptFailed)
            {
                RetireProjectiles();
                phase = Phase.Cleared;
                DEF_Progression.Current?.WaveCleared(s.waveNumber, s.playerHealth);
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
        if(DEF_Progression.Current!=null && (AttemptFailed || (DEF_GameState.I!=null && DEF_GameState.I.playerHealth<=0)))
        {TryRetryDefense();return;}
        if (phase == Phase.Spawning || phase == Phase.Active)
        {
            LastFailure = "Cannot start wave: a wave is already active. Clear the current enemies while keeping the base alive, then start the next wave.";
            DEF_HUD.Instance?.Notify(LastFailure);
            DEF_Progression.Current?.Reject("start wave", "a wave is already active", "Clear the current enemies while keeping the base alive, then start the next wave.");
            return;
        }
        if (!DEF_Progression.Allows(DEF_Action.StartWave)) return;
        var s = DEF_GameState.I;
        if (s == null || DEF_HUD.BlockingWorldInput || (phase != Phase.Idle && phase != Phase.Cleared)) return;
        int nextWave=s.playerHealth<=0 ? 1 : s.waveNumber+1;
        if(DEF_Progression.Current!=null)
        {
            var stage=DEF_Progression.Current.State.Current;
            nextWave=stage==DEF_ProgressionState.Stage.WaveReady ? 1 : stage==DEF_ProgressionState.Stage.JuggernautReady ? 3 : 2;
        }
        long quota=(long)baseZombies+(long)(nextWave-1)*extraPerWave+Mathf.FloorToInt(threat/25f);
        if(quota<=0 || quota>10000 || !ValidateSpawnConfiguration(nextWave,(int)quota,out _))
        {
            LastFailure="Cannot start wave: spawn configuration is invalid. Configure valid portal points, enemy prefabs, quota and timing, then retry.";
            DEF_HUD.Instance?.Notify(LastFailure);
            DEF_Progression.Current?.Reject("start wave","spawn configuration is invalid","Configure valid portal points, enemy prefabs, quota and timing, then retry.");
            return;
        }
        if (DEF_HUD.TrainingActive && !DEF_HUD.Instance.ReadyForWave) return;
        if (DEF_HUD.TrainingActive) DEF_HUD.Instance.FinishTraining();
        RetireProjectiles();
        if (DEF_Progression.Current != null && (s.playerHealth <= 0 || !DEF_Progression.Current.WaveStarting())) return;
        spawnedThisWave = 0;
        if (s.playerHealth <= 0f) ResetRun(s);
        s.waveNumber=nextWave;

        toSpawn = expectedSpawns = (int)quota; AttemptFailed=false;
        spawnTimer = 0.5f;
        lastCount = 0;
        stallTimer = 0f;
        phase = Phase.Spawning;
        DEF_HUD.Instance?.Notify("WAVE " + s.waveNumber.ToString("00") + (DEF_Progression.Current!=null && DEF_Progression.Current.workshopRecording ? " INCOMING  /  Defend the east approach" : " INCOMING  /  Defend all four sides"));
        DEF_Audio.PlayUI("wave_start", 0.7f);
        if (spawnPoints.Count > 0)
            DEF_Tutorials.Show("wave", Camera.main != null ? Camera.main.transform.position + Camera.main.transform.forward * 3f - Vector3.up : transform.position, (DEF_Progression.Current!=null && DEF_Progression.Current.workshopRecording ? "RECORDING WAVE\nInfected use the east approach. Defend the east window." : "WAVE ALARM\nInfected approach from all four sides. Turn and defend each window."));
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
        if (DEF_Progression.Current != null) return;
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

    bool SpawnOne()
    {
        if(spawnPoints.Count==0)return false;
        var s=DEF_GameState.I;
        float scale=1f+(s!=null?(s.waveNumber-1)*hpScalePerWave:0);
                int portal=Random.Range(0,spawnPoints.Count);
        if(DEF_Progression.Current!=null && DEF_Progression.Current.workshopRecording){
            // A disclosed single east approach makes this recording loop short and repeatable.
            var target=new Vector3(17,0,1);float best=float.PositiveInfinity;
            for(int i=0;i<spawnPoints.Count;i++){float d=(spawnPoints[i]-target).sqrMagnitude;if(d<best){best=d;portal=i;}}
        }
        if(!SpawnAtPortal(portal,scale))return false;
        spawnedThisWave++;return true;
    }
    bool SpawnAtPortal(int pi,float hpScale)
    {
        if(pi<0 || pi>=spawnPoints.Count || pi>=portalCells.Count)return false;
        var grid=FindFirstObjectByType<DEF_GridManager>();
        var trail=grid!=null?MakeTrail(portalCells[pi],grid):null;
        int wave=DEF_GameState.I!=null?DEF_GameState.I.waveNumber:0;
        var actor=DEF_Zombie.Spawn(spawnPoints[pi],hpScale,trail,KindFor(wave,spawnedThisWave));
        if(actor==null)return false;
        attemptActors.Add(actor);return true;
    }
}


