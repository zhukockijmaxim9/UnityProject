using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform firePoint;

    [Header("Loadout")]
    [SerializeField] private WeaponDefinition[] availableWeapons;

    private WeaponDefinition currentDefinition;
    private int currentWeaponIndex = -1;
    private bool[] unlockedWeapons;
    private float nextShotTime;
    private float reloadTimer;
    private int currentDamage;
    private float currentFireRate;
    private float currentProjectileSpeed;
    private float currentProjectileKnockback;
    private int currentMagazineSize;
    private float currentReloadTime;
    private int currentProjectilesPerShot;
    private float currentSpreadAngle;
    private int currentAmmo;
    private bool isReloading;
    private int extraDamage;

    public string CurrentWeaponName => currentDefinition != null ? currentDefinition.weaponName : "";
    public int CurrentAmmo => currentAmmo;
    public int CurrentMagazineSize => currentMagazineSize;
    public bool IsReloading => isReloading;

    private void Awake()
    {
        if (availableWeapons != null)
        {
            unlockedWeapons = new bool[availableWeapons.Length];
            if (unlockedWeapons.Length > 0)
            {
                unlockedWeapons[0] = true;
            }
        }

        EquipWeapon(0);
    }

    private void Update()
    {
        HandleKeyboardInput();
        HandleShootingInput();

        if (!isReloading)
        {
            return;
        }

        reloadTimer -= Time.deltaTime;
        if (reloadTimer <= 0f)
        {
            FinishReload();
        }
    }

    private void HandleKeyboardInput()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame) EquipWeapon(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame) EquipWeapon(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame) EquipWeapon(2);
        else if (Keyboard.current.digit4Key.wasPressedThisFrame) EquipWeapon(3);

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            TryReload();
        }
    }

    private void HandleShootingInput()
    {
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            TryFire();
        }
    }

    public bool TryFire()
    {
        if (!GameManager.CanGameplayRun() || isReloading || Time.time < nextShotTime)
        {
            return false;
        }

        if (!EnsureReadyToShoot())
        {
            return false;
        }

        if (currentAmmo <= 0)
        {
            BeginReload();
            return false;
        }

        FireProjectiles();
        currentAmmo--;
        nextShotTime = Time.time + (1f / Mathf.Max(0.01f, currentFireRate));
        ReportAmmo();

        if (CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.ShakeOnShot();
        }

        if (currentAmmo <= 0)
        {
            BeginReload();
        }

        return true;
    }

    public void TryReload()
    {
        if (isReloading || currentAmmo >= currentMagazineSize || currentMagazineSize <= 0)
        {
            return;
        }

        BeginReload();
    }

    public void NextWeapon()
    {
        if (availableWeapons == null || availableWeapons.Length == 0)
        {
            return;
        }

        for (int i = 1; i <= availableWeapons.Length; i++)
        {
            int nextIndex = (currentWeaponIndex + i) % availableWeapons.Length;
            if (IsWeaponUnlocked(nextIndex))
            {
                EquipWeapon(nextIndex);
                return;
            }
        }
    }

    public void PreviousWeapon()
    {
        if (availableWeapons == null || availableWeapons.Length == 0)
        {
            return;
        }

        for (int i = 1; i <= availableWeapons.Length; i++)
        {
            int previousIndex = currentWeaponIndex - i;
            if (previousIndex < 0)
            {
                previousIndex += availableWeapons.Length;
            }

            if (IsWeaponUnlocked(previousIndex))
            {
                EquipWeapon(previousIndex);
                return;
            }
        }
    }

    public void ModifyDamage(int delta)
    {
        extraDamage += delta;
        currentDamage += delta;
        Debug.Log("Damage upgraded. Bonus: +" + extraDamage + ", total damage: " + currentDamage);
    }

    public void MultiplyFireRate(float multiplier)
    {
        currentFireRate = Mathf.Max(0.1f, currentFireRate * multiplier);
    }

    public void ModifyMagazineSize(int delta)
    {
        currentMagazineSize = Mathf.Max(1, currentMagazineSize + delta);
        currentAmmo = Mathf.Min(currentAmmo, currentMagazineSize);
        ReportAmmo();
    }

    public void MultiplyReloadTime(float multiplier)
    {
        currentReloadTime = Mathf.Max(0.05f, currentReloadTime * multiplier);
    }

    public void ModifyProjectileCount(int delta)
    {
        currentProjectilesPerShot = Mathf.Max(1, currentProjectilesPerShot + delta);
    }

    public void UnlockNextWeapon()
    {
        if (availableWeapons == null || unlockedWeapons == null)
        {
            return;
        }

        for (int i = 0; i < unlockedWeapons.Length; i++)
        {
            if (!unlockedWeapons[i] && availableWeapons[i] != null)
            {
                unlockedWeapons[i] = true;
                EquipWeapon(i);
                Debug.Log("Unlocked and equipped: " + availableWeapons[i].weaponName);
                return;
            }
        }
    }

    public bool IsWeaponUnlocked(int index)
    {
        return unlockedWeapons != null && index >= 0 && index < unlockedWeapons.Length && unlockedWeapons[index];
    }

    public void EquipWeapon(int weaponIndex)
    {
        if (availableWeapons == null || weaponIndex < 0 || weaponIndex >= availableWeapons.Length)
        {
            return;
        }

        if (!IsWeaponUnlocked(weaponIndex))
        {
            return;
        }

        WeaponDefinition definition = availableWeapons[weaponIndex];
        if (definition == null || definition.bulletPrefab == null)
        {
            Debug.LogWarning("Weapon is not configured at index " + weaponIndex);
            return;
        }

        currentDefinition = definition;
        currentWeaponIndex = weaponIndex;
        currentDamage = Mathf.Max(1, definition.damage + extraDamage);
        currentFireRate = Mathf.Max(0.1f, definition.fireRate);
        currentProjectileSpeed = Mathf.Max(0.1f, definition.projectileSpeed);
        currentProjectileKnockback = Mathf.Max(0f, definition.projectileKnockback);
        currentMagazineSize = Mathf.Max(1, definition.magazineSize);
        currentReloadTime = Mathf.Max(0.05f, definition.reloadTime);
        currentProjectilesPerShot = Mathf.Max(1, definition.projectilesPerShot);
        currentSpreadAngle = Mathf.Max(0f, definition.spreadAngle);
        currentAmmo = currentMagazineSize;
        isReloading = false;
        ReportAmmo();
    }

    private bool EnsureReadyToShoot()
    {
        if (firePoint == null)
        {
            firePoint = transform.Find("FirePoint");
        }

        return firePoint != null && currentDefinition != null && currentDefinition.bulletPrefab != null;
    }

    private void FireProjectiles()
    {
        if (currentProjectilesPerShot == 1)
        {
            SpawnProjectile(0f);
            return;
        }

        float totalSpread = currentSpreadAngle;
        float step = currentProjectilesPerShot > 1 ? totalSpread / (currentProjectilesPerShot - 1) : 0f;
        float startAngle = -totalSpread * 0.5f;

        for (int i = 0; i < currentProjectilesPerShot; i++)
        {
            float spreadOffset = startAngle + (step * i);
            SpawnProjectile(spreadOffset);
        }
    }

    private void SpawnProjectile(float spreadOffset)
    {
        Quaternion shotRotation = firePoint.rotation * Quaternion.Euler(0f, 0f, spreadOffset);
        GameObject bulletObject = ObjectPoolManager.Spawn(currentDefinition.bulletPrefab, firePoint.position, shotRotation);
        if (bulletObject == null)
        {
            return;
        }

        Bullet bullet = bulletObject.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.Initialize(currentProjectileSpeed, currentDamage, currentProjectileKnockback);
        }
    }

    private void BeginReload()
    {
        if (isReloading)
        {
            return;
        }

        isReloading = true;
        reloadTimer = currentReloadTime;
    }

    private void FinishReload()
    {
        isReloading = false;
        currentAmmo = currentMagazineSize;
        ReportAmmo();
    }

    private void ReportAmmo()
    {
        GameManager.ReportWeaponAmmo(currentAmmo, currentMagazineSize);
    }
}
