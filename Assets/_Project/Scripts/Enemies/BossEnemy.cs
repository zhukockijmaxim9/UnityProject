using UnityEngine;

public class BossEnemy : EnemyAI
{
    [Header("Boss Summoning Settings")]
    [SerializeField] private GameObject summonPrefab;
    [SerializeField] private int summonsPerCycle = 2;
    [SerializeField] private float summonCooldown = 15f;
    [SerializeField] private float summonRadius = 1.5f;

    [Header("Boss Dash Settings")]
    [SerializeField] private float bossDashSpeed = 12f;
    [SerializeField] private float bossDashDuration = 0.6f;
    [SerializeField] private float bossDashCooldown = 5f;
    [SerializeField] private float bossDashTriggerDistance = 8f;

    private float nextSummonTime;
    private float nextDashTime;
    private float dashTimer;
    private Vector2 dashDirection;

    public override void ConfigureForWave(int waveNumber)
    {
        base.ConfigureForWave(waveNumber);
        // Босс ходит медленно, но он очень прочный
        speed *= 0.6f; 
    }

    // Полный иммунитет к отталкиванию
    public override void ApplyKnockback(Vector2 force)
    {
        // Ничего не делаем
    }

    protected override void HandleMovement()
    {
        if (player == null) return;

        if (dashTimer > 0f)
        {
            dashTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = dashDirection * bossDashSpeed;
            RotateTowards(dashDirection);
            return;
        }

        HandleSummoning();

        Vector2 toPlayer = player.position - transform.position;
        float distanceToPlayer = toPlayer.magnitude;
        Vector2 direction = distanceToPlayer > 0.001f ? toPlayer.normalized : Vector2.zero;

        // Логика рывка босса
        if (distanceToPlayer <= bossDashTriggerDistance && Time.time >= nextDashTime)
        {
            dashDirection = direction;
            dashTimer = bossDashDuration;
            nextDashTime = Time.time + bossDashCooldown;
            return;
        }

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

    private void HandleSummoning()
    {
        if (isDead || Time.time < nextSummonTime || summonPrefab == null) return;

        nextSummonTime = Time.time + summonCooldown;

        for (int i = 0; i < summonsPerCycle; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * summonRadius;
            Vector2 spawnPos = (Vector2)transform.position + randomOffset;
            
            GameObject summoned = Instantiate(summonPrefab, spawnPos, Quaternion.identity);
            EnemyAI summonedAI = summoned.GetComponent<EnemyAI>();
            if (summonedAI != null)
            {
                summonedAI.ConfigureForWave(GameManager.Instance != null ? GameManager.Instance.currentLevel : 1);
            }
        }
        
        Debug.Log("BOSS SUMMONED MINIONS!");
    }
}
