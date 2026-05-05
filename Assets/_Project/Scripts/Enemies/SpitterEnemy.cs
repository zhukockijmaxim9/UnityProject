using UnityEngine;

public class SpitterEnemy : EnemyAI
{
    [Header("Spitter Settings")]
    [SerializeField] private GameObject spitPrefab;
    [SerializeField] private float spitRange = 7f;
    [SerializeField] private float spitRate = 2f;
    [SerializeField] private float spitProjectileSpeed = 10f;
    [SerializeField] private int spitDamage = 15;

    private float nextSpitTime;

    public override void ConfigureForWave(int waveNumber)
    {
        base.ConfigureForWave(waveNumber);
        // Плевун медленнее, но имеет большую дистанцию остановки
        speed *= 0.9f;
        health = Mathf.Max(1, Mathf.RoundToInt(health * 0.7f));
        stoppingDistance = spitRange;
        scoreValue = Mathf.RoundToInt(scoreValue * 1.3f);
        currentHealth = health;
    }

    protected override void HandleMovement()
    {
        if (player == null) return;

        Vector2 toPlayer = player.position - transform.position;
        float distanceToPlayer = toPlayer.magnitude;
        Vector2 direction = distanceToPlayer > 0.001f ? toPlayer.normalized : Vector2.zero;

        // Если игрок в радиусе стрельбы, атакуем
        if (distanceToPlayer <= spitRange + 1f && Time.time >= nextSpitTime)
        {
            nextSpitTime = Time.time + (1f / spitRate);
            Spit(direction);
        }

        // Движение к игроку до дистанции стрельбы
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

    private void Spit(Vector2 direction)
    {
        if (spitPrefab == null) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        GameObject projectile = ObjectPoolManager.Spawn(spitPrefab, transform.position, Quaternion.Euler(0, 0, angle));
        
        Bullet bulletScript = projectile.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(spitProjectileSpeed, spitDamage, 2f, true);
        }
    }
}
