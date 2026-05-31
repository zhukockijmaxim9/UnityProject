using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class MenuTheme
{
    public static readonly Color Accent = new Color(0.35f, 0.92f, 1f, 1f);
    public static readonly Color AccentSoft = new Color(0.35f, 0.92f, 1f, 0.35f);
    public static readonly Color Title = new Color(0.92f, 0.98f, 1f, 1f);
    public static readonly Color Body = new Color(0.82f, 0.88f, 0.94f, 1f);
    public static readonly Color ButtonNormal = new Color(0.1f, 0.14f, 0.22f, 0.96f);
    public static readonly Color ButtonPressed = new Color(0.08f, 0.55f, 0.68f, 1f);
    public static readonly Color ButtonDisabled = new Color(0.12f, 0.12f, 0.14f, 0.55f);
    public static readonly Color Danger = new Color(1f, 0.42f, 0.38f, 1f);
    public static readonly Color PanelGlow = new Color(0.08f, 0.12f, 0.2f, 0.45f);

    public const float TitleSize = 48f;
    public const float ButtonTextSize = 30f;
    public const float LabelSize = 26f;
    public const float ButtonHeight = 64f;

    public static void StyleOverlay(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        image.color = new Color(0.05f, 0.08f, 0.14f, alpha);
        image.raycastTarget = true;
    }

    public static void StyleTitle(TextMeshProUGUI text)
    {
        if (text == null)
        {
            return;
        }

        EnsureFont(text);
        text.fontSize = TitleSize;
        text.fontWeight = FontWeight.Bold;
        text.color = Title;
        text.alignment = TextAlignmentOptions.Center;
        text.characterSpacing = 3f;
    }

    public static void StyleLabel(TextMeshProUGUI text)
    {
        if (text == null)
        {
            return;
        }

        EnsureFont(text);
        text.fontSize = LabelSize;
        text.fontWeight = FontWeight.Bold;
        text.color = Body;
        text.alignment = TextAlignmentOptions.Left;
    }

    public static void StyleButton(Button button, bool isDanger = false)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.targetGraphic as Image;
        if (image == null)
        {
            image = button.GetComponent<Image>();
            button.targetGraphic = image;
        }

        if (image != null)
        {
            image.color = isDanger
                ? new Color(0.28f, 0.1f, 0.12f, 0.96f)
                : ButtonNormal;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = isDanger
            ? new Color(1f, 0.55f, 0.5f, 1f)
            : new Color(0.55f, 0.95f, 1f, 1f);
        colors.pressedColor = isDanger ? new Color(0.9f, 0.25f, 0.22f, 1f) : ButtonPressed;
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = ButtonDisabled;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.12f;
        button.colors = colors;
        button.transition = Selectable.Transition.ColorTint;

        LayoutElement layout = button.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = button.gameObject.AddComponent<LayoutElement>();
        }

        layout.preferredHeight = ButtonHeight;
        layout.minHeight = ButtonHeight;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            EnsureFont(label);
            label.fontSize = ButtonTextSize;
            label.fontWeight = FontWeight.Bold;
            label.color = isDanger ? Danger : Color.white;
            label.alignment = TextAlignmentOptions.Center;
        }

        if (button.GetComponent<MenuButtonHover>() == null)
        {
            button.gameObject.AddComponent<MenuButtonHover>();
        }
    }

    public static void StyleSlider(Slider slider)
    {
        if (slider == null)
        {
            return;
        }

        if (slider.fillRect != null)
        {
            Image fill = slider.fillRect.GetComponent<Image>();
            if (fill != null)
            {
                fill.color = Accent;
            }
        }

        if (slider.handleRect != null)
        {
            Image handle = slider.handleRect.GetComponent<Image>();
            if (handle != null)
            {
                handle.color = Title;
            }
        }
    }

    public static TextMeshProUGUI CreatePanelTitle(Transform parent, string title)
    {
        RemoveTitleBlock(parent);

        if (string.IsNullOrEmpty(title))
        {
            return null;
        }

        GameObject block = new GameObject("MenuTitleBlock");
        block.transform.SetParent(parent, false);
        block.transform.SetAsFirstSibling();

        RectTransform rect = block.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -48f);
        rect.sizeDelta = new Vector2(760f, 64f);

        TextMeshProUGUI titleText = CreateText(block.transform, "MenuTitle", title);
        StretchTextToParent(titleText);
        StyleTitle(titleText);
        return titleText;
    }

    public static TextMeshProUGUI CreateTitleInButtonContainer(Transform panel, string title)
    {
        RemoveTitleBlock(panel);

        if (panel == null || string.IsNullOrEmpty(title))
        {
            return null;
        }

        Transform container = panel.Find("ButtonContaier");
        if (container == null)
        {
            return CreatePanelTitle(panel, title);
        }

        RemoveTitleBlock(container);

        GameObject block = new GameObject("MenuTitleBlock");
        block.transform.SetParent(container, false);
        block.transform.SetAsFirstSibling();

        LayoutElement layout = block.AddComponent<LayoutElement>();
        layout.preferredHeight = 56f;
        layout.minHeight = 56f;

        TextMeshProUGUI titleText = CreateText(block.transform, "MenuTitle", title);
        StretchTextToParent(titleText);
        StyleTitle(titleText);
        return titleText;
    }

    public static void RemoveTitleBlock(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        Transform existing = parent.Find("MenuTitleBlock");
        if (existing != null)
        {
            Object.Destroy(existing.gameObject);
        }
    }

    private static void StretchTextToParent(TextMeshProUGUI text)
    {
        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static TextMeshProUGUI CreateText(Transform parent, string objectName, string value)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(800f, 40f);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.raycastTarget = false;
        EnsureFont(text);
        return text;
    }

    public static void EnsureFont(TextMeshProUGUI text)
    {
        if (text == null || text.font != null)
        {
            return;
        }

        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }
    }
}
