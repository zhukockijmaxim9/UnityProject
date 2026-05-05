using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    [SerializeField] private WeaponDefinition.AmmoType ammoType = WeaponDefinition.AmmoType.Bullets;
    [SerializeField] private int amount = 30;

    public void Setup(WeaponDefinition.AmmoType newAmmoType, int newAmount)
    {
        ammoType = newAmmoType;
        amount = newAmount;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        WeaponController weaponController = other.GetComponent<WeaponController>();
        if (weaponController == null)
        {
            return;
        }

        weaponController.AddAmmo(ammoType, amount);
        Destroy(gameObject);
    }
}
