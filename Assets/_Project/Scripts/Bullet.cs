using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    public float speed = 15f;
    public int damage = 1;

    [SerializeField] private float lifetime = 3f;
    [SerializeField] private float knockbackForce = 3f;

    private Rigidbody2D rb;
    private float currentSpeed;
    private int currentDamage;
    private float currentKnockbackForce;
    private float returnTimer;
    private bool isEnemyBullet;

    private void OnEnable()
    {
        returnTimer = lifetime;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentSpeed = speed;
        currentDamage = damage;
        currentKnockbackForce = knockbackForce;
    }


    private void Start()
    {
        rb.linearVelocity = transform.right * currentSpeed;
    }

    private void Update()
    {
        if (returnTimer > 0f)
        {
            returnTimer -= Time.deltaTime;
            if (returnTimer <= 0f)
            {
                ObjectPoolManager.ReturnToPool(gameObject);
            }
        }
    }

    public void Initialize(float newSpeed, int newDamage, float newKnockbackForce, bool fromEnemy = false)
    {
        currentSpeed = newSpeed;
        currentDamage = newDamage;
        currentKnockbackForce = newKnockbackForce;
        isEnemyBullet = fromEnemy;

        if (rb != null)
        {
            rb.linearVelocity = transform.right * currentSpeed;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Проверяем попадание в стену (для всех типов пуль)
        if (other.CompareTag("Wall"))
        {
            ObjectPoolManager.ReturnToPool(gameObject);
            return; // Выходим из метода, чтобы не проверять врагов/игрока
        }

        if (isEnemyBullet)
        {
            if (other.CompareTag("Player"))
            {
                PlayerHealth player = other.GetComponent<PlayerHealth>();
                if (player != null)
                {
                    player.TakeDamage(currentDamage);
                    player.ApplyKnockback((Vector2)transform.right * currentKnockbackForce);
                }
                ObjectPoolManager.ReturnToPool(gameObject);
            }
        }
        else
        {
            if (other.CompareTag("Enemy"))
            {
                EnemyAI enemy = other.GetComponent<EnemyAI>();
                if (enemy != null)
                {
                    enemy.TakeDamage(currentDamage);
                    enemy.ApplyKnockback((Vector2)transform.right * currentKnockbackForce);
                }
                ObjectPoolManager.ReturnToPool(gameObject);
            }
        }
    }
}
