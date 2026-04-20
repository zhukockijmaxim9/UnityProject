using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 15f;
    public int damage = 1; // Урон одной пули

    void Start()
    {
        GetComponent<Rigidbody2D>().linearVelocity = transform.right * speed;
        Destroy(gameObject, 3f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            EnemyAI enemy = other.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                
                // Вычисляем направление полета пули и толкаем зомби
                Vector2 knockback = transform.right * 3f; // 3f - сила толчка пули
                enemy.ApplyKnockback(knockback);
            }
            Destroy(gameObject);
        }
    }
}