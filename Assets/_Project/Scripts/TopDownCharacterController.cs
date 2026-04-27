using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownCharacterController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 0.45f;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float dashStaminaCost = 30f;
    [SerializeField] private float staminaRegenRate = 24f;
    [SerializeField] private float staminaRegenDelay = 0.6f;
    [SerializeField] private Slider staminaSlider;

    [Header("Default Weapon Bootstrap")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 4f;

    private Rigidbody2D rb;
    private Camera mainCamera;
    private Vector2 moveInput;
    private Vector2 mousePos;
    private Vector2 dashDirection;
    private PlayerHealth playerHealth;
    private WeaponController weaponController;
    private float currentStamina;
    private float dashTimer;
    private float dashCooldownTimer;
    private float staminaRegenDelayTimer;

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public bool IsDashing => dashTimer > 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        playerHealth = GetComponent<PlayerHealth>();
        weaponController = GetComponent<WeaponController>();
        if (weaponController == null)
        {
            weaponController = gameObject.AddComponent<WeaponController>();
        }

        currentStamina = maxStamina;

        ResolveFirePoint();
        weaponController.Bootstrap(bulletPrefab, firePoint, fireRate);
        UpdateStaminaUi();
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnReload(InputValue value)
    {
        if (value.isPressed && CanAct())
        {
            weaponController.TryReload();
        }
    }

    public void OnPrevious(InputValue value)
    {
        if (value.isPressed && CanAct())
        {
            weaponController.PreviousWeapon();
        }
    }

    public void OnNext(InputValue value)
    {
        if (value.isPressed && CanAct())
        {
            weaponController.NextWeapon();
        }
    }

    public void OnDash(InputValue value)
    {
        if (value.isPressed)
        {
            TryDash();
        }
    }

    public void OnSprint(InputValue value)
    {
        OnDash(value);
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null && Mouse.current != null)
        {
            mousePos = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        }

        UpdateDashAndStaminaTimers();
        HandleContinuousAttack();
    }

    private void HandleContinuousAttack()
    {
        // Проверяем, зажата ли левая кнопка мыши напрямую через Mouse.current
        if (Mouse.current != null && Mouse.current.leftButton.isPressed && CanAct())
        {
            weaponController.TryFire();
        }
    }

    private void FixedUpdate()
    {
        if (!CanAct())
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (playerHealth != null && playerHealth.IsInKnockback())
        {
            return;
        }

        if (IsDashing)
        {
            rb.linearVelocity = dashDirection * dashSpeed;
            RotateTowards(dashDirection);
            return;
        }

        rb.linearVelocity = moveInput * moveSpeed;

        Vector2 lookDirection = mousePos - rb.position;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            RotateTowards(lookDirection.normalized);
        }
    }

    private bool CanAct()
    {
        return GameManager.CanGameplayRun() && playerHealth != null && !playerHealth.IsDead;
    }

    private void TryDash()
    {
        if (!CanAct() || IsDashing || dashCooldownTimer > 0f || currentStamina < dashStaminaCost)
        {
            return;
        }

        Vector2 desiredDashDirection = moveInput.sqrMagnitude > 0.01f ? moveInput.normalized : transform.right;
        if (desiredDashDirection.sqrMagnitude <= 0.001f)
        {
            desiredDashDirection = Vector2.right;
        }

        dashDirection = desiredDashDirection;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        currentStamina = Mathf.Max(0f, currentStamina - dashStaminaCost);
        staminaRegenDelayTimer = staminaRegenDelay;
        UpdateStaminaUi();
    }

    private void UpdateDashAndStaminaTimers()
    {
        if (dashTimer > 0f)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                dashTimer = 0f;
                rb.linearVelocity = Vector2.zero;
            }
        }

        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (staminaRegenDelayTimer > 0f)
        {
            staminaRegenDelayTimer -= Time.deltaTime;
            return;
        }

        if (currentStamina < maxStamina)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + (staminaRegenRate * Time.deltaTime));
            UpdateStaminaUi();
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

    private void ResolveFirePoint()
    {
        if (firePoint != null)
        {
            return;
        }

        Transform directMatch = transform.Find("FirePoint");
        if (directMatch != null)
        {
            firePoint = directMatch;
            return;
        }

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "FirePoint")
            {
                firePoint = child;
                return;
            }
        }
    }

    private void UpdateStaminaUi()
    {
        if (staminaSlider == null)
        {
            return;
        }

        staminaSlider.maxValue = maxStamina;
        staminaSlider.value = currentStamina;
    }
}
