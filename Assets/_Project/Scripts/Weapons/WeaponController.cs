using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform firePoint;

    [Header("Loadout")]
    [SerializeField] private WeaponDefinition[] availableWeapons;

    [Header("Ammo")]
    [SerializeField] private int bulletsAmmo;
    [SerializeField] private int shellsAmmo;

    private WeaponDefinition currentDefinition;
    private int currentWeaponIndex = -1;
    private bool[] unlockedWeapons;
    private int[] ammoInMagazines;

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
    private int magazineSizeBonus;
    private float fireRateMultiplier = 1f;
    private float reloadTimeMultiplier = 1f;

    public string CurrentWeaponName => currentDefinition != null ? currentDefinition.weaponName : "";
    public int CurrentAmmo => currentAmmo;
    public int CurrentMagazineSize => currentMagazineSize;
    public bool IsReloading => isReloading;

    private void Awake()
    {
        if (availableWeapons != null)
        {
            unlockedWeapons = new bool[availableWeapons.Length];
            ammoInMagazines = new int[availableWeapons.Length];

            if (availableWeapons.Length > 0)
            {
                unlockedWeapons[0] = true;
                ammoInMagazines[0] = GetMagazineSize(availableWeapons[0]);
            }
        }

        EquipWeapon(0);
    }

    private void Update()
    {
        HandleKeyboardInput();
        HandleShootingInput();
        UpdateReload();
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

    private void UpdateReload()
    {
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
        SaveCurrentMagazine();
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

    public void ModifyDamage(int delta)
    {
        extraDamage += delta;
        currentDamage += delta;
        Debug.Log("Damage upgraded. Bonus: +" + extraDamage + ", total damage: " + currentDamage);
    }

    public void MultiplyFireRate(float multiplier)
    {
        fireRateMultiplier = Mathf.Max(0.1f, fireRateMultiplier * multiplier);
        currentFireRate = Mathf.Max(0.1f, currentFireRate * multiplier);
    }

    public void ModifyMagazineSize(int delta)
    {
        magazineSizeBonus += delta;
        currentMagazineSize = Mathf.Max(1, currentMagazineSize + delta);
        currentAmmo = Mathf.Min(currentAmmo, currentMagazineSize);
        SaveCurrentMagazine();
        ReportAmmo();
    }

    public void MultiplyReloadTime(float multiplier)
    {
        reloadTimeMultiplier = Mathf.Max(0.05f, reloadTimeMultiplier * multiplier);
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
                ammoInMagazines[i] = GetMagazineSize(availableWeapons[i]);
                AddAmmo(availableWeapons[i].ammoType, GetMagazineSize(availableWeapons[i]));
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

    public bool IsAmmoTypeUnlocked(WeaponDefinition.AmmoType ammoType)
    {
        if (ammoType == WeaponDefinition.AmmoType.Pistol)
        {
            return true;
        }

        if (availableWeapons == null || unlockedWeapons == null)
        {
            return false;
        }

        for (int i = 0; i < availableWeapons.Length; i++)
        {
            if (unlockedWeapons[i] && availableWeapons[i] != null && availableWeapons[i].ammoType == ammoType)
            {
                return true;
            }
        }

        return false;
    }

    public void AddAmmo(WeaponDefinition.AmmoType ammoType, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (ammoType == WeaponDefinition.AmmoType.Bullets)
        {
            bulletsAmmo += amount;
        }
        else if (ammoType == WeaponDefinition.AmmoType.Shells)
        {
            shellsAmmo += amount;
        }

        ReportAmmo();
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

        SaveCurrentMagazine();

        currentDefinition = definition;
        currentWeaponIndex = weaponIndex;
        currentDamage = Mathf.Max(1, definition.damage + extraDamage);
        currentFireRate = Mathf.Max(0.1f, definition.fireRate * fireRateMultiplier);
        currentProjectileSpeed = Mathf.Max(0.1f, definition.projectileSpeed);
        currentProjectileKnockback = Mathf.Max(0f, definition.projectileKnockback);
        currentMagazineSize = GetMagazineSize(definition);
        currentReloadTime = Mathf.Max(0.05f, definition.reloadTime * reloadTimeMultiplier);
        currentProjectilesPerShot = Mathf.Max(1, definition.projectilesPerShot);
        currentSpreadAngle = Mathf.Max(0f, definition.spreadAngle);
        currentAmmo = Mathf.Clamp(ammoInMagazines[weaponIndex], 0, currentMagazineSize);
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
        GameObject bulletObject = Instantiate(currentDefinition.bulletPrefab, firePoint.position, shotRotation);
        Bullet bullet = bulletObject.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.Initialize(currentProjectileSpeed, currentDamage, currentProjectileKnockback);
        }
    }

    private void BeginReload()
    {
        if (isReloading || !CanReload())
        {
            return;
        }

        isReloading = true;
        reloadTimer = currentReloadTime;
    }

    private bool CanReload()
    {
        if (currentDefinition == null || currentAmmo >= currentMagazineSize)
        {
            return false;
        }

        if (currentDefinition.ammoType == WeaponDefinition.AmmoType.Pistol)
        {
            return true;
        }

        return GetReserveAmmo(currentDefinition.ammoType) > 0;
    }

    private void FinishReload()
    {
        isReloading = false;

        if (currentDefinition.ammoType == WeaponDefinition.AmmoType.Pistol)
        {
            currentAmmo = currentMagazineSize;
        }
        else
        {
            int neededAmmo = currentMagazineSize - currentAmmo;
            int loadedAmmo = Mathf.Min(neededAmmo, GetReserveAmmo(currentDefinition.ammoType));
            RemoveReserveAmmo(currentDefinition.ammoType, loadedAmmo);
            currentAmmo += loadedAmmo;
        }

        SaveCurrentMagazine();
        ReportAmmo();
    }

    private int GetMagazineSize(WeaponDefinition definition)
    {
        return Mathf.Max(1, definition.magazineSize + magazineSizeBonus);
    }

    private void SaveCurrentMagazine()
    {
        if (ammoInMagazines == null || currentWeaponIndex < 0 || currentWeaponIndex >= ammoInMagazines.Length)
        {
            return;
        }

        ammoInMagazines[currentWeaponIndex] = currentAmmo;
    }

    private int GetReserveAmmo(WeaponDefinition.AmmoType ammoType)
    {
        if (ammoType == WeaponDefinition.AmmoType.Bullets)
        {
            return bulletsAmmo;
        }

        if (ammoType == WeaponDefinition.AmmoType.Shells)
        {
            return shellsAmmo;
        }

        return -1;
    }

    private void RemoveReserveAmmo(WeaponDefinition.AmmoType ammoType, int amount)
    {
        if (ammoType == WeaponDefinition.AmmoType.Bullets)
        {
            bulletsAmmo = Mathf.Max(0, bulletsAmmo - amount);
        }
        else if (ammoType == WeaponDefinition.AmmoType.Shells)
        {
            shellsAmmo = Mathf.Max(0, shellsAmmo - amount);
        }
    }

    private void ReportAmmo()
    {
        int reserveAmmo = currentDefinition != null ? GetReserveAmmo(currentDefinition.ammoType) : 0;
        GameManager.ReportWeaponAmmo(currentAmmo, reserveAmmo);
    }
}
