using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab; // Сюда перетащим префаб зомби
    public float spawnRate = 2f;   // Раз во сколько секунд спавнить
    public float spawnDistance = 15f; // Дистанция от игрока (чтобы не спавнились прямо перед носом)

    private Transform player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        // Запускаем бесконечный цикл вызова функции Spawn
        InvokeRepeating("SpawnEnemy", spawnRate, spawnRate);
    }

    void SpawnEnemy()
    {
        if (player == null) return;

        // Генерируем случайную точку на круге вокруг игрока
        Vector2 spawnPos = (Vector2)player.position + Random.insideUnitCircle.normalized * spawnDistance;

        // Создаем зомби
        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }
}