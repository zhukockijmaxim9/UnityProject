using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 2f;
    private Transform player;
    private Rigidbody2D rb;

    [Header("Health & Combat")]
    public int health = 2;
    public int scoreValue = 100;
    public int damageToPlayer = 1;
    public float attackRate = 1f;
    private float nextAttackTime;
    private bool isDead;

    [Header("Knockback Settings")]
    public float knockbackTotalTime = 0.2f;
    private float knockbackCounter;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void FixedUpdate()
    {
        if (player == null) return;

        if (knockbackCounter > 0)
        {
            knockbackCounter -= Time.fixedDeltaTime;
        }
        else
        {
            Vector2 direction = (player.position - transform.position).normalized;
            rb.linearVelocity = direction * speed;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            rb.rotation = angle;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
        {
            return;
        }

        health -= damage;
        Debug.Log("Zombie took damage. Remaining health: " + health);

        if (health <= 0)
        {
            isDead = true;
            GameManager.ReportEnemyKilled(scoreValue);
            Destroy(gameObject);
        }
    }

    public void ApplyKnockback(Vector2 force)
    {
        knockbackCounter = knockbackTotalTime;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(force, ForceMode2D.Impulse);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time >= nextAttackTime)
            {
                PlayerHealth pHealth = collision.gameObject.GetComponent<PlayerHealth>();
                if (pHealth != null)
                {
                    pHealth.TakeDamage(damageToPlayer);

                    Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
                    pHealth.ApplyKnockback(knockbackDir * 7f);

                    nextAttackTime = Time.time + attackRate;
                }
            }
        }
    }
}
