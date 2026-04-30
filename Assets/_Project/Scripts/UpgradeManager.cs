using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("UI Panel")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private UpgradeButton[] upgradeButtons; // 0 - Survival, 1 - Mobility, 2 - Weaponry

    private PlayerHealth playerHealth;
    private TopDownCharacterController playerController;
    private WeaponController weaponController;

    public enum UpgradeType
    {
        MaxHealth, Heal, 
        MoveSpeed, StaminaRegen, 
        Damage, FireRate, MagazineSize, UnlockWeapon, ReloadSpeed
    }

    private struct UpgradeOption
    {
        public UpgradeType type;
        public string title;
        public string description;
    }

    private void Awake()
    {
        Instance = this;
        if (upgradePanel != null) upgradePanel.SetActive(false);
    }

    private void Start()
    {
        FindPlayerReferences();
    }

    private void FindPlayerReferences()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            playerController = player.GetComponent<TopDownCharacterController>();
            weaponController = player.GetComponentInChildren<WeaponController>();
        }
    }

    public void OpenUpgradeMenu()
    {
        if (upgradePanel == null || upgradeButtons.Length < 3) return;
        FindPlayerReferences();

        // Категория 1: Выживаемость (Здоровье)
        List<UpgradeOption> survivalOptions = new List<UpgradeOption>
        {
            new UpgradeOption { type = UpgradeType.MaxHealth, title = "Vitality", description = "+20 Max HP" },
            new UpgradeOption { type = UpgradeType.Heal, title = "First Aid", description = "Heal 50 HP" }
        };

        // Категория 2: Подвижность (Скорость/Стамина)
        List<UpgradeOption> mobilityOptions = new List<UpgradeOption>
        {
            new UpgradeOption { type = UpgradeType.MoveSpeed, title = "Agility", description = "+15% Move Speed" },
            new UpgradeOption { type = UpgradeType.StaminaRegen, title = "Endurance", description = "+25% Stamina Regen" }
        };

        // Категория 3: Оружие
        List<UpgradeOption> weaponOptions = new List<UpgradeOption>
        {
            new UpgradeOption { type = UpgradeType.Damage, title = "Heavy Bullets", description = "+1 Damage" },
            new UpgradeOption { type = UpgradeType.FireRate, title = "Rapid Fire", description = "+20% Fire Rate" },
            new UpgradeOption { type = UpgradeType.MagazineSize, title = "Extended Mag", description = "+4 Magazine Size" },
            new UpgradeOption { type = UpgradeType.UnlockWeapon, title = "Arsenal", description = "Unlock Next Weapon" },
            new UpgradeOption { type = UpgradeType.ReloadSpeed, title = "Quick Hands", description = "+20% Reload Speed" }
        };

        // Выбираем по одному случайному из каждой категории
        UpgradeOption opt1 = survivalOptions[Random.Range(0, survivalOptions.Count)];
        UpgradeOption opt2 = mobilityOptions[Random.Range(0, mobilityOptions.Count)];
        UpgradeOption opt3 = weaponOptions[Random.Range(0, weaponOptions.Count)];

        // Настраиваем кнопки
        upgradeButtons[0].Setup(opt1.title, opt1.description, (int)opt1.type);
        upgradeButtons[1].Setup(opt2.title, opt2.description, (int)opt2.type);
        upgradeButtons[2].Setup(opt3.title, opt3.description, (int)opt3.type);

        upgradePanel.SetActive(true);
    }

    public void ApplyUpgrade(int typeIndex)
    {
        UpgradeType type = (UpgradeType)typeIndex;
        switch (type)
        {
            case UpgradeType.MaxHealth: playerHealth.IncreaseMaxHealth(20, true); break;
            case UpgradeType.Heal: playerHealth.Heal(50); break;
            case UpgradeType.MoveSpeed: playerController.moveSpeed *= 1.15f; break;
            case UpgradeType.StaminaRegen: 
                playerController.staminaRegenRate *= 1.25f; 
                break;
            case UpgradeType.Damage: weaponController.ModifyDamage(1); break;
            case UpgradeType.FireRate: weaponController.MultiplyFireRate(1.2f); break;
            case UpgradeType.MagazineSize: weaponController.ModifyMagazineSize(4); break;
            case UpgradeType.ReloadSpeed: weaponController.MultiplyReloadTime(0.8f); break;
            case UpgradeType.UnlockWeapon: weaponController.UnlockNextWeapon(); break;
        }

        FinishUpgrade();
    }

    private void FinishUpgrade()
    {
        upgradePanel.SetActive(false);
        GameManager.Instance.CloseUpgradeMenu();
    }
}
