using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Endless wave loop:
///   Rest (restTime) -> Wave N starts -> enemies spawn over time -> all dead -> wave complete -> Rest ...
/// Every eliteEveryXWaves an elite joins the wave, every bossEveryXWaves a boss does (boss wins if both match).
/// </summary>
public class Wave_Manager : MonoBehaviour
{
    public static Wave_Manager Instance;

    [Serializable]
    public class EnemySpawnEntry
    {
        public Enemy prefab;
        [Tooltip("First wave this enemy can appear in.")]
        public int minWave = 1;
        [Tooltip("Relative chance to be picked. 2 = twice as common as 1.")]
        public float weight = 1f;
    }

    [Header("Flow")]
    [SerializeField] private bool startOnPlay = true;
    [Tooltip("Delay before wave 1.")]
    [SerializeField] private float firstWaveDelay = 3f;
    [Tooltip("Rest between waves (seconds).")]
    [SerializeField] private float restTime = 15f;

    [Header("Wave Size")]
    [SerializeField] private int baseEnemyCount = 5;
    [Tooltip("Extra enemies added every wave.")]
    [SerializeField] private int enemiesPerWave = 2;
    [Tooltip("Max enemies alive at once. The rest wait until some die.")]
    [SerializeField] private int maxAlive = 20;
    [SerializeField] private float spawnInterval = 0.6f;

    [Header("Enemies")]
    [SerializeField] private EnemySpawnEntry[] enemies;

    [Header("Elite / Boss")]
    [Tooltip("Every X waves add elites. 0 = never.")]
    [SerializeField] private int eliteEveryXWaves = 5;
    [SerializeField] private Enemy[] elitePrefabs;
    [SerializeField] private int elitesPerEliteWave = 1;
    [Tooltip("Every X waves add a boss. 0 = never. Overrides the elite wave if both match.")]
    [SerializeField] private int bossEveryXWaves = 10;
    [SerializeField] private Enemy[] bossPrefabs;

