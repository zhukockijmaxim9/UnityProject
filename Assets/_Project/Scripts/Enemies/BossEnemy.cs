using UnityEngine;

public class BossEnemy : EnemyAI
{
    [Header("Boss Settings")]
    [SerializeField] private GameObject summonPrefab;
    [SerializeField] private int summonsPerCycle = 2;
    [SerializeField] private float summonCooldown = 7f;
    [SerializeField] private float summonRadius = 1.5f;
    [SerializeField] private float bossKnockbackMassMultiplier = 3f;

    private float nextSummonTime;

    public override void ConfigureForWave(int waveNumber)
    {
        base.ConfigureForWave(waveNumber);
        // Босс имеет много здоровья и иммунитет к сильному отталкиванию
        currentKnockbackMass = knockbackMass * bossKnockbackMassMultiplier;
    }

    protected override void HandleMovement()
    {
        if (player == null) return;

        HandleSummoning();

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
                // Призываем камикадзе
                summonedAI.ConfigureForWave(GameManager.Instance != null ? GameManager.Instance.currentLevel : 1);
            }
        }
        
        Debug.Log("BOSS SUMMONED MINIONS!");
    }
}
