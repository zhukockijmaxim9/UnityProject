using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownCharacterController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

    private Rigidbody2D rb;
    private Camera mainCamera;
    private Vector2 moveInput;
    private Vector2 mousePos;
    
    // Ссылка на скрипт здоровья
    private PlayerHealth playerHealth;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        // Получаем ссылку на компонент здоровья
        playerHealth = GetComponent<PlayerHealth>();
    }

    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();

    public void OnAttack()
    {
        Shoot();
    }

    private void Update()
    {
        mousePos = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
    }

    private void FixedUpdate()
    {
        // ПРОВЕРКА: Если игрока оттолкнули, мы не трогаем rb.linearVelocity вручную
        if (playerHealth != null && playerHealth.IsInKnockback())
        {
            return; // Выходим и даем физике отработать толчок
        }

        // Обычное движение (теперь оно не будет мешать knockback)
        rb.linearVelocity = moveInput * moveSpeed;
        
        Vector2 lookDir = mousePos - rb.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        rb.rotation = angle;
    }

    private void Shoot()
    {
        if (bulletPrefab != null && firePoint != null)
        {
            Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        }
    }
}