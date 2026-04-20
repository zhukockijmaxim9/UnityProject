using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 2f;
    private Transform player;
    private Rigidbody2D rb;

    [Header("Health & Combat")]
    public int health = 2;
    public int damageToPlayer = 1;
    public float attackRate = 1f;
    private float nextAttackTime;

    [Header("Knockback Settings")]
    public float knockbackTotalTime = 0.2f; // Время, в течение которого зомби не может идти сам
    private float knockbackCounter;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Автоматически находим игрока по тегу
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void FixedUpdate()
    {
        if (player == null) return;

        // ПРОВЕРКА: Если нас оттолкнули, мы ждем и не управляем скоростью
        if (knockbackCounter > 0)
        {
            knockbackCounter -= Time.fixedDeltaTime;
        }
        else
        {
            // ОБЫЧНОЕ ДВИЖЕНИЕ
            Vector2 direction = (player.position - transform.position).normalized;
            rb.linearVelocity = direction * speed;

            // Поворот в сторону игрока
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            rb.rotation = angle;
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log("Зомби получил урон! Осталось: " + health);

        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }

    // МЕТОД ДЛЯ ОТТАЛКИВАНИЯ (вызывается из пули)
    public void ApplyKnockback(Vector2 force)
    {
        knockbackCounter = knockbackTotalTime; // Запускаем таймер "шока"
        rb.linearVelocity = Vector2.zero;            // Обнуляем текущую скорость ходьбы
        rb.AddForce(force, ForceMode2D.Impulse); // Даем физический пинок
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Если коснулись игрока
        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time >= nextAttackTime)
            {
                PlayerHealth pHealth = collision.gameObject.GetComponent<PlayerHealth>();
                if (pHealth != null)
                {
                    pHealth.TakeDamage(damageToPlayer);
                    
                    // Отталкиваем игрока при укусе
                    Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
                    pHealth.ApplyKnockback(knockbackDir * 7f); // Сила толчка игрока
                    
                    nextAttackTime = Time.time + attackRate;
                }
            }
        }
    }
}