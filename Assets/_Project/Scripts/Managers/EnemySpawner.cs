using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
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
        GameManager.ReportWave(currentWave);
        GameManager.ReportWaveState(true);

        yield return new WaitForSeconds(currentWave == 1 ? 1f : 2f);

        float spawnInterval = Mathf.Max(0.2f, baseSpawnRate - (currentWave * 0.15f));

        yield return SpawnGroup(enemyPrefab, 5 + (currentWave * 2), spawnInterval);

        if (currentWave >= 2)
        {
            yield return SpawnGroup(dasherPrefab, currentWave / 2, spawnInterval);
        }

        if (currentWave >= 3)
        {
            yield return SpawnGroup(spitterPrefab, currentWave / 3, spawnInterval);
        }

        if (currentWave >= 5)
        {
            yield return SpawnGroup(exploderPrefab, currentWave / 4, spawnInterval);
        }

        if (currentWave % 4 == 0)
        {
            yield return SpawnGroup(bossPrefab, currentWave / 4, 1.2f);
        }

        isSpawning = false;
        waitingForNextWave = false;
    }

    private IEnumerator SpawnGroup(GameObject prefab, int amount, float interval)
    {
        if (prefab == null || amount <= 0)
        {
            yield break;
        }

        for (int i = 0; i < amount; i++)
        {
            SpawnEnemy(prefab);
            yield return new WaitForSeconds(Mathf.Max(0.05f, interval));
        }
    }

    private void SpawnEnemy(GameObject prefab)
    {
        if (player == null || prefab == null) return;

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
            enemy.ConfigureForWave(currentWave);
        }
    }

    private void ResolvePlayer()
    {
        if (player != null) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }
}
