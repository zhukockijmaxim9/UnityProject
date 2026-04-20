using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyArchetype
    {
        Walker,
        Dasher,
        Bruiser,
        Boss
    }

    [Header("Archetype")]
    [SerializeField] private EnemyArchetype startingArchetype = EnemyArchetype.Walker;
    [SerializeField] private float stoppingDistance = 0.6f;

    [Header("Movement Settings")]
    public float speed = 2f;

    [Header("Health & Combat")]
    public int health = 2;
    public int scoreValue = 100;
    public int damageToPlayer = 1;
    public float attackRate = 1f;
    [SerializeField] private float playerKnockbackForce = 7f;

    [Header("Knockback Settings")]
    public float knockbackTotalTime = 0.2f;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 7.5f;
    [SerializeField] private float dashDuration = 0.35f;
    [SerializeField] private float dashCooldown = 2.75f;
    [SerializeField] private float dashTriggerDistance = 6f;

    [Header("Boss Settings")]
    [SerializeField] private GameObject summonPrefab;
    [SerializeField] private int summonsPerCycle = 2;
    [SerializeField] private float summonCooldown = 7f;
    [SerializeField] private float summonRadius = 1.5f;

    private Transform player;
    private Rigidbody2D rb;
    private float nextAttackTime;
    private float knockbackCounter;
    private float nextDashTime;
    private float dashTimer;
    private float nextSummonTime;
    private Vector2 dashDirection;
    private bool isDead;
    private int currentHealth;
    private float baseSpeed;
    private int baseHealth;
    private int baseScoreValue;
    private int baseDamageToPlayer;
    private float baseAttackRate;
    private EnemyArchetype currentArchetype;
    private bool runtimeConfigured;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseSpeed = speed;
        baseHealth = health;
        baseScoreValue = scoreValue;
        baseDamageToPlayer = damageToPlayer;
        baseAttackRate = attackRate;
    }

    private void OnEnable()
    {
        GameManager.ReportEnemySpawned();
        isDead = false;
        knockbackCounter = 0f;
        dashTimer = 0f;
    }

    private void Start()
    {
        ResolvePlayer();

        if (!runtimeConfigured)
        {
            ConfigureForWave(startingArchetype, Mathf.Max(1, GameManager.GetCurrentWaveNumber()));
        }
    }

    private void FixedUpdate()
    {
        if (player == null)
        {
            ResolvePlayer();
        }

        if (player == null || isDead || !GameManager.CanGameplayRun())
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (knockbackCounter > 0f)
        {
            knockbackCounter -= Time.fixedDeltaTime;
            return;
        }

        if (dashTimer > 0f)
        {
            dashTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = dashDirection * dashSpeed;
            RotateTowards(dashDirection);
            return;
        }

        Vector2 toPlayer = player.position - transform.position;
        float distanceToPlayer = toPlayer.magnitude;
        Vector2 direction = distanceToPlayer > 0.001f ? toPlayer / distanceToPlayer : Vector2.zero;

        TryUseSpecialAbility(direction, distanceToPlayer);
        if (dashTimer > 0f)
        {
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

    public void ConfigureForWave(EnemyArchetype archetype, int waveNumber, GameObject summonSource = null)
    {
        currentArchetype = archetype;
        runtimeConfigured = true;
        isDead = false;

        float waveScale = Mathf.Max(0, waveNumber - 1);

        speed = baseSpeed * (1f + (waveScale * 0.04f));
        health = Mathf.Max(1, Mathf.RoundToInt(baseHealth * (1f + (waveScale * 0.22f))));
        damageToPlayer = Mathf.Max(1, Mathf.RoundToInt(baseDamageToPlayer + (waveScale * 0.15f)));
        attackRate = Mathf.Max(0.35f, baseAttackRate * (1f - (waveScale * 0.02f)));
        scoreValue = Mathf.Max(10, Mathf.RoundToInt(baseScoreValue * (1f + (waveScale * 0.3f))));

        switch (currentArchetype)
        {
            case EnemyArchetype.Dasher:
                speed *= 1.5f;
                health = Mathf.Max(1, Mathf.RoundToInt(health * 0.8f));
                attackRate = Mathf.Max(0.3f, attackRate * 0.8f);
                scoreValue = Mathf.RoundToInt(scoreValue * 1.25f);
                break;

            case EnemyArchetype.Bruiser:
                speed *= 0.7f;
                health = Mathf.Max(1, Mathf.RoundToInt(health * 2.2f));
                damageToPlayer += 1;
                attackRate *= 1.2f;
                scoreValue = Mathf.RoundToInt(scoreValue * 1.75f);
                break;

            case EnemyArchetype.Boss:
                speed *= 1.15f;
                health = Mathf.Max(1, Mathf.RoundToInt(health * 3.5f));
                damageToPlayer += Mathf.Max(1, waveNumber / 3);
                attackRate = Mathf.Max(0.3f, attackRate * 0.85f);
                scoreValue = Mathf.RoundToInt(scoreValue * 4f);

                if (summonPrefab == null)
                {
                    summonPrefab = summonSource;
                }
                break;
        }

        currentHealth = health;
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0)
        {
            return;
        }

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            isDead = true;
            GameManager.ReportEnemyKilled(scoreValue);
            ObjectPoolManager.ReturnToPool(gameObject);
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
        if (!GameManager.CanGameplayRun() || isDead || !collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
        if (playerHealth == null || !playerHealth.CanTakeDamage())
        {
            return;
        }

        playerHealth.TakeDamage(damageToPlayer);
        Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
        playerHealth.ApplyKnockback(knockbackDir * playerKnockbackForce);
        nextAttackTime = Time.time + attackRate;
    }

    private void ResolvePlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void RotateTowards(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rb.rotation = angle;
    }

    private void TryUseSpecialAbility(Vector2 direction, float distanceToPlayer)
    {
        bool canDash = currentArchetype == EnemyArchetype.Dasher || currentArchetype == EnemyArchetype.Boss;
        if (canDash && distanceToPlayer <= dashTriggerDistance && Time.time >= nextDashTime)
        {
            dashDirection = direction;
            dashTimer = dashDuration;
            nextDashTime = Time.time + dashCooldown;
            return;
        }

        bool canSummon = currentArchetype == EnemyArchetype.Boss && summonPrefab != null && summonsPerCycle > 0;
        if (canSummon && Time.time >= nextSummonTime)
        {
            nextSummonTime = Time.time + summonCooldown;
            SummonMinions();
        }
    }

    private void SummonMinions()
    {
        int currentWaveNumber = Mathf.Max(1, GameManager.GetCurrentWaveNumber());

        for (int i = 0; i < summonsPerCycle; i++)
        {
            Vector2 offset = Random.insideUnitCircle * summonRadius;
            GameObject spawnedMinion = ObjectPoolManager.Spawn(summonPrefab, transform.position + (Vector3)offset, Quaternion.identity);
            EnemyAI minion = spawnedMinion.GetComponent<EnemyAI>();
            if (minion != null)
            {
                minion.ConfigureForWave(EnemyArchetype.Walker, currentWaveNumber);
            }
        }
    }
}
