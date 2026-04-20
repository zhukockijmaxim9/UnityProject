using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject enemyPrefab;
    public GameObject bossPrefab;

    [Header("Spawn Settings")]
    public Transform player;
    public float minSpawnDistance = 14f; // Чуть увеличил, чтобы точно за камерой
    public float maxSpawnDistance = 18f;
    public float baseSpawnRate = 2.0f;

    [Header("Wave Info")]
    public int currentWave = 0;
    private bool isSpawning = false;

    void Awake()
    {
        // Runtime-state must always start from a clean baseline,
        // even if Unity preserves component values between play sessions.
        currentWave = 0;
        isSpawning = false;
    }

    void Update()
    {
        // Если врагов на сцене нет и спавн завершен — запускаем следующую волну
        // Добавляем проверку currentWave == 0, чтобы первый запуск был четким
        if (!isSpawning && (currentWave == 0 || GameObject.FindGameObjectsWithTag("Enemy").Length == 0))
        {
            isSpawning = true;
            StartCoroutine(StartNextWave());
        }
    }

    IEnumerator StartNextWave()
    {
        // isSpawning = true;
        currentWave++;
        
        // Количество обычных зомби
        int enemiesToSpawn = currentWave * 5;
        // Количество боссов (растет каждую волну)
        int bossesToSpawn = currentWave; 

        float currentSpawnRate = Mathf.Max(0.2f, baseSpawnRate - (currentWave * 0.15f));

        Debug.Log($"<color=cyan>=== НАЧАЛО ВОЛНЫ {currentWave} ===</color>");
        Debug.Log($"План: {enemiesToSpawn} зомби и {bossesToSpawn} босс(ов). Интервал: {currentSpawnRate}с");

        yield return new WaitForSeconds(2f);

        // 1. Спавним обычных зомби
        for (int i = 1; i <= enemiesToSpawn; i++)
        {
            SpawnEnemy(enemyPrefab, $"Зомби №{i}");
            yield return new WaitForSeconds(currentSpawnRate);
        }

        // 2. Спавним боссов (в конце волны)
        if (bossPrefab != null)
        {
            for (int j = 1; j <= bossesToSpawn; j++)
            {
                Debug.Log($"<color=red>ВЫХОДИТ БОСС №{j}!</color>");
                SpawnEnemy(bossPrefab, $"БОСС №{j}");
                yield return new WaitForSeconds(1f); // Небольшая пауза между боссами
            }
        }

        isSpawning = false;
        Debug.Log($"<color=orange>Все враги волны {currentWave} выпущены на арену!</color>");
    }

    void SpawnEnemy(GameObject prefab, string logName)
    {
        if (player == null) return;

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float randomDist = Random.Range(minSpawnDistance, maxSpawnDistance);
        Vector3 spawnPos = new Vector3(player.position.x + randomDir.x * randomDist, 
                                       player.position.y + randomDir.y * randomDist, 0);

        Instantiate(prefab, spawnPos, Quaternion.identity);
        Debug.Log($"[Спавнер]: {logName} появился в мире.");
    }
}
