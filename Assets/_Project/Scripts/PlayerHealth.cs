using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100;
    private int currentHealth;
    private bool isDead;
    public bool IsDead => isDead;
    public bool IsInKnockback => knockbackCounter > 0;

    [Header("Health Regen")]
    [SerializeField] private float healthRegenPerSecond = 2f;
    private bool healthRegenUnlocked;
    private float healthRegenProgress;

    [Header("UI References")]
    public Slider healthSlider;
    [SerializeField] private GameObject damagePopupPrefab;

    [Header("Knockback Settings")]
    public float knockbackTotalTime = 0.2f;
    private float knockbackCounter;
    private Rigidbody2D rb;

    [Header("Invulnerability")]
    public float damageInvulnerabilityTime = 0.5f;
    private float damageInvulnerabilityCounter;

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
        if (damageInvulnerabilityCounter > 0)
        {
            damageInvulnerabilityCounter -= Time.deltaTime;
        }

        if (knockbackCounter > 0)
        {
            knockbackCounter -= Time.deltaTime;
        }

        RegenerateHealth();
    }

    public bool CanTakeDamage()
    {
        return !isDead && damageInvulnerabilityCounter <= 0;
    }

    public void TakeDamage(int damage)
    {
        if (!CanTakeDamage()) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        damageInvulnerabilityCounter = damageInvulnerabilityTime;
        UpdateHealthUi();
        GameManager.ReportPlayerHealth(currentHealth, maxHealth);
        SpawnDamagePopup(damage);

        if (CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.ShakeOnHit();
        }

        if (currentHealth <= 0)
        {
            HandleDeath();
        }
    }

    public void ApplyKnockback(Vector2 direction)
    {
        if (isDead) return;
        knockbackCounter = knockbackTotalTime;
        rb.linearVelocity = direction;
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthUi();
        GameManager.ReportPlayerHealth(currentHealth, maxHealth);
    }

    public void UnlockHealthRegen()
    {
        healthRegenUnlocked = true;
    }

    public void MultiplyHealthRegen(float multiplier)
    {
        healthRegenPerSecond = Mathf.Max(0.1f, healthRegenPerSecond * multiplier);
    }

    public void IncreaseMaxHealth(int amount, bool heal)
    {
        maxHealth += amount;
        if (heal)
        {
            currentHealth += amount;
        }
        UpdateHealthUi();
        GameManager.ReportPlayerHealth(currentHealth, maxHealth);
    }

    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;
        currentHealth = 0;
        rb.linearVelocity = Vector2.zero;
        UpdateHealthUi();
        GameManager.ReportPlayerHealth(currentHealth, maxHealth);
        GameManager.ReportPlayerDeath();
    }

    private void UpdateHealthUi()
    {
        if (healthSlider == null) return;
        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
    }

    private void RegenerateHealth()
    {
        if (!healthRegenUnlocked || isDead || currentHealth >= maxHealth)
        {
            return;
        }

        healthRegenProgress += healthRegenPerSecond * Time.deltaTime;
        if (healthRegenProgress < 1f)
        {
            return;
        }

        int healAmount = Mathf.FloorToInt(healthRegenProgress);
        healthRegenProgress -= healAmount;
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        UpdateHealthUi();
        GameManager.ReportPlayerHealth(currentHealth, maxHealth);
    }

    private void SpawnDamagePopup(int amount)
    {
        if (damagePopupPrefab == null) return;

        GameObject popupObj = Instantiate(damagePopupPrefab, transform.position + Vector3.up, Quaternion.identity);
        DamagePopup popup = popupObj.GetComponent<DamagePopup>();
        if (popup != null)
        {
            popup.Setup(amount, Color.red);
        }
    }
}
