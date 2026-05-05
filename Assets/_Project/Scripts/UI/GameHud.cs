using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHud : MonoBehaviour
{
    private Canvas hudCanvas;
    private TextMeshProUGUI healthText;
    private TextMeshProUGUI ammoText;
    private TextMeshProUGUI waveText;
    private TextMeshProUGUI killsText;
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI statusText;

    public void EnsureCreated()
    {
        if (hudCanvas != null && healthText != null && ammoText != null && waveText != null && killsText != null && scoreText != null && statusText != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("HUDCanvas_Auto");
        canvasObject.transform.SetParent(transform, false);

        hudCanvas = canvasObject.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        float rightPadding = -40f;
        healthText = CreateHudText("HealthText", new Vector2(rightPadding, -40f));
        ammoText = CreateHudText("AmmoText", new Vector2(rightPadding, -80f));
        waveText = CreateHudText("WaveText", new Vector2(rightPadding, -120f));
        killsText = CreateHudText("KillsText", new Vector2(rightPadding, -160f));
        scoreText = CreateHudText("ScoreText", new Vector2(rightPadding, -200f));
        statusText = CreateHudText("StatusText", new Vector2(rightPadding, -240f));
    }

    public void Refresh(
        int currentHealth,
        int maxHealth,
        int currentAmmo,
        int reserveAmmo,
        int currentWave,
        int killCount,
        int score,
        int currentLevel,
        int currentXP,
        int targetXP)
    {
        EnsureCreated();

        healthText.text = maxHealth > 0 ? $"HP: {currentHealth}/{maxHealth}" : "HP: --";
        ammoText.text = reserveAmmo < 0 ? $"Ammo: {currentAmmo}/∞" : $"Ammo: {currentAmmo}/{reserveAmmo}";
        waveText.text = $"Wave: {Mathf.Max(1, currentWave)}";
        killsText.text = $"Kills: {killCount}";
        scoreText.text = $"Score: {score:0000}";
        statusText.text = $"LVL: {currentLevel} | XP: {currentXP}/{targetXP}";
        statusText.color = Color.cyan;
    }

    private TextMeshProUGUI CreateHudText(string objectName, Vector2 anchoredPosition)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(hudCanvas.transform, false);

        TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
        textComponent.fontSize = 32;
        textComponent.fontWeight = FontWeight.Bold;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Right;
        textComponent.outlineWidth = 0.2f;
        textComponent.outlineColor = Color.black;

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(500f, 50f);

        return textComponent;
    }
}
