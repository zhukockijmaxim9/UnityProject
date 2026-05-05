using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("UI Panel")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private UpgradeButton[] upgradeButtons;

    private PlayerHealth playerHealth;
    private TopDownCharacterController playerController;
    private WeaponController weaponController;

    private bool level3WeaponUnlocked;
    private bool level10WeaponUnlocked;
    private bool level20WeaponUnlocked;
    private bool healthRegenUnlocked;

    public enum UpgradeType
    {
        MaxHealth,
        MoveSpeed,
        StaminaRegen,
        Damage,
        FireRate,
        MagazineSize,
        ReloadSpeed,
        HealthRegen
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
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }
    }

    private void Start()
    {
        FindPlayerReferences();
    }

    private void FindPlayerReferences()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        playerHealth = player.GetComponent<PlayerHealth>();
        playerController = player.GetComponent<TopDownCharacterController>();
        weaponController = player.GetComponent<WeaponController>();
    }

    public void OpenUpgradeMenu()
    {
        if (upgradePanel == null || upgradeButtons.Length < 3)
        {
            return;
        }

        FindPlayerReferences();
        ApplyFixedLevelUpgrades();

        UpgradeOption healthOption = TakeRandomOption(BuildHealthUpgrades());
        UpgradeOption movementOption = TakeRandomOption(BuildMovementUpgrades());
        UpgradeOption weaponOption = TakeRandomOption(BuildWeaponUpgrades());

        upgradeButtons[0].Setup(healthOption.title, healthOption.description, (int)healthOption.type);
        upgradeButtons[1].Setup(movementOption.title, movementOption.description, (int)movementOption.type);
        upgradeButtons[2].Setup(weaponOption.title, weaponOption.description, (int)weaponOption.type);

        upgradePanel.SetActive(true);
    }

    public void ApplyUpgrade(int typeIndex)
    {
        UpgradeType type = (UpgradeType)typeIndex;

        switch (type)
        {
            case UpgradeType.MaxHealth:
                playerHealth.IncreaseMaxHealth(20, true);
                break;
            case UpgradeType.MoveSpeed:
                playerController.moveSpeed *= 1.15f;
                break;
            case UpgradeType.StaminaRegen:
                playerController.staminaRegenRate *= 1.25f;
                break;
            case UpgradeType.Damage:
                weaponController.ModifyDamage(1);
                break;
            case UpgradeType.FireRate:
                weaponController.MultiplyFireRate(1.2f);
                break;
            case UpgradeType.MagazineSize:
                weaponController.ModifyMagazineSize(4);
                break;
            case UpgradeType.ReloadSpeed:
                weaponController.MultiplyReloadTime(0.8f);
                break;
            case UpgradeType.HealthRegen:
                playerHealth.MultiplyHealthRegen(1.25f);
                break;
        }

        FinishUpgrade();
    }

    private void ApplyFixedLevelUpgrades()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        int level = GameManager.Instance.currentLevel;

        if (level >= 3 && !level3WeaponUnlocked)
        {
            level3WeaponUnlocked = true;
            weaponController.UnlockNextWeapon();
        }

        if (level >= 10 && !level10WeaponUnlocked)
        {
            level10WeaponUnlocked = true;
            weaponController.UnlockNextWeapon();
        }

        if (level >= 20 && !level20WeaponUnlocked)
        {
            level20WeaponUnlocked = true;
            weaponController.UnlockNextWeapon();
        }

        if (level >= 5 && !healthRegenUnlocked)
        {
            healthRegenUnlocked = true;
            playerHealth.UnlockHealthRegen();
        }
    }

    private List<UpgradeOption> BuildHealthUpgrades()
    {
        List<UpgradeOption> options = new List<UpgradeOption>
        {
            new UpgradeOption { type = UpgradeType.MaxHealth, title = "Vitality", description = "+20 Max HP" }
        };

        if (healthRegenUnlocked)
        {
            options.Add(new UpgradeOption { type = UpgradeType.HealthRegen, title = "Recovery", description = "+25% HP Regen" });
        }

        return options;
    }

    private List<UpgradeOption> BuildMovementUpgrades()
    {
        return new List<UpgradeOption>
        {
            new UpgradeOption { type = UpgradeType.MoveSpeed, title = "Agility", description = "+15% Move Speed" },
            new UpgradeOption { type = UpgradeType.StaminaRegen, title = "Endurance", description = "+25% Stamina Regen" }
        };
    }

    private List<UpgradeOption> BuildWeaponUpgrades()
    {
        return new List<UpgradeOption>
        {
            new UpgradeOption { type = UpgradeType.Damage, title = "Heavy Bullets", description = "+1 Damage" },
            new UpgradeOption { type = UpgradeType.FireRate, title = "Rapid Fire", description = "+20% Fire Rate" },
            new UpgradeOption { type = UpgradeType.MagazineSize, title = "Extended Mag", description = "+4 Magazine Size" },
            new UpgradeOption { type = UpgradeType.ReloadSpeed, title = "Quick Hands", description = "+20% Reload Speed" }
        };
    }

    private UpgradeOption TakeRandomOption(List<UpgradeOption> options)
    {
        int index = Random.Range(0, options.Count);
        UpgradeOption option = options[index];
        options.RemoveAt(index);
        return option;
    }

    private void FinishUpgrade()
    {
        upgradePanel.SetActive(false);
        GameManager.Instance.CloseUpgradeMenu();
    }
}
