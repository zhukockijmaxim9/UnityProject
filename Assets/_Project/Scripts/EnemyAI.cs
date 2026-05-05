using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class EnemyAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 2f;
    [SerializeField] protected float stoppingDistance = 0.6f;

    [Header("Health & Combat")]
    public int health = 100;
    public int scoreValue = 100;
    public int damageToPlayer = 1;
    public float attackRate = 1f;
    [SerializeField] protected float playerKnockbackForce = 7f;

    [Header("Knockback Settings")]
    public float knockbackTotalTime = 0.2f;
    [SerializeField] protected float knockbackRepeatDelay = 0.1f;
    [SerializeField] protected float knockbackMass = 1f;

    [Header("Visual Effects")]
    [SerializeField] protected GameObject damagePopupPrefab;

    protected Transform player;
    protected Rigidbody2D rb;
    protected float nextAttackTime;
    protected float knockbackCounter;
    protected float nextKnockbackTime;
    protected bool isDead;
    public int currentHealth;

    protected float baseSpeed;
    protected int baseHealth;
    protected int baseScoreValue;
    protected int baseDamageToPlayer;
    protected float baseAttackRate;
    protected float currentKnockbackMass;

    protected SpriteRenderer spriteRenderer;
    protected Color originalColor;
    protected Coroutine flashCoroutine;
    protected bool runtimeConfigured;

    protected virtual void Awake()
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
        currentKnockbackMass = Mathf.Max(0.05f, knockbackMass);
    }

    protected virtual void OnEnable()
    {
        GameManager.ReportEnemySpawned();
        isDead = false;
        knockbackCounter = 0f;
        nextKnockbackTime = 0f;
        
        Collider2D coll = GetComponent<Collider2D>();
        if (coll != null) coll.enabled = true;
    }

    protected virtual void Start()
    {
        ResolvePlayer();
        if (!runtimeConfigured)
        {
            ConfigureForWave(Mathf.Max(1, GameManager.GetCurrentWaveNumber()));
        }
    }

    protected virtual void FixedUpdate()
    {
        if (player == null) ResolvePlayer();

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

        HandleMovement();
    }

    // Абстрактный метод, каждый тип врага будет двигаться по-своему
    protected abstract void HandleMovement();

    public virtual void ConfigureForWave(int waveNumber)
    {
        runtimeConfigured = true;
        isDead = false;
        float waveScale = Mathf.Max(0, waveNumber - 1);

        speed = baseSpeed * (1f + (waveScale * 0.04f));
        health = Mathf.Max(1, Mathf.RoundToInt(baseHealth * (1f + (waveScale * 0.22f))));
        damageToPlayer = Mathf.Max(1, Mathf.RoundToInt(baseDamageToPlayer + (waveScale * 0.15f)));
        attackRate = Mathf.Max(0.35f, baseAttackRate * (1f - (waveScale * 0.02f)));
        scoreValue = Mathf.Max(10, Mathf.RoundToInt(baseScoreValue * (1f + (waveScale * 0.3f))));
        currentHealth = health;
    }

    public virtual void TakeDamage(int damage)
    {
        if (isDead || damage <= 0) return;

        currentHealth -= damage;
        StartFlash();
        SpawnDamagePopup(damage);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected void SpawnDamagePopup(int amount)
    {
        if (damagePopupPrefab == null) return;
        GameObject popupObj = Instantiate(damagePopupPrefab, transform.position + Vector3.up, Quaternion.identity);
        DamagePopup popup = popupObj.GetComponent<DamagePopup>();
        if (popup != null) popup.Setup(amount, Color.yellow);
    }

    protected void StartFlash()
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    protected IEnumerator FlashRoutine()
    {
        if (spriteRenderer == null) yield break;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        Collider2D coll = GetComponent<Collider2D>();
        if (coll != null) coll.enabled = false;

        GameManager.ReportEnemyKilled(scoreValue);
        StartCoroutine(DeathAnimationRoutine());
    }

    protected IEnumerator DeathAnimationRoutine()
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

    public void ApplyKnockback(Vector2 force)
    {
        if (isDead || force.sqrMagnitude <= 0.0001f || Time.time < nextKnockbackTime) return;

        Vector2 appliedImpulse = force / Mathf.Max(0.05f, currentKnockbackMass);
        if (appliedImpulse.sqrMagnitude <= 0.0001f) return;

        nextKnockbackTime = Time.time + knockbackRepeatDelay;
        knockbackCounter = knockbackTotalTime;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(appliedImpulse, ForceMode2D.Impulse);
    }

    protected virtual void OnCollisionStay2D(Collision2D collision)
    {
        if (!GameManager.CanGameplayRun() || isDead || !collision.gameObject.CompareTag("Player")) return;
        if (Time.time < nextAttackTime) return;

        PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
        if (playerHealth == null || !playerHealth.CanTakeDamage()) return;

        playerHealth.TakeDamage(damageToPlayer);
        Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
        playerHealth.ApplyKnockback(knockbackDir * playerKnockbackForce);
        nextAttackTime = Time.time + attackRate;
    }

    protected void ResolvePlayer()
    {
        if (player != null) return;
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
    }

    protected void RotateTowards(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f) return;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rb.rotation = angle;
    }
}
