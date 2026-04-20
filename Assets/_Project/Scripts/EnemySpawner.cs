using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnInstruction
    {
        public EnemyAI.EnemyArchetype archetype = EnemyAI.EnemyArchetype.Walker;
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
    public GameObject bossPrefab;

    [Header("Spawn Settings")]
    public Transform player;
    public float minSpawnDistance = 14f;
    [FormerlySerializedAs("spawnDistance")]
    public float maxSpawnDistance = 18f;
    [FormerlySerializedAs("spawnRate")]
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
        if (!GameManager.CanGameplayRun())
        {
            return;
        }

        if (player == null)
        {
            ResolvePlayer();
        }

        if (player == null || isSpawning)
        {
            return;
        }

        bool arenaIsClear = GameObject.FindGameObjectsWithTag("Enemy").Length == 0;
        if (arenaIsClear)
        {
            if (!waitingForNextWave)
            {
                waitingForNextWave = true;
                nextWaveStartTime = Time.time + timeBetweenWaves;
                GameManager.ReportWaveState(false);
                Debug.Log($"Wave {currentWave} cleared. Preparing next wave...");
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

        Debug.Log($"<color=cyan>=== {wave.label} STARTED ===</color>");
        yield return new WaitForSeconds(wave.startDelay);

        for (int i = 0; i < wave.spawns.Count; i++)
        {
            SpawnInstruction instruction = wave.spawns[i];
            for (int j = 0; j < instruction.amount; j++)
            {
                SpawnEnemy(instruction, j + 1);
                yield return new WaitForSeconds(Mathf.Max(0.05f, instruction.interval));
            }
        }

        isSpawning = false;
        waitingForNextWave = false;
        Debug.Log($"<color=orange>Wave {currentWave} deployed. Survive the assault.</color>");
    }

    private void SpawnEnemy(SpawnInstruction instruction, int spawnIndex)
    {
        GameObject prefab = ResolvePrefab(instruction);
        if (player == null || prefab == null)
        {
            return;
        }

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float randomDist = Random.Range(minSpawnDistance, maxSpawnDistance);
        Vector3 spawnPos = new Vector3(
            player.position.x + (randomDir.x * randomDist),
            player.position.y + (randomDir.y * randomDist),
            0f
        );

        GameObject spawnedEnemy = Instantiate(prefab, spawnPos, Quaternion.identity);
        EnemyAI enemy = spawnedEnemy.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            enemy.ConfigureForWave(instruction.archetype, currentWave, enemyPrefab);
        }

        Debug.Log($"[Spawner]: {instruction.archetype} #{spawnIndex} spawned.");
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
                wave.label = $"Wave {waveNumber}: Swarm";
                wave.spawns.Add(new SpawnInstruction
                {
                    archetype = EnemyAI.EnemyArchetype.Walker,
                    amount = 5 + (tier * 3),
                    interval = walkerInterval
                });
                if (tier > 1)
                {
                    wave.spawns.Add(new SpawnInstruction
                    {
                        archetype = EnemyAI.EnemyArchetype.Dasher,
                        amount = tier - 1,
                        interval = specialInterval
                    });
                }
                break;

            case 1:
                wave.label = $"Wave {waveNumber}: Hunter Pack";
                wave.spawns.Add(new SpawnInstruction
                {
                    archetype = EnemyAI.EnemyArchetype.Walker,
                    amount = 4 + (tier * 2),
                    interval = walkerInterval
                });
                wave.spawns.Add(new SpawnInstruction
                {
                    archetype = EnemyAI.EnemyArchetype.Dasher,
                    amount = 2 + tier,
                    interval = specialInterval
                });
                break;

            case 2:
                wave.label = $"Wave {waveNumber}: Boss Pressure";
                wave.spawns.Add(new SpawnInstruction
                {
                    archetype = EnemyAI.EnemyArchetype.Walker,
                    amount = 3 + (tier * 2),
                    interval = walkerInterval
                });
                wave.spawns.Add(new SpawnInstruction
                {
                    archetype = EnemyAI.EnemyArchetype.Dasher,
                    amount = Mathf.Max(1, tier),
                    interval = specialInterval
                });
                if (bossPrefab != null)
                {
                    wave.spawns.Add(new SpawnInstruction
                    {
                        archetype = EnemyAI.EnemyArchetype.Boss,
                        amount = 1,
                        interval = 0.75f,
                        prefabOverride = bossPrefab
                    });
                }
                break;

            default:
                wave.label = $"Wave {waveNumber}: Bruiser Push";
                wave.spawns.Add(new SpawnInstruction
                {
                    archetype = EnemyAI.EnemyArchetype.Walker,
                    amount = 4 + (tier * 2),
                    interval = walkerInterval
                });
                wave.spawns.Add(new SpawnInstruction
                {
                    archetype = EnemyAI.EnemyArchetype.Bruiser,
                    amount = 1 + tier,
                    interval = specialInterval
                });
                if (tier >= 2)
                {
                    wave.spawns.Add(new SpawnInstruction
                    {
                        archetype = EnemyAI.EnemyArchetype.Dasher,
                        amount = 1 + (tier / 2),
                        interval = specialInterval
                    });
                }
                break;
        }

        return wave;
    }

    private GameObject ResolvePrefab(SpawnInstruction instruction)
    {
        if (instruction.prefabOverride != null)
        {
            return instruction.prefabOverride;
        }

        if (instruction.archetype == EnemyAI.EnemyArchetype.Boss && bossPrefab != null)
        {
            return bossPrefab;
        }

        return enemyPrefab;
    }

    private void ResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }
}
