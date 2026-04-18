using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 2f; // Скорость зомби (чуть медленнее игрока)

    private Transform player;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Ищем игрока на сцене по тегу
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void FixedUpdate()
    {
        if (player != null)
        {
            // Находим направление к игроку
            Vector2 direction = (player.position - transform.position).normalized;
            
            // Двигаем зомби через физику
            rb.linearVelocity = direction * speed;

            // Поворачиваем зомби лицом к игроку
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            rb.rotation = angle;
        }
    }

    // Проверка попадания пули
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Если в нас попал объект с тегом Bullet
        if (other.CompareTag("Bullet"))
        {
            Die();
            Destroy(other.gameObject); // Уничтожаем пулю
        }
    }

    void Die()
    {
        // Здесь можно добавить эффекты крови или звук
        Destroy(gameObject);
    }
}