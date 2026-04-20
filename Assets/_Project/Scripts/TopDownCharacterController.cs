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
    [SerializeField] private float fireRate = 4f;

    private Rigidbody2D rb;
    private Camera mainCamera;
    private Vector2 moveInput;
    private Vector2 mousePos;
    private PlayerHealth playerHealth;
    private float nextShotTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        playerHealth = GetComponent<PlayerHealth>();
        ResolveFirePoint();
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnAttack()
    {
        TryShoot();
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

        rb.linearVelocity = moveInput * moveSpeed;

        Vector2 lookDirection = mousePos - rb.position;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
            rb.rotation = angle;
        }
    }

    private bool CanAct()
    {
        return GameManager.CanGameplayRun() && playerHealth != null && !playerHealth.IsDead;
    }

    private void TryShoot()
    {
        if (!CanAct() || Time.time < nextShotTime)
        {
            return;
        }

        ResolveFirePoint();
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogWarning("Player shooting setup is incomplete. Bullet prefab or fire point is missing.", this);
            return;
        }

        Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        nextShotTime = Time.time + (1f / Mathf.Max(0.01f, fireRate));
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
}
