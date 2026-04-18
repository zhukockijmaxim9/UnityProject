using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 3f; // Чтобы пули не летели вечно, засоряя память

    void Start()
    {
        // Пуля летит "вперед" (вправо в 2D) со старта
        GetComponent<Rigidbody2D>().linearVelocity = transform.right * speed;
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Здесь будет урон врагу
        if (collision.CompareTag("Enemy")) 
        {
            // Destroy(collision.gameObject); // Пока просто удаляем врага
            Destroy(gameObject); // Удаляем пулю
        }
    }
}