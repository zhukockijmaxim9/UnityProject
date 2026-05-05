using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scoring")]
    [SerializeField] private int defaultPointsPerKill = 100;
    [SerializeField] private float restartDelay = 2.5f;

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
    private int maxAmmo;
    private bool waveActive;
    private bool isGameOver;
    private bool isUpgradeMenuOpen;
    private Coroutine restartCoroutine;
    private GameHud hud;

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

    public static void ReportWeaponAmmo(int ammoInMagazine, int magazineSize)
    {
        EnsureInstance().SetWeaponAmmo(ammoInMagazine, magazineSize);
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

        EnsureHudExists();
        RefreshHud();
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
        RefreshHud();

        if (restartCoroutine != null)
        {
            StopCoroutine(restartCoroutine);
        }

        restartCoroutine = StartCoroutine(RestartCurrentSceneAfterDelay());
    }

    public void SetWeaponAmmo(int ammoInMagazine, int magazineSize)
    {
        currentAmmo = Mathf.Max(0, ammoInMagazine);
        maxAmmo = Mathf.Max(0, magazineSize);
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
        maxAmmo = 0;
        waveActive = false;
        isGameOver = false;

        if (restartCoroutine != null)
        {
            StopCoroutine(restartCoroutine);
            restartCoroutine = null;
        }

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
        EnsureHudExists();
        RefreshHud();
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
        EnsureHudExists();
        hud.Refresh(currentHealth, maxHealth, currentAmmo, maxAmmo, currentWave, killCount, score, currentLevel, currentXP, targetXP);
    }

    private IEnumerator RestartCurrentSceneAfterDelay()
    {
        yield return new WaitForSeconds(restartDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
