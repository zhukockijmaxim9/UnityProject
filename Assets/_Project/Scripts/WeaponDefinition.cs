using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Game/Weapons/Weapon Definition")]
public class WeaponDefinition : ScriptableObject
{
    [Header("Identity")]
    public string weaponName = "Weapon";

    [Header("Projectile")]
    public GameObject bulletPrefab;
    public int damage = 1;
    public float fireRate = 4f;
    public float projectileSpeed = 15f;
    public float projectileKnockback = 3f;

    [Header("Magazine")]
    public int magazineSize = 8;
    public float reloadTime = 1.2f;

    [Header("Shot Pattern")]
    public int projectilesPerShot = 1;
    public float spreadAngle = 0f;
}
