using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class EnemySpawner : MonoBehaviour
{
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

    [Header("Wave Info")]
    public int currentWave = 0;
    private bool isSpawning = false;

    void Awake()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        currentWave = 0;
        isSpawning = false;
        GameManager.ResetStats();
    }

    void Update()
    {
        if (!isSpawning && (currentWave == 0 || GameObject.FindGameObjectsWithTag("Enemy").Length == 0))
        {
            isSpawning = true;
            StartCoroutine(StartNextWave());
        }
    }

    IEnumerator StartNextWave()
    {
        currentWave++;
        GameManager.ReportWave(currentWave);

        int enemiesToSpawn = currentWave * 5;
        int bossesToSpawn = currentWave;

        float currentSpawnRate = Mathf.Max(0.2f, baseSpawnRate - (currentWave * 0.3f));

        Debug.Log($"<color=cyan>=== WAVE {currentWave} STARTED ===</color>");
        Debug.Log($"Plan: {enemiesToSpawn} zombies and {bossesToSpawn} boss(es). Interval: {currentSpawnRate}s");

        yield return new WaitForSeconds(2f);

        for (int i = 1; i <= enemiesToSpawn; i++)
        {
            SpawnEnemy(enemyPrefab, $"Zombie #{i}");
            yield return new WaitForSeconds(currentSpawnRate);
        }

        if (bossPrefab != null)
        {
            for (int j = 1; j <= bossesToSpawn; j++)
            {
                Debug.Log($"<color=red>BOSS #{j} ENTERS THE ARENA!</color>");
                SpawnEnemy(bossPrefab, $"Boss #{j}");
                yield return new WaitForSeconds(1f);
            }
        }

        isSpawning = false;
        Debug.Log($"<color=orange>All enemies from wave {currentWave} are now on the arena.</color>");
    }

    void SpawnEnemy(GameObject prefab, string logName)
    {
        if (player == null) return;

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float randomDist = Random.Range(minSpawnDistance, maxSpawnDistance);
        Vector3 spawnPos = new Vector3(
            player.position.x + randomDir.x * randomDist,
            player.position.y + randomDir.y * randomDist,
            0f
        );

        Instantiate(prefab, spawnPos, Quaternion.identity);
        Debug.Log($"[Spawner]: {logName} spawned.");
    }
}
