using UnityEngine;

public class DasherEnemy : EnemyAI
{
    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 7.5f;
    [SerializeField] private float dashDuration = 0.35f;
    [SerializeField] private float dashCooldown = 2.75f;
    [SerializeField] private float dashTriggerDistance = 6f;

    private float nextDashTime;
    private float dashTimer;
    private Vector2 dashDirection;

    public override void ConfigureForWave(int waveNumber)
    {
        base.ConfigureForWave(waveNumber);
        // Дешер быстрее, но у него меньше здоровья
        speed *= 1.5f;
        health = Mathf.Max(1, Mathf.RoundToInt(health * 0.8f));
        attackRate = Mathf.Max(0.3f, attackRate * 0.8f);
        scoreValue = Mathf.RoundToInt(scoreValue * 1.25f);
        currentHealth = health;
        currentKnockbackMass = Mathf.Max(0.05f, knockbackMass * 0.85f);
    }

    protected override void HandleMovement()
    {
        if (player == null) return;

        if (dashTimer > 0f)
        {
            dashTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = dashDirection * dashSpeed;
            RotateTowards(dashDirection);
            return;
        }

        Vector2 toPlayer = player.position - transform.position;
        float distanceToPlayer = toPlayer.magnitude;
        Vector2 direction = distanceToPlayer > 0.001f ? toPlayer.normalized : Vector2.zero;

        // Логика рывка
        if (distanceToPlayer <= dashTriggerDistance && Time.time >= nextDashTime)
        {
            dashDirection = direction;
            dashTimer = dashDuration;
            nextDashTime = Time.time + dashCooldown;
            return;
        }

        // Обычное движение
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
}
