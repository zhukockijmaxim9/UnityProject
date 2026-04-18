using UnityEngine;
using UnityEngine.InputSystem; // КРИТИЧНО для Unity 6!

[RequireComponent(typeof(Rigidbody2D))] // Гарантирует, что Rigidbody2D есть на объекте
public class TopDownCharacterController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f; // Скорость движения

    private Rigidbody2D rb;
    private Vector2 moveInput; // Сюда сохраняем ввод от игрока

    private void Awake()
    {
        // Получаем ссылку на Rigidbody при старте
        rb = GetComponent<Rigidbody2D>();
    }

    // Этот метод автоматически вызывается New Input System
    // при нажатии клавиш движения (WASD/Стрелки).
    public void OnMove(InputValue value)
    {
        // Получаем Vector2 (X и Y) от нажатых клавиш
        moveInput = value.Get<Vector2>();
        
        // Debug.Log($"Input: {moveInput}"); // Раскомментируй, чтобы видеть ввод в консоли
    }

    // Физика обновляется здесь (двигаем Rigidbody)
    private void FixedUpdate()
    {
        // Перемещаем физическое тело игрока
        Move();
    }

    private void Move()
    {
        // rb.velocity = вектор скорости. Мы берем направление ввода и умножаем на скорость.
        // Это самый простой и надежный способ движения для Top-Down.
        rb.linearVelocity = moveInput * moveSpeed;
    }
}