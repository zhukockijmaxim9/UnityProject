using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private const string GameplaySceneName = "SampleScene";
    private const string MainMenuSceneName = "MainMenuScene";

    [Header("Scoring")]
    [SerializeField] private int defaultPointsPerKill = 100;

    [Header("XP System")]
    public int currentLevel = 1;
    public int currentXP = 0;
    public int targetXP = 500;
    public float xpMultiplier = 1.2f;

    private int currentWave;
    private int killCount;
    private int aliveEnemies;
    private int score;
    private int currentHealth;
    private int maxHealth;
    private int currentAmmo;
    private int reserveAmmo;
    private WeaponController weaponController;
    private bool waveActive;
    private bool isGameOver;
    private bool isUpgradeMenuOpen;
    private GameHud hud;
    private DeathScreen deathScreen;

    public static bool IsGameOver => Instance != null && Instance.isGameOver;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static GameManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

#if UNITY_2023_1_OR_NEWER
        Instance = FindAnyObjectByType<GameManager>();
#else
        Instance = FindObjectOfType<GameManager>();
#endif
        if (Instance != null)
        {
            return Instance;
        }

        GameObject managerObject = new GameObject("GameManager");
        return managerObject.AddComponent<GameManager>();
    }

    public static void ReportWave(int waveNumber)
    {
        EnsureInstance().SetWave(waveNumber);
    }

    public static void ReportEnemySpawned()
    {
        EnsureInstance().aliveEnemies++;
    }

    public static int GetAliveEnemies()
    {
        return EnsureInstance().aliveEnemies;
    }

    public static void ReportEnemyKilled(int pointsAwarded)
    {
        EnsureInstance().RegisterEnemyKilled(pointsAwarded);
    }

    public static void ReportPlayerHealth(int playerCurrentHealth, int playerMaxHealth)
    {
        EnsureInstance().SetPlayerHealth(playerCurrentHealth, playerMaxHealth);
    }

    public static void ReportPlayerDeath()
    {
        EnsureInstance().HandlePlayerDeath();
    }

    public static void ReportWeaponAmmo(int ammoInMagazine, int weaponReserveAmmo)
    {
        EnsureInstance().SetWeaponAmmo(ammoInMagazine, weaponReserveAmmo);
    }

    public static void ReportWeaponLoadout(WeaponController controller)
    {
        EnsureInstance().SetWeaponLoadout(controller);
    }

    public static void ReportWaveState(bool active)
    {
        EnsureInstance().SetWaveState(active);
    }

    public static void ResetStats()
    {
        EnsureInstance().ResetRun();
    }

    public static bool CanGameplayRun()
    {
        return !EnsureInstance().isGameOver && !EnsureInstance().isUpgradeMenuOpen;
    }

    public static int GetCurrentWaveNumber()
    {
        return EnsureInstance().currentWave;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += HandleSceneLoaded;

        if (SceneManager.GetActiveScene().isLoaded)
        {
            UpdateHudForActiveScene(SceneManager.GetActiveScene());
        }
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Instance = null;
    }

    public void SetWave(int waveNumber)
    {
        currentWave = Mathf.Max(0, waveNumber);
        RefreshHud();
    }

    public void RegisterEnemyKilled(int pointsAwarded)
    {
        killCount++;
        aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
        score += pointsAwarded > 0 ? pointsAwarded : defaultPointsPerKill;

        GainXP(pointsAwarded / 2);
        RefreshHud();
    }

    public void CloseUpgradeMenu()
    {
        isUpgradeMenuOpen = false;
        Time.timeScale = 1f;
        RefreshHud();
    }

    public void SetPlayerHealth(int playerCurrentHealth, int playerMaxHealth)
    {
        currentHealth = Mathf.Max(0, playerCurrentHealth);
        maxHealth = Mathf.Max(0, playerMaxHealth);
        RefreshHud();
    }

    public void HandlePlayerDeath()
    {
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;
        waveActive = false;

        if (PauseMenu.isPaused)
        {
            ClosePauseMenuIfOpen();
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        EnsureDeathScreenExists();
        deathScreen.Show(currentWave, killCount, score, currentLevel);
    }

    public void RestartAfterDeath()
    {
        Time.timeScale = 1f;
        HideDeathScreen();
        isGameOver = false;
        SceneManager.LoadScene(GameplaySceneName);
    }

    public void LoadMainMenuAfterDeath()
    {
        Time.timeScale = 1f;
        HideDeathScreen();
        isGameOver = false;
        SceneManager.LoadScene(MainMenuSceneName);
    }

    public void SetWeaponAmmo(int ammoInMagazine, int weaponReserveAmmo)
    {
        currentAmmo = Mathf.Max(0, ammoInMagazine);
        reserveAmmo = weaponReserveAmmo;
        RefreshHud();
    }

    public void SetWeaponLoadout(WeaponController controller)
    {
        weaponController = controller;
        RefreshHud();
    }

    public void SetWaveState(bool active)
    {
        waveActive = active && !isGameOver;
        RefreshHud();
    }

    public void ResetRun()
    {
        currentWave = 0;
        killCount = 0;
        aliveEnemies = 0;
        score = 0;
        currentHealth = 0;
        maxHealth = 0;
        currentAmmo = 0;
        reserveAmmo = 0;
        weaponController = null;
        waveActive = false;
        isGameOver = false;

        HideDeathScreen();
        RefreshHud();
    }

    private void GainXP(int amount)
    {
        currentXP += amount;
        if (currentXP >= targetXP)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentXP -= targetXP;
        currentLevel++;
        targetXP = Mathf.RoundToInt(targetXP * xpMultiplier);

        isUpgradeMenuOpen = true;
        Time.timeScale = 0f;

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OpenUpgradeMenu();
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        UpdateHudForActiveScene(scene);
    }

    private bool IsGameplayScene(Scene scene)
    {
        return scene.name == GameplaySceneName;
    }

    private void UpdateHudForActiveScene(Scene activeScene)
    {
        bool isGameplay = IsGameplayScene(activeScene);

        EnsureHudExists();
        hud.SetVisible(isGameplay);
        HideDeathScreen();

        if (!isGameplay)
        {
            isUpgradeMenuOpen = false;
            isGameOver = false;
            Time.timeScale = 1f;
            return;
        }

        ResetRun();
    }

    private void EnsureDeathScreenExists()
    {
        if (deathScreen != null)
        {
            return;
        }

        deathScreen = GetComponent<DeathScreen>();
        if (deathScreen == null)
        {
            deathScreen = gameObject.AddComponent<DeathScreen>();
        }

        deathScreen.RestartRequested += RestartAfterDeath;
        deathScreen.MainMenuRequested += LoadMainMenuAfterDeath;
        deathScreen.EnsureCreated();
    }

    private void HideDeathScreen()
    {
        if (deathScreen != null)
        {
            deathScreen.Hide();
        }
    }

    private void ClosePauseMenuIfOpen()
    {
#if UNITY_2023_1_OR_NEWER
        PauseMenu pauseMenu = FindAnyObjectByType<PauseMenu>();
#else
        PauseMenu pauseMenu = FindObjectOfType<PauseMenu>();
#endif
        if (pauseMenu != null)
        {
            pauseMenu.ForceClose();
        }
    }

    private void EnsureHudExists()
    {
        if (hud == null)
        {
            hud = GetComponent<GameHud>();
            if (hud == null)
            {
                hud = gameObject.AddComponent<GameHud>();
            }
        }

        hud.EnsureCreated();
    }

    private void RefreshHud()
    {
        if (!IsGameplayScene(SceneManager.GetActiveScene()))
        {
            return;
        }

        EnsureHudExists();
        hud.Refresh(currentHealth, maxHealth, currentAmmo, reserveAmmo, currentWave, killCount, score, currentLevel, currentXP, targetXP);
        hud.RefreshWeaponBar(weaponController);
    }
}
