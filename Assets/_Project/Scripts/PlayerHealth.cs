using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    [SerializeField] private float damageInvulnerabilityTime = 0.5f;

    [Header("UI References")]
    public Slider healthSlider;

    [Header("Knockback Settings")]
    public float knockbackTotalTime = 0.2f;

    private int currentHealth;
    private float damageInvulnerabilityCounter;
    private float knockbackCounter;
    private bool isDead;
    private Rigidbody2D rb;

    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUi();
        GameManager.ReportPlayerHealth(currentHealth, maxHealth);
    }

    private void Update()
    {
        if (knockbackCounter > 0f)
        {
            knockbackCounter -= Time.deltaTime;
        }

        if (damageInvulnerabilityCounter > 0f)
        {
            damageInvulnerabilityCounter -= Time.deltaTime;
        }
    }

    public bool CanTakeDamage()
    {
        return !isDead && damageInvulnerabilityCounter <= 0f && GameManager.CanGameplayRun();
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || !CanTakeDamage())
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        damageInvulnerabilityCounter = damageInvulnerabilityTime;
        UpdateHealthUi();
        GameManager.ReportPlayerHealth(currentHealth, maxHealth);

        if (CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.ShakeOnHit();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void ApplyKnockback(Vector2 force)
    {
        if (isDead)
        {
            return;
        }

        knockbackCounter = knockbackTotalTime;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(force, ForceMode2D.Impulse);
    }

    public bool IsInKnockback()
    {
        return knockbackCounter > 0f;
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || isDead)
        {
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UpdateHealthUi();
        GameManager.ReportPlayerHealth(currentHealth, maxHealth);
    }

    public void IncreaseMaxHealth(int amount, bool healToFull = false)
    {
        if (amount <= 0)
        {
            return;
        }

        maxHealth += amount;
        currentHealth = healToFull ? maxHealth : Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHealthUi();
        GameManager.ReportPlayerHealth(currentHealth, maxHealth);
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        currentHealth = 0;
        rb.linearVelocity = Vector2.zero;
        UpdateHealthUi();
        GameManager.ReportPlayerHealth(currentHealth, maxHealth);
        GameManager.ReportPlayerDeath();
    }

    private void UpdateHealthUi()
    {
        if (healthSlider == null)
        {
            return;
        }

        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
    }
}
