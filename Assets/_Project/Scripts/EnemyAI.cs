using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyArchetype
    {
        Walker,
        Dasher,
        Bruiser,
        Boss,
        Exploder,
        Spitter
    }

    [Header("Archetype")]
    [SerializeField] public EnemyArchetype startingArchetype = EnemyArchetype.Walker;
    [SerializeField] private float stoppingDistance = 0.6f;

    [Header("Movement Settings")]
    public float speed = 2f;
    [SerializeField] private float pathUpdateDelay = 0.2f;

    [Header("Health & Combat")]
    public int health = 100;
    public int scoreValue = 100;
    public int damageToPlayer = 1;
    public float attackRate = 1f;
    [SerializeField] private float playerKnockbackForce = 7f;

    [Header("Knockback Settings")]
    public float knockbackTotalTime = 0.2f;
    [SerializeField] private float knockbackRepeatDelay = 0.1f;
    [SerializeField] private float knockbackMass = 1f;
    [SerializeField] private float dasherKnockbackMassMultiplier = 0.85f;
    [SerializeField] private float bruiserKnockbackMassMultiplier = 1.75f;
    [SerializeField] private float bossKnockbackMassMultiplier = 3f;

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

    [Header("Exploder Settings")]
    [SerializeField] private float explosionRadius = 3.5f;
    [SerializeField] private int explosionDamage = 40;

    [Header("Visual Effects")]
    [SerializeField] private GameObject damagePopupPrefab;
    [SerializeField] private GameObject explosionEffectPrefab;

    [Header("Spitter Settings")]
    [SerializeField] private GameObject spitPrefab;
    [SerializeField] private float spitRange = 7f;
    [SerializeField] private float spitRate = 2f;
    [SerializeField] private float spitProjectileSpeed = 10f;
    [SerializeField] private int spitDamage = 15;

    private Transform player;
    private Rigidbody2D rb;
    private float nextAttackTime;
    private float knockbackCounter;
    private float nextKnockbackTime;
    private float nextDashTime;
    private float dashTimer;
    private float nextSummonTime;
    private float nextSpitTime;
    private Vector2 dashDirection;
    private bool isDead;
    public int currentHealth;
    private float baseSpeed;
    private int baseHealth;
    private int baseScoreValue;
    private int baseDamageToPlayer;
    private float baseAttackRate;
    private float baseKnockbackMass;
    private float currentKnockbackMass;
    private EnemyArchetype currentArchetype;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;
    private bool runtimeConfigured;

    private List<Vector2> currentPath;
    private float pathUpdateTimer;
    private int currentPathIndex;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        
        baseSpeed = speed;
        baseHealth = health;
        baseScoreValue = scoreValue;
        baseDamageToPlayer = damageToPlayer;
        baseAttackRate = attackRate;
        baseKnockbackMass = Mathf.Max(0.05f, knockbackMass);
        currentKnockbackMass = baseKnockbackMass;
    }

    private void OnEnable()
    {
        GameManager.ReportEnemySpawned();
        isDead = false;
        knockbackCounter = 0f;
        nextKnockbackTime = 0f;
        dashTimer = 0f;
        
        // Сбрасываем колайдер при спавне из пула
        Collider2D coll = GetComponent<Collider2D>();
        if (coll != null) coll.enabled = true;
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
        Vector2 lookingDirection = distanceToPlayer > 0.001f ? toPlayer / distanceToPlayer : Vector2.zero;

        // --- PATHFINDING (A*) ---
        pathUpdateTimer -= Time.fixedDeltaTime;
        if (pathUpdateTimer <= 0f)
        {
            pathUpdateTimer = pathUpdateDelay + Random.Range(-0.05f, 0.05f);

            if (Pathfinding2D.Instance != null)
            {
                currentPath = Pathfinding2D.Instance.FindPath(transform.position, player.position);
                currentPathIndex = 1; // 0 — это наша текущая позиция
            }
            else
            {
                currentPath = null;
            }
        }

        Vector2 movementDirection = Vector2.zero;

        if (currentPath != null && currentPath.Count > currentPathIndex)
        {
            Vector2 toWaypoint = currentPath[currentPathIndex] - (Vector2)transform.position;

            if (toWaypoint.sqrMagnitude < 0.15f)
            {
                currentPathIndex++;
                if (currentPath.Count > currentPathIndex)
                {
                    toWaypoint = currentPath[currentPathIndex] - (Vector2)transform.position;
                    movementDirection = toWaypoint.normalized;
                }
            }
            else
            {
                movementDirection = toWaypoint.normalized;
            }
        }
        else
        {
            // Фолбэк: если путь не найден — идём напрямую
            movementDirection = lookingDirection;
        }

        TryUseSpecialAbility(lookingDirection, distanceToPlayer);
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
            rb.linearVelocity = movementDirection * speed;
        }

        RotateTowards(lookingDirection);
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
        currentKnockbackMass = GetKnockbackMassForArchetype(currentArchetype);

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
                currentKnockbackMass = baseKnockbackMass * bossKnockbackMassMultiplier;

                if (summonPrefab == null)
                {
                    summonPrefab = summonSource;
                }
                break;
            case EnemyArchetype.Exploder:
                speed *= 1.8f;
                health = Mathf.Max(1, Mathf.RoundToInt(health * 0.4f));
                scoreValue = Mathf.RoundToInt(scoreValue * 1.5f);
                break;
            case EnemyArchetype.Spitter:
                speed *= 0.9f;
                health = Mathf.Max(1, Mathf.RoundToInt(health * 0.7f));
                stoppingDistance = spitRange;
                scoreValue = Mathf.RoundToInt(scoreValue * 1.3f);
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
        StartFlash();
        SpawnDamagePopup(damage);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void SpawnDamagePopup(int amount)
    {
        if (damagePopupPrefab == null) return;

        GameObject popupObj = Instantiate(damagePopupPrefab, transform.position + Vector3.up, Quaternion.identity);
        DamagePopup popup = popupObj.GetComponent<DamagePopup>();
        if (popup != null)
        {
            popup.Setup(amount, Color.yellow);
        }
    }

    private void StartFlash()
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        if (spriteRenderer == null) yield break;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Отключаем всё лишнее мгновенно
        Collider2D coll = GetComponent<Collider2D>();
        if (coll != null) coll.enabled = false;
        
        // Удалена ссылка на BossHealthBar

        if (currentArchetype == EnemyArchetype.Exploder)
        {
            Explode();
        }
        else
        {
            GameManager.ReportEnemyKilled(scoreValue);
            StartCoroutine(DeathAnimationRoutine());
        }
    }

    private IEnumerator DeathAnimationRoutine()
    {
        float duration = 0.4f;
        float timer = 0f;
        Vector3 startScale = transform.localScale;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                spriteRenderer.color = c;
            }
            
            yield return null;
        }

        ObjectPoolManager.ReturnToPool(gameObject);
        
        transform.localScale = startScale;
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
    }

    private void Explode()
    {
        if (explosionEffectPrefab != null)
        {
            ObjectPoolManager.Spawn(explosionEffectPrefab, transform.position, Quaternion.identity);
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
        
        if (CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.ShakeOnHit();
        }

        ObjectPoolManager.ReturnToPool(gameObject);
    }

    public void ApplyKnockback(Vector2 force)
    {
        if (isDead || force.sqrMagnitude <= 0.0001f || Time.time < nextKnockbackTime)
        {
            return;
        }

        Vector2 appliedImpulse = force / Mathf.Max(0.05f, currentKnockbackMass);
        if (appliedImpulse.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        nextKnockbackTime = Time.time + knockbackRepeatDelay;
        knockbackCounter = knockbackTotalTime;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(appliedImpulse, ForceMode2D.Impulse);
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

        if (currentArchetype == EnemyArchetype.Exploder)
        {
            Explode();
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
        HandleDash(direction, distanceToPlayer);
        HandleSummoning();
        HandleSpitting(direction, distanceToPlayer);
    }

    private void HandleDash(Vector2 direction, float distanceToPlayer)
    {
        if (currentArchetype == EnemyArchetype.Boss) return;
        bool canDash = currentArchetype == EnemyArchetype.Dasher;
        if (canDash && distanceToPlayer <= dashTriggerDistance && Time.time >= nextDashTime)
        {
            dashDirection = direction;
            dashTimer = dashDuration;
            nextDashTime = Time.time + dashCooldown;
        }
    }

    private void HandleSummoning()
    {
        if (isDead || currentArchetype != EnemyArchetype.Boss || Time.time < nextSummonTime || summonPrefab == null) return;

        nextSummonTime = Time.time + summonCooldown;

        for (int i = 0; i < summonsPerCycle; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * summonRadius;
            Vector2 spawnPos = (Vector2)transform.position + randomOffset;
            
            GameObject summoned = Instantiate(summonPrefab, spawnPos, Quaternion.identity);
            EnemyAI summonedAI = summoned.GetComponent<EnemyAI>();
            if (summonedAI != null)
            {
                summonedAI.ConfigureForWave(EnemyArchetype.Exploder, GameManager.Instance != null ? GameManager.Instance.currentLevel : 1);
            }
        }
        
        Debug.Log("BOSS SUMMONED KAMIKAZES!");
    }

    private void HandleSpitting(Vector2 direction, float distanceToPlayer)
    {
        bool canSpit = currentArchetype == EnemyArchetype.Spitter && spitPrefab != null;
        if (canSpit && distanceToPlayer <= spitRange + 1f && Time.time >= nextSpitTime)
        {
            nextSpitTime = Time.time + (1f / spitRate);
            Spit(direction);
        }
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

    private float GetKnockbackMassForArchetype(EnemyArchetype archetype)
    {
        float multiplier = 1f;

        switch (archetype)
        {
            case EnemyArchetype.Dasher:
                multiplier = dasherKnockbackMassMultiplier;
                break;

            case EnemyArchetype.Bruiser:
                multiplier = bruiserKnockbackMassMultiplier;
                break;

            case EnemyArchetype.Boss:
                multiplier = bossKnockbackMassMultiplier;
                break;
        }

        return Mathf.Max(0.05f, baseKnockbackMass * multiplier);
    }
}
