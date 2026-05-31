using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeathScreen : MonoBehaviour
{
    private Canvas canvas;
    private GameObject panelRoot;
    private TextMeshProUGUI statsText;

    public event Action RestartRequested;
    public event Action MainMenuRequested;

    public void EnsureCreated()
    {
        if (panelRoot != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("DeathScreenCanvas");
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        panelRoot = new GameObject("DeathPanel");
        panelRoot.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelRoot.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image overlay = panelRoot.AddComponent<Image>();
        MenuTheme.StyleOverlay(overlay, 0.78f);

        GameObject card = new GameObject("MenuCardFrame");
        card.transform.SetParent(panelRoot.transform, false);
        RectTransform cardRect = card.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(720f, 560f);
        cardRect.anchoredPosition = Vector2.zero;

        Image cardImage = card.AddComponent<Image>();
        cardImage.color = MenuTheme.PanelGlow;
        cardImage.raycastTarget = false;

        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = MenuTheme.AccentSoft;
        outline.effectDistance = new Vector2(2f, -2f);

        GameObject content = new GameObject("Content");
        content.transform.SetParent(card.transform, false);

        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(40f, 40f);
        contentRect.offsetMax = new Vector2(-40f, -40f);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 20f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI title = MenuTheme.CreateText(content.transform, "Title", "ВЫ ПОГИБЛИ");
        MenuTheme.StyleTitle(title);
        title.color = MenuTheme.Danger;

        LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 72f;

        statsText = MenuTheme.CreateText(content.transform, "Stats", string.Empty);
        statsText.fontSize = 32f;
        statsText.fontWeight = FontWeight.Medium;
        statsText.color = MenuTheme.Body;
        statsText.alignment = TextAlignmentOptions.Center;
        statsText.lineSpacing = 10f;

        LayoutElement statsLayout = statsText.gameObject.AddComponent<LayoutElement>();
        statsLayout.preferredHeight = 170f;

        CreateButton(content.transform, "Заново", false, () => RestartRequested?.Invoke());
        CreateButton(content.transform, "В меню", true, () => MainMenuRequested?.Invoke());

        panelRoot.SetActive(false);
    }

    public void Show(int wave, int kills, int score, int level)
    {
        EnsureCreated();

        statsText.text =
            $"Волна: {Mathf.Max(1, wave)}\n" +
            $"Убийств: {kills}\n" +
            $"Счёт: {score:0000}\n" +
            $"Уровень: {level}";

        panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private static void CreateButton(Transform parent, string label, bool isDanger, Action onClick)
    {
        GameObject buttonObject = new GameObject(label + "Button");
        buttonObject.transform.SetParent(parent, false);

        LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = MenuTheme.ButtonHeight;

        Image image = buttonObject.AddComponent<Image>();
        image.color = MenuTheme.ButtonNormal;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick());
        MenuTheme.StyleButton(button, isDanger);

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
        textComponent.text = label;
        textComponent.fontSize = MenuTheme.ButtonTextSize;
        textComponent.fontWeight = FontWeight.Bold;
        textComponent.color = isDanger ? MenuTheme.Danger : Color.white;
        textComponent.alignment = TextAlignmentOptions.Center;
    }
}
