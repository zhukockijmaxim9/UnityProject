using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownCharacterController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Camera mainCamera;
    private Vector2 moveInput;
    private Vector2 mousePos;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main; // Находим главную камеру для расчета координат мыши
    }

    // Движение (вызывается из Player Input)
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    // Позиция мыши (вызывается из Player Input, если добавишь Action "Look")
    // Или просто берем напрямую из системы ввода в Update
    private void Update()
    {
        // Получаем позицию курсора на экране и переводим в мировые координаты
        mousePos = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
    }

    private void FixedUpdate()
    {
        Move();
        RotateTowardsMouse();
    }

    private void Move()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }

    private void RotateTowardsMouse()
    {
        // Вычисляем направление от игрока к мышке
        Vector2 lookDir = mousePos - rb.position;
        
        // Вычисляем угол в градусах (Atan2 возвращает радианы)
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        
        // Поворачиваем Rigidbody (так физика работает стабильнее, чем через transform.rotation)
        rb.rotation = angle;
    }
}