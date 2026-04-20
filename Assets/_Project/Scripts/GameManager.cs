using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scoring")]
    [SerializeField] private int defaultPointsPerKill = 100;
    [SerializeField] private float restartDelay = 2.5f;

    private int currentWave;
    private int killCount;
    private int score;
    private int currentHealth;
    private int maxHealth;
    private int currentAmmo;
    private int maxAmmo;
    private bool waveActive;
    private bool isGameOver;
    private Coroutine restartCoroutine;

    private Canvas hudCanvas;
    private Text healthText;
    private Text ammoText;
    private Text waveText;
    private Text killsText;
    private Text scoreText;
    private Text statusText;

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
        return !EnsureInstance().isGameOver;
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
        score += pointsAwarded > 0 ? pointsAwarded : defaultPointsPerKill;
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

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        EnsureHudExists();
        RefreshHud();
    }

    private void EnsureHudExists()
    {
        if (hudCanvas != null && healthText != null && ammoText != null && waveText != null && killsText != null && scoreText != null && statusText != null)
        {
            return;
        }

        Font hudFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new GameObject("HUDCanvas");
        canvasObject.transform.SetParent(transform, false);

        hudCanvas = canvasObject.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        healthText = CreateHudText("HealthText", hudFont, new Vector2(-24f, -24f));
        ammoText = CreateHudText("AmmoText", hudFont, new Vector2(-24f, -64f));
        waveText = CreateHudText("WaveText", hudFont, new Vector2(-24f, -104f));
        killsText = CreateHudText("KillsText", hudFont, new Vector2(-24f, -144f));
        scoreText = CreateHudText("ScoreText", hudFont, new Vector2(-24f, -184f));
        statusText = CreateHudText("StatusText", hudFont, new Vector2(-24f, -224f));
    }

    private Text CreateHudText(string objectName, Font font, Vector2 anchoredPosition)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(hudCanvas.transform, false);

        Text textComponent = textObject.AddComponent<Text>();
        textComponent.font = font;
        textComponent.fontSize = 28;
        textComponent.fontStyle = FontStyle.Bold;
        textComponent.color = Color.white;
        textComponent.alignment = TextAnchor.UpperRight;
        textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
        textComponent.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(400f, 36f);

        return textComponent;
    }

    private void RefreshHud()
    {
        if (healthText == null || ammoText == null || waveText == null || killsText == null || scoreText == null || statusText == null)
        {
            return;
        }

        healthText.text = maxHealth > 0 ? $"HP: {currentHealth}/{maxHealth}" : "HP: --";
        ammoText.text = maxAmmo > 0 ? $"Ammo: {currentAmmo}/{maxAmmo}" : "Ammo: --";
        waveText.text = $"Wave: {Mathf.Max(1, currentWave)}";
        killsText.text = $"Kills: {killCount}";
        scoreText.text = $"Score: {score:0000}";
        statusText.text = isGameOver ? "Status: Game Over" : waveActive ? "Status: Wave Active" : "Status: Between Waves";
        statusText.color = isGameOver
            ? new Color(1f, 0.45f, 0.45f)
            : waveActive
                ? new Color(0.55f, 1f, 0.55f)
                : new Color(1f, 0.9f, 0.45f);
    }

    private IEnumerator RestartCurrentSceneAfterDelay()
    {
        yield return new WaitForSeconds(restartDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
