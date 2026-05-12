using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    public float speed = 15f;
    public int damage = 25;

    [SerializeField] private float lifetime = 3f;
    [SerializeField] private float knockbackForce = 3f;

    private Rigidbody2D rb;
    private float currentSpeed;
    private int currentDamage;
    private float currentKnockbackForce;
    private float lifeTimer;
    private bool isEnemyBullet;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentSpeed = speed;
        currentDamage = damage;
        currentKnockbackForce = knockbackForce;
    }

    private void OnEnable()
    {
        lifeTimer = lifetime;
    }

    private void Start()
    {
        rb.linearVelocity = transform.right * currentSpeed;
    }

    private void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
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
        if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
            return;
        }

        if (isEnemyBullet)
        {
            HitPlayer(other);
        }
        else
        {
            HitEnemy(other);
        }
    }

    private void HitPlayer(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        PlayerHealth player = other.GetComponent<PlayerHealth>();
        if (player != null)
        {
            player.TakeDamage(currentDamage);
            player.ApplyKnockback((Vector2)transform.right * currentKnockbackForce);
        }

        Destroy(gameObject);
    }

    private void HitEnemy(Collider2D other)
    {
        if (!other.CompareTag("Enemy"))
        {
            return;
        }

        EnemyAI enemy = other.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            enemy.TakeDamage(currentDamage);
            enemy.ApplyKnockback((Vector2)transform.right * currentKnockbackForce);
        }

        Destroy(gameObject);
    }
}
