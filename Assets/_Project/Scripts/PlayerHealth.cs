using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 5;
    private int currentHealth;

    [Header("UI References")]
    public Slider healthSlider;

    [Header("Knockback Settings")]
    public float knockbackTotalTime = 0.2f;
    private float knockbackCounter;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
        }
    }

    void Update()
    {
        // Таймер отталкивания
        if (knockbackCounter > 0)
        {
            knockbackCounter -= Time.deltaTime;
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void ApplyKnockback(Vector2 force)
    {
        knockbackCounter = knockbackTotalTime;
        rb.linearVelocity = Vector2.zero; // Сбрасываем старую скорость
        rb.AddForce(force, ForceMode2D.Impulse); // Даем пинок
    }

    // Это свойство будет проверять наш скрипт движения
    public bool IsInKnockback()
    {
        return knockbackCounter > 0;
    }

    void Die()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}