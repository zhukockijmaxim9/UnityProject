using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Game/Weapons/Weapon Definition")]
public class WeaponDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Название оружия, которое будет отображаться в UI")]
    public string weaponName = "Weapon";

    [Header("Projectile")]
    [Tooltip("Префаб пули, которой стреляет это оружие")]
    public GameObject bulletPrefab;
    
    [Tooltip("Урон от одной пули")]
    public int damage = 1;
    
    [Tooltip("Скорострельность (выстрелов в секунду)")]
    public float fireRate = 4f;
    
    [Tooltip("Скорость полета пули")]
    public float projectileSpeed = 15f;
    
    [Tooltip("Сила отбрасывания врага при попадании")]
    public float projectileKnockback = 3f;

    [Header("Magazine")]
    [Tooltip("Сколько патронов в одной обойме")]
    public int magazineSize = 8;
    
    [Tooltip("Время перезарядки в секундах")]
    public float reloadTime = 1.2f;

    [Header("Shot Pattern")]
    [Tooltip("Количество пуль за один выстрел (например, 1 для пистолета, 8 для дробовика)")]
    public int projectilesPerShot = 1;
    
    [Range(0, 90)]
    [Tooltip("Угол разброса пуль в градусах")]
    public float spreadAngle = 0f;
}
