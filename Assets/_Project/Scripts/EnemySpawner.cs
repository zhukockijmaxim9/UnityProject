using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class EnemySpawner : MonoBehaviour
{
    public enum EnemyType
    {
        Walker,
        Dasher,
        Spitter,
        Exploder,
        Boss
    }

    [System.Serializable]
    public class SpawnInstruction
    {
        public EnemyType type = EnemyType.Walker;
        public int amount = 5;
        public float interval = 0.6f;
        public GameObject prefabOverride;
    }

    [System.Serializable]
    public class WaveDefinition
    {
        public string label = "Wave";
        public float startDelay = 1.5f;
        public List<SpawnInstruction> spawns = new List<SpawnInstruction>();
    }

    [Header("Prefabs")]
    public GameObject enemyPrefab;
    public GameObject dasherPrefab;
    public GameObject bossPrefab;
    public GameObject exploderPrefab;
    public GameObject spitterPrefab;

    [Header("Spawn Settings")]
    public Transform player;
    public float minSpawnDistance = 14f;
    public float maxSpawnDistance = 18f;
    public float baseSpawnRate = 2.0f;
    [SerializeField] private float timeBetweenWaves = 2.5f;
    [SerializeField] private bool useGeneratedWaves = true;
    [SerializeField] private List<WaveDefinition> customWaves = new List<WaveDefinition>();

    [Header("Wave Info")]
    public int currentWave = 0;

    private bool isSpawning;
    private bool waitingForNextWave;
    private float nextWaveStartTime;

    private void Awake()
    {
        ResolvePlayer();

        currentWave = 0;
        isSpawning = false;
        waitingForNextWave = true;
        nextWaveStartTime = Time.time + 1f;

        GameManager.ResetStats();
        GameManager.ReportWaveState(false);
    }

    private void Update()
    {
        if (!GameManager.CanGameplayRun()) return;

        if (player == null || isSpawning) return;

        bool arenaIsClear = GameManager.GetAliveEnemies() <= 0;
        if (arenaIsClear)
        {
            if (!waitingForNextWave)
            {
                waitingForNextWave = true;
                nextWaveStartTime = Time.time + timeBetweenWaves;
                GameManager.ReportWaveState(false);
            }

            if (Time.time >= nextWaveStartTime)
            {
                isSpawning = true;
                StartCoroutine(StartNextWave());
            }
        }
        else
        {
            waitingForNextWave = false;
            GameManager.ReportWaveState(true);
        }
    }

    private IEnumerator StartNextWave()
    {
        currentWave++;
        WaveDefinition wave = GetWaveDefinition(currentWave);

        GameManager.ReportWave(currentWave);
        GameManager.ReportWaveState(true);

        yield return new WaitForSeconds(wave.startDelay);

        for (int i = 0; i < wave.spawns.Count; i++)
        {
            SpawnInstruction instruction = wave.spawns[i];
            for (int j = 0; j < instruction.amount; j++)
            {
                SpawnEnemy(instruction);
                yield return new WaitForSeconds(Mathf.Max(0.05f, instruction.interval));
            }
        }

        isSpawning = false;
        waitingForNextWave = false;
    }

    private void SpawnEnemy(SpawnInstruction instruction)
    {
        GameObject prefab = ResolvePrefab(instruction);
        if (player == null || prefab == null) return;

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float randomDist = Random.Range(minSpawnDistance, maxSpawnDistance);
        Vector3 spawnPos = new Vector3(
            player.position.x + (randomDir.x * randomDist),
            player.position.y + (randomDir.y * randomDist),
            0f
        );

        GameObject spawnedEnemy = ObjectPoolManager.Spawn(prefab, spawnPos, Quaternion.identity);
        EnemyAI enemy = spawnedEnemy.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            enemy.ConfigureForWave(currentWave);
        }
    }

    private WaveDefinition GetWaveDefinition(int waveNumber)
    {
        if (!useGeneratedWaves && customWaves.Count > 0)
        {
            return customWaves[Mathf.Clamp(waveNumber - 1, 0, customWaves.Count - 1)];
        }

        return BuildGeneratedWave(waveNumber);
    }

    private WaveDefinition BuildGeneratedWave(int waveNumber)
    {
        int tier = 1 + ((waveNumber - 1) / 4);
        float walkerInterval = Mathf.Max(0.2f, baseSpawnRate - (waveNumber * 0.15f));
        float specialInterval = Mathf.Max(0.25f, walkerInterval + 0.1f);

        WaveDefinition wave = new WaveDefinition
        {
            label = $"Wave {waveNumber}",
            startDelay = waveNumber == 1 ? 1f : 2f
        };

        switch ((waveNumber - 1) % 4)
        {
            case 0:
                wave.spawns.Add(new SpawnInstruction { type = EnemyType.Walker, amount = 5 + (tier * 3), interval = walkerInterval });
                if (tier > 1) wave.spawns.Add(new SpawnInstruction { type = EnemyType.Dasher, amount = tier - 1, interval = specialInterval });
                break;

            case 1:
                wave.spawns.Add(new SpawnInstruction { type = EnemyType.Walker, amount = 4 + (tier * 2), interval = walkerInterval });
                wave.spawns.Add(new SpawnInstruction { type = EnemyType.Dasher, amount = 1 + tier, interval = specialInterval });
                break;

            case 2:
                wave.spawns.Add(new SpawnInstruction { type = EnemyType.Walker, amount = 3 + (tier * 2), interval = walkerInterval });
                if (bossPrefab != null) wave.spawns.Add(new SpawnInstruction { type = EnemyType.Boss, amount = tier, interval = 1.2f, prefabOverride = bossPrefab });
                break;

            default:
                wave.spawns.Add(new SpawnInstruction { type = EnemyType.Walker, amount = 6 + tier, interval = walkerInterval });
                wave.spawns.Add(new SpawnInstruction { type = EnemyType.Exploder, amount = tier, interval = specialInterval });
                break;
        }

        // Add some variety based on wave number
        if (waveNumber >= 3)
        {
            wave.spawns.Add(new SpawnInstruction { type = EnemyType.Spitter, amount = tier, interval = specialInterval });
        }

        return wave;
    }

    private GameObject ResolvePrefab(SpawnInstruction instruction)
    {
        if (instruction.prefabOverride != null) return instruction.prefabOverride;

        switch (instruction.type)
        {
            case EnemyType.Dasher: return dasherPrefab != null ? dasherPrefab : enemyPrefab;
            case EnemyType.Boss: return bossPrefab != null ? bossPrefab : enemyPrefab;
            case EnemyType.Exploder: return exploderPrefab != null ? exploderPrefab : enemyPrefab;
            case EnemyType.Spitter: return spitterPrefab != null ? spitterPrefab : enemyPrefab;
            default: return enemyPrefab;
        }
    }

    private void ResolvePlayer()
    {
        if (player != null) return;
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
    }
}
