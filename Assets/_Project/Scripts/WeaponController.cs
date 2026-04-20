using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform firePoint;

    [Header("Loadout")]
    [SerializeField] private WeaponDefinition[] availableWeapons;
    [SerializeField] private int startingWeaponIndex;

    [Header("Fallback Weapon")]
    [SerializeField] private string fallbackWeaponName = "Pistol";
    [SerializeField] private GameObject fallbackBulletPrefab;
    [SerializeField] private int fallbackDamage = 1;
    [SerializeField] private float fallbackFireRate = 4f;
    [SerializeField] private float fallbackProjectileSpeed = 15f;
    [SerializeField] private float fallbackProjectileKnockback = 3f;
    [SerializeField] private int fallbackMagazineSize = 8;
    [SerializeField] private float fallbackReloadTime = 1.2f;
    [SerializeField] private int fallbackProjectilesPerShot = 1;
    [SerializeField] private float fallbackSpreadAngle = 0f;

    private WeaponDefinition currentDefinition;
    private int currentWeaponIndex = -1;
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

    public string CurrentWeaponName => currentDefinition != null ? currentDefinition.weaponName : fallbackWeaponName;
    public int CurrentAmmo => currentAmmo;
    public int CurrentMagazineSize => currentMagazineSize;
    public bool IsReloading => isReloading;

    public void Bootstrap(GameObject bulletPrefab, Transform muzzle, float fireRate)
    {
        if (firePoint == null)
        {
            firePoint = muzzle;
        }

        if (fallbackBulletPrefab == null)
        {
            fallbackBulletPrefab = bulletPrefab;
        }

        if (fireRate > 0f)
        {
            fallbackFireRate = fireRate;
        }

        if (currentDefinition == null || currentWeaponIndex < 0)
        {
            ApplyFallbackWeapon();
        }

        ReportAmmo();
    }

    private void Awake()
    {
        EquipStartingWeapon();
    }

    private void Update()
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
        nextShotTime = Time.time + (1f / Mathf.Max(0.01f, currentFireRate));
        ReportAmmo();

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

        EquipWeapon((currentWeaponIndex + 1) % availableWeapons.Length);
    }

    public void PreviousWeapon()
    {
        if (availableWeapons == null || availableWeapons.Length == 0)
        {
            return;
        }

        int previousIndex = currentWeaponIndex - 1;
        if (previousIndex < 0)
        {
            previousIndex = availableWeapons.Length - 1;
        }

        EquipWeapon(previousIndex);
    }

    public void ModifyDamage(int delta)
    {
        currentDamage = Mathf.Max(1, currentDamage + delta);
    }

    public void MultiplyFireRate(float multiplier)
    {
        currentFireRate = Mathf.Max(0.1f, currentFireRate * multiplier);
    }

    public void ModifyMagazineSize(int delta)
    {
        currentMagazineSize = Mathf.Max(1, currentMagazineSize + delta);
        currentAmmo = Mathf.Min(currentAmmo, currentMagazineSize);
    }

    public void MultiplyReloadTime(float multiplier)
    {
        currentReloadTime = Mathf.Max(0.05f, currentReloadTime * multiplier);
    }

    public void ModifyProjectileCount(int delta)
    {
        currentProjectilesPerShot = Mathf.Max(1, currentProjectilesPerShot + delta);
    }

    private void EquipStartingWeapon()
    {
        if (availableWeapons != null && availableWeapons.Length > 0)
        {
            EquipWeapon(Mathf.Clamp(startingWeaponIndex, 0, availableWeapons.Length - 1));
            return;
        }

        ApplyFallbackWeapon();
    }

    private void EquipWeapon(int weaponIndex)
    {
        if (availableWeapons == null || weaponIndex < 0 || weaponIndex >= availableWeapons.Length)
        {
            ApplyFallbackWeapon();
            return;
        }

        WeaponDefinition definition = availableWeapons[weaponIndex];
        if (definition == null)
        {
            ApplyFallbackWeapon();
            return;
        }

        currentDefinition = definition;
        currentWeaponIndex = weaponIndex;
        fallbackWeaponName = string.IsNullOrWhiteSpace(definition.weaponName) ? fallbackWeaponName : definition.weaponName;
        fallbackBulletPrefab = definition.bulletPrefab != null ? definition.bulletPrefab : fallbackBulletPrefab;
        currentDamage = Mathf.Max(1, definition.damage);
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

    private void ApplyFallbackWeapon()
    {
        currentDefinition = null;
        currentWeaponIndex = -1;
        currentDamage = Mathf.Max(1, fallbackDamage);
        currentFireRate = Mathf.Max(0.1f, fallbackFireRate);
        currentProjectileSpeed = Mathf.Max(0.1f, fallbackProjectileSpeed);
        currentProjectileKnockback = Mathf.Max(0f, fallbackProjectileKnockback);
        currentMagazineSize = Mathf.Max(1, fallbackMagazineSize);
        currentReloadTime = Mathf.Max(0.05f, fallbackReloadTime);
        currentProjectilesPerShot = Mathf.Max(1, fallbackProjectilesPerShot);
        currentSpreadAngle = Mathf.Max(0f, fallbackSpreadAngle);
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

        return firePoint != null && fallbackBulletPrefab != null;
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
        GameObject bulletObject = ObjectPoolManager.Spawn(fallbackBulletPrefab, firePoint.position, shotRotation);

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