    [Header("Spawn")]
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("Spawn points closer than this to the player are skipped (if possible).")]
    [SerializeField] private float minDistanceFromPlayer = 8f;
    [Tooltip("Random offset around the spawn point so enemies don't stack.")]
    [SerializeField] private float spawnRadius = 1.5f;

    public enum WaveType { Normal, Elite, Boss }

    public int CurrentWave { get; private set; }
    /// <summary>Waves fully cleared this run.</summary>
    public int WavesCompleted { get; private set; }
    /// <summary>Enemies killed this run.</summary>
    public int EnemiesKilled { get; private set; }
    public WaveType CurrentWaveType { get; private set; }
    public bool IsResting { get; private set; }
    public float RestTimeRemaining { get; private set; }
    public int EnemiesAlive => alive.Count;
    /// <summary>Enemies left in this wave (alive + not spawned yet).</summary>
    public int EnemiesRemaining => alive.Count + spawnQueue.Count;

    /// <summary>(waveNumber, waveType) — UI banner, music, etc.</summary>
    public Action<int, WaveType> onWaveStart;
    public Action onWaveComplete;
    /// <summary>(restSeconds) — rest countdown started.</summary>
    public Action<float> onRestStart;

    private readonly HashSet<Enemy> alive = new HashSet<Enemy>();
    private readonly Queue<Enemy> spawnQueue = new Queue<Enemy>();
    private Transform player;
    private Coroutine loop;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable() => Enemy.OnAnyEnemyDied += CountKill;
    private void OnDisable() => Enemy.OnAnyEnemyDied -= CountKill;

    private void CountKill(Enemy enemy) => EnemiesKilled++;

    private void Start()
    {
        if (startOnPlay) StartWaves();
    }

    /// <summary>Starts the wave loop from wave 1.</summary>
    public void StartWaves()
    {
        if (loop != null) StopCoroutine(loop);
        CurrentWave = 0;
        WavesCompleted = 0;
        EnemiesKilled = 0;
        loop = StartCoroutine(WaveLoop());
    }

    public void StopWaves()
    {
        if (loop != null) StopCoroutine(loop);
        loop = null;
    }

    private IEnumerator WaveLoop()
    {
        yield return new WaitForSeconds(firstWaveDelay);

        while (true)
        {
            yield return RunWave();

            WavesCompleted++;
            onWaveComplete?.Invoke();

            yield return Rest();
        }
    }

    private IEnumerator Rest()
    {
        IsResting = true;
        RestTimeRemaining = restTime;
        onRestStart?.Invoke(restTime);

        while (RestTimeRemaining > 0)
        {
            RestTimeRemaining -= Time.deltaTime;
            yield return null;
        }

        RestTimeRemaining = 0;
        IsResting = false;
    }

    private IEnumerator RunWave()
    {
        CurrentWave++;
        CurrentWaveType = GetWaveType(CurrentWave);
        BuildSpawnQueue();

        onWaveStart?.Invoke(CurrentWave, CurrentWaveType);

        // Spawn over time, respecting the alive cap.
        while (spawnQueue.Count > 0)
        {
            CleanupAlive();

            if (alive.Count < maxAlive)
            {
                Spawn(spawnQueue.Dequeue());
                yield return new WaitForSeconds(spawnInterval);
            }
            else
            {
                yield return null;
            }
        }

        // Wait for every enemy to die.
        while (true)
        {
            CleanupAlive();
            if (alive.Count == 0) break;
            yield return null;
        }
    }

    private WaveType GetWaveType(int wave)
    {
        if (bossEveryXWaves > 0 && wave % bossEveryXWaves == 0 && bossPrefabs.Length > 0) return WaveType.Boss;
        if (eliteEveryXWaves > 0 && wave % eliteEveryXWaves == 0 && elitePrefabs.Length > 0) return WaveType.Elite;
        return WaveType.Normal;
    }

    private void BuildSpawnQueue()
    {
        spawnQueue.Clear();

        int count = baseEnemyCount + enemiesPerWave * (CurrentWave - 1);
        for (int i = 0; i < count; i++)
        {
            Enemy prefab = PickEnemy();
            if (prefab != null) spawnQueue.Enqueue(prefab);
        }

        // Specials spawn last, once the wave is already going.
        if (CurrentWaveType == WaveType.Elite)
        {
            for (int i = 0; i < elitesPerEliteWave; i++)
                spawnQueue.Enqueue(elitePrefabs[UnityEngine.Random.Range(0, elitePrefabs.Length)]);
        }
        else if (CurrentWaveType == WaveType.Boss)
        {
            spawnQueue.Enqueue(bossPrefabs[UnityEngine.Random.Range(0, bossPrefabs.Length)]);
        }
    }

    /// <summary>Weighted random among the enemies unlocked for this wave.</summary>
    private Enemy PickEnemy()
    {
        float total = 0f;
        foreach (EnemySpawnEntry e in enemies)
            if (e.prefab != null && CurrentWave >= e.minWave) total += e.weight;

        if (total <= 0f) return null;

        float roll = UnityEngine.Random.Range(0f, total);
        foreach (EnemySpawnEntry e in enemies)
        {
            if (e.prefab == null || CurrentWave < e.minWave) continue;
            roll -= e.weight;
            if (roll <= 0f) return e.prefab;
        }

        return null;
    }

    private void Spawn(Enemy prefab)
    {
        Vector3 pos = GetSpawnPosition();
        Enemy enemy = Instantiate(prefab, pos, Quaternion.identity);

        alive.Add(enemy);
        enemy.OnDied += HandleEnemyDied;
    }

    private void HandleEnemyDied(Enemy enemy)
    {
        enemy.OnDied -= HandleEnemyDied;
        alive.Remove(enemy);
    }

    /// <summary>Removes enemies destroyed without dying (fell off the map, despawned...).</summary>
    private void CleanupAlive()
    {
        alive.RemoveWhere(e => e == null);
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("Wave_Manager has no spawn points.", this);
            return transform.position;
        }

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // Prefer points far from the player; fall back to any point.
        List<Transform> valid = new List<Transform>();
        foreach (Transform sp in spawnPoints)
        {
            if (sp == null) continue;
            if (player == null || Vector3.Distance(sp.position, player.position) >= minDistanceFromPlayer)
                valid.Add(sp);
        }

        Transform chosen = valid.Count > 0
            ? valid[UnityEngine.Random.Range(0, valid.Count)]
            : spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];

        Vector2 offset = UnityEngine.Random.insideUnitCircle * spawnRadius;
        Vector3 pos = chosen.position + new Vector3(offset.x, 0, offset.y);

        // Snap onto the NavMesh so the enemy can path from frame one.
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, spawnRadius + 2f, NavMesh.AllAreas))
            pos = hit.position + Vector3.up * 1f;

        return pos;
    }

    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;

        Gizmos.color = Color.magenta;
        foreach (Transform sp in spawnPoints)
        {
            if (sp == null) continue;
            Gizmos.DrawWireSphere(sp.position, spawnRadius);
        }
    }
}
