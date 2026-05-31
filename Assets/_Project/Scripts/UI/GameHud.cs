using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHud : MonoBehaviour
{
    private const int MaxWeaponSlots = 4;
    private const float SlotSize = 76f;
    private const float SlotSpacing = 14f;

    private Canvas hudCanvas;
    private TextMeshProUGUI healthText;
    private TextMeshProUGUI ammoText;
    private TextMeshProUGUI waveText;
    private TextMeshProUGUI killsText;
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI statusText;
    private RectTransform weaponBarRoot;
    private WeaponSlotView[] weaponSlots;

    private class WeaponSlotView
    {
        public GameObject root;
        public Image background;
        public Image highlight;
        public Image icon;
        public TextMeshProUGUI hotkeyText;
    }

    public void EnsureCreated()
    {
        if (hudCanvas != null && healthText != null && ammoText != null && waveText != null && killsText != null && scoreText != null && statusText != null)
        {
            EnsureWeaponBarCreated();
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

        EnsureWeaponBarCreated();
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

        healthText.text = maxHealth > 0 ? $"Здоровье: {currentHealth}/{maxHealth}" : "Здоровье: --";
        ammoText.text = reserveAmmo < 0 ? $"Патроны: {currentAmmo}/∞" : $"Патроны: {currentAmmo}/{reserveAmmo}";
        waveText.text = $"Волна: {Mathf.Max(1, currentWave)}";
        killsText.text = $"Убийств: {killCount}";
        scoreText.text = $"Счёт: {score:0000}";
        statusText.text = $"Ур. {currentLevel} | Опыт: {currentXP}/{targetXP}";
        statusText.color = Color.cyan;
    }

    public void RefreshWeaponBar(WeaponController weaponController)
    {
        EnsureCreated();

        if (weaponBarRoot == null || weaponSlots == null)
        {
            return;
        }

        int weaponCount = weaponController != null ? weaponController.WeaponCount : 0;
        int currentIndex = weaponController != null ? weaponController.CurrentWeaponIndex : -1;
        int visibleSlots = 0;

        for (int i = 0; i < weaponSlots.Length; i++)
        {
            WeaponSlotView slot = weaponSlots[i];
            bool isUnlocked = weaponController != null && weaponController.IsWeaponUnlocked(i);
            bool hasWeapon = i < weaponCount;
            bool showSlot = hasWeapon && isUnlocked;

            slot.root.SetActive(showSlot);
            if (!showSlot)
            {
                continue;
            }

            visibleSlots++;

            WeaponDefinition definition = weaponController.GetWeaponDefinition(i);
            bool isSelected = i == currentIndex;

            slot.hotkeyText.text = (i + 1).ToString();
            slot.hotkeyText.color = isSelected ? MenuTheme.Accent : MenuTheme.Body;

            if (definition != null && definition.icon != null)
            {
                slot.icon.sprite = definition.icon;
                slot.icon.color = isSelected ? Color.white : new Color(0.75f, 0.8f, 0.86f, 0.9f);
                slot.icon.enabled = true;
            }
            else
            {
                slot.icon.enabled = false;
            }

            slot.background.color = isSelected
                ? new Color(0.1f, 0.24f, 0.32f, 0.95f)
                : new Color(0.08f, 0.1f, 0.16f, 0.78f);

            slot.highlight.enabled = isSelected;
        }

        float barWidth = visibleSlots > 0
            ? (visibleSlots * SlotSize) + ((visibleSlots - 1) * SlotSpacing)
            : 0f;
        weaponBarRoot.sizeDelta = new Vector2(barWidth, SlotSize + 8f);
    }

    public void SetVisible(bool visible)
    {
        EnsureCreated();

        if (hudCanvas != null)
        {
            hudCanvas.gameObject.SetActive(visible);
        }
    }

    private void EnsureWeaponBarCreated()
    {
        if (weaponBarRoot != null && weaponSlots != null)
        {
            return;
        }

        GameObject barObject = new GameObject("WeaponBar");
        barObject.transform.SetParent(hudCanvas.transform, false);

        weaponBarRoot = barObject.AddComponent<RectTransform>();
        weaponBarRoot.anchorMin = new Vector2(0.5f, 0f);
        weaponBarRoot.anchorMax = new Vector2(0.5f, 0f);
        weaponBarRoot.pivot = new Vector2(0.5f, 0f);
        weaponBarRoot.anchoredPosition = new Vector2(0f, 36f);

        HorizontalLayoutGroup layout = barObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = SlotSpacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        weaponSlots = new WeaponSlotView[MaxWeaponSlots];
        for (int i = 0; i < MaxWeaponSlots; i++)
        {
            weaponSlots[i] = CreateWeaponSlot(barObject.transform, i + 1);
        }
    }

    private WeaponSlotView CreateWeaponSlot(Transform parent, int hotkeyNumber)
    {
        GameObject slotObject = new GameObject($"WeaponSlot_{hotkeyNumber}");
        slotObject.transform.SetParent(parent, false);

        RectTransform slotRect = slotObject.AddComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(SlotSize, SlotSize);

        LayoutElement layoutElement = slotObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = SlotSize;
        layoutElement.preferredHeight = SlotSize;

        Image background = slotObject.AddComponent<Image>();
        background.color = new Color(0.08f, 0.1f, 0.16f, 0.78f);

        GameObject highlightObject = new GameObject("Highlight");
        highlightObject.transform.SetParent(slotObject.transform, false);

        RectTransform highlightRect = highlightObject.AddComponent<RectTransform>();
        highlightRect.anchorMin = Vector2.zero;
        highlightRect.anchorMax = Vector2.one;
        highlightRect.offsetMin = new Vector2(-3f, -3f);
        highlightRect.offsetMax = new Vector2(3f, 3f);

        Image highlight = highlightObject.AddComponent<Image>();
        highlight.color = MenuTheme.AccentSoft;
        highlight.raycastTarget = false;

        Outline highlightOutline = highlightObject.AddComponent<Outline>();
        highlightOutline.effectColor = MenuTheme.Accent;
        highlightOutline.effectDistance = new Vector2(2f, -2f);

        GameObject hotkeyObject = new GameObject("Hotkey");
        hotkeyObject.transform.SetParent(slotObject.transform, false);

        RectTransform hotkeyRect = hotkeyObject.AddComponent<RectTransform>();
        hotkeyRect.anchorMin = new Vector2(0f, 1f);
        hotkeyRect.anchorMax = new Vector2(0f, 1f);
        hotkeyRect.pivot = new Vector2(0f, 1f);
        hotkeyRect.anchoredPosition = new Vector2(8f, -4f);
        hotkeyRect.sizeDelta = new Vector2(24f, 24f);

        TextMeshProUGUI hotkeyText = hotkeyObject.AddComponent<TextMeshProUGUI>();
        MenuTheme.EnsureFont(hotkeyText);
        hotkeyText.fontSize = 24f;
        hotkeyText.fontWeight = FontWeight.Bold;
        hotkeyText.alignment = TextAlignmentOptions.TopLeft;
        hotkeyText.text = hotkeyNumber.ToString();

        GameObject iconObject = new GameObject("Icon");
        iconObject.transform.SetParent(slotObject.transform, false);

        RectTransform iconRect = iconObject.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(0f, -6f);
        iconRect.sizeDelta = new Vector2(44f, 44f);

        Image icon = iconObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        return new WeaponSlotView
        {
            root = slotObject,
            background = background,
            highlight = highlight,
            icon = icon,
            hotkeyText = hotkeyText
        };
    }

    private TextMeshProUGUI CreateHudText(string objectName, Vector2 anchoredPosition)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(hudCanvas.transform, false);

        TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
        MenuTheme.EnsureFont(textComponent);
        textComponent.fontSize = 32;
        textComponent.fontWeight = FontWeight.Bold;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Right;

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(500f, 50f);

        return textComponent;
    }
}
