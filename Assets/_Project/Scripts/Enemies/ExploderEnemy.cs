using UnityEngine;

public class ExploderEnemy : EnemyAI
{
    [Header("Exploder Settings")]
    [SerializeField] private float explosionRadius = 4f;
    [SerializeField] private int explosionDamage = 40;
    [SerializeField] private GameObject explosionEffectPrefab;

    public override void ConfigureForWave(int waveNumber)
    {
        base.ConfigureForWave(waveNumber);
        // Камикадзе быстрый, но очень хрупкий
        speed *= 1.8f;
        health = Mathf.Max(1, Mathf.RoundToInt(health * 0.4f));
        scoreValue = Mathf.RoundToInt(scoreValue * 1.5f);
        currentHealth = health;
    }

    protected override void HandleMovement()
    {
        if (player == null) return;

        Vector2 toPlayer = player.position - transform.position;
        float distanceToPlayer = toPlayer.magnitude;
        Vector2 direction = distanceToPlayer > 0.001f ? toPlayer.normalized : Vector2.zero;

        if (distanceToPlayer <= stoppingDistance)
        {
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            rb.linearVelocity = direction * speed;
        }

        RotateTowards(direction);
    }

    protected override void Die()
    {
        if (isDead) return;
        isDead = true;

        Explode();
    }

    private void Explode()
    {
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D hit in colliders)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth pHealth = hit.GetComponent<PlayerHealth>();
                if (pHealth != null)
                {
                    pHealth.TakeDamage(explosionDamage);
                    Vector2 dir = (hit.transform.position - transform.position).normalized;
                    pHealth.ApplyKnockback(dir * 10f);
                }
            }
        }

        GameManager.ReportEnemyKilled(scoreValue);
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.TryDropAmmo(transform.position);
        }
        
        if (CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.ShakeOnHit();
        }

        Destroy(gameObject);
    }

    protected override void OnCollisionStay2D(Collision2D collision)
    {
        if (!GameManager.CanGameplayRun() || isDead || !collision.gameObject.CompareTag("Player")) return;
        
        // Камикадзе взрывается при касании игрока
        Explode();
    }
}
