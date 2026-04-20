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
        Destroy(gameObject, lifetime);
    }

    public void Initialize(float newSpeed, int newDamage, float newKnockbackForce)
    {
        currentSpeed = newSpeed;
        currentDamage = newDamage;
        currentKnockbackForce = newKnockbackForce;

        if (rb != null)
        {
            rb.linearVelocity = transform.right * currentSpeed;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
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
