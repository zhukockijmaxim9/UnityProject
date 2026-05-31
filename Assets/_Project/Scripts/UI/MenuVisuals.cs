using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MenuVisuals
{
    public static void ApplyMainMenu(Canvas canvas)
    {
        if (canvas == null)
        {
            return;
        }

        StyleBackground(canvas.transform, 0.92f);
        StyleMenuPanel(FindDeepChild(canvas.transform, "MainMenu"), null, 0.28f);
        StyleSettingsPanel(FindDeepChild(canvas.transform, "SettingPanel"));
    }

    public static void ApplyGameplayMenus(Transform pausePanel, Transform settingsPanel)
    {
        StyleMenuPanel(pausePanel, "ПАУЗА", 0.45f);
        StyleSettingsPanel(settingsPanel);
    }

    public static Canvas FindCanvasInScene(Scene scene)
    {
#if UNITY_2023_1_OR_NEWER
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);
#endif
        foreach (Canvas canvas in canvases)
        {
            if (canvas.gameObject.scene == scene)
            {
                return canvas;
            }
        }

        return null;
    }

    private static void StyleBackground(Transform root, float brightness)
    {
        Transform background = FindDeepChild(root, "Background");
        if (background == null)
        {
            return;
        }

        Image image = background.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(brightness, brightness, brightness, 1f);
        }
    }

    private static void StyleMenuPanel(Transform panel, string title, float overlayAlpha)
    {
        if (panel == null)
        {
            return;
        }

        MenuTheme.StyleOverlay(panel.GetComponent<Image>(), overlayAlpha);
        MenuTheme.CreateTitleInButtonContainer(panel, title);
        RemoveCardFrame(panel);

        Transform container = FindDeepChild(panel, "ButtonContaier");
        if (container != null)
        {
            VerticalLayoutGroup layout = container.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = 16f;
                layout.padding = new RectOffset(0, 0, 0, 0);
                layout.childAlignment = TextAnchor.MiddleCenter;
            }
        }

        StyleButtons(panel);
    }

    private static void StyleSettingsPanel(Transform panel)
    {
        if (panel == null)
        {
            return;
        }

        MenuTheme.StyleOverlay(panel.GetComponent<Image>(), 0.42f);
        MenuTheme.CreatePanelTitle(panel, "НАСТРОЙКИ");
        RemoveCardFrame(panel);
        EnsureSettingsLabels(panel);
        LayoutSettingsPanel(panel);
        StyleButtons(panel);
        StyleSliders(panel);
        StyleDropdowns(panel);
    }

    private static void LayoutSettingsPanel(Transform panel)
    {
        SetCenteredElement(panel, "Volume", new Vector2(0f, 130f), new Vector2(800f, 40f));
        SetCenteredElement(panel, "VolumeSlider", new Vector2(0f, 85f), new Vector2(800f, 50f));
        SetCenteredElement(panel, "ResolutionLabel", new Vector2(0f, 35f), new Vector2(800f, 40f));
        SetCenteredElement(panel, "ResolutionDropdown", new Vector2(0f, -15f), new Vector2(800f, 50f));
        SetCenteredElement(panel, "BackButton", new Vector2(0f, -206f), new Vector2(800f, 50f));
    }

    private static void SetCenteredElement(Transform panel, string childName, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        Transform child = panel.Find(childName);
        if (child == null)
        {
            child = FindDeepChild(panel, childName);
        }

        if (child == null)
        {
            return;
        }

        RectTransform rect = child.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void RemoveCardFrame(Transform panel)
    {
        if (panel == null)
        {
            return;
        }

        Transform existing = panel.Find("MenuCardFrame");
        if (existing != null)
        {
            Object.Destroy(existing.gameObject);
        }
    }

    private static void EnsureSettingsLabels(Transform panel)
    {
        TextMeshProUGUI volumeLabel = FindChildComponent<TextMeshProUGUI>(panel, "Volume");
        if (volumeLabel != null)
        {
            volumeLabel.text = "Громкость";
            MenuTheme.StyleLabel(volumeLabel);
        }

        Transform dropdown = FindDeepChild(panel, "ResolutionDropdown");
        if (dropdown == null)
        {
            return;
        }

        Transform labelTransform = panel.Find("ResolutionLabel");
        TextMeshProUGUI resolutionLabel;
        if (labelTransform == null)
        {
            GameObject labelObject = new GameObject("ResolutionLabel");
            labelObject.transform.SetParent(panel, false);
            resolutionLabel = labelObject.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            resolutionLabel = labelTransform.GetComponent<TextMeshProUGUI>();
        }

        if (resolutionLabel != null)
        {
            resolutionLabel.text = "Разрешение экрана";
            resolutionLabel.raycastTarget = false;
            MenuTheme.StyleLabel(resolutionLabel);
        }
    }

    private static void StyleButtons(Transform panel)
    {
        Button[] buttons = panel.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (IsDropdownPart(button.transform))
            {
                continue;
            }

            ApplyButtonLabel(button);
            MenuTheme.StyleButton(button, IsDangerButton(button));
        }
    }

    private static void StyleSliders(Transform panel)
    {
        Slider[] sliders = panel.GetComponentsInChildren<Slider>(true);
        foreach (Slider slider in sliders)
        {
            MenuTheme.StyleSlider(slider);
        }
    }

    private static void StyleDropdowns(Transform panel)
    {
        TMP_Dropdown[] dropdowns = panel.GetComponentsInChildren<TMP_Dropdown>(true);
        foreach (TMP_Dropdown dropdown in dropdowns)
        {
            Image image = dropdown.GetComponent<Image>();
            if (image != null)
            {
                image.color = MenuTheme.ButtonNormal;
            }

            LayoutElement layout = dropdown.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = dropdown.gameObject.AddComponent<LayoutElement>();
            }

            layout.preferredHeight = MenuTheme.ButtonHeight;

            if (dropdown.captionText != null)
            {
                dropdown.captionText.color = Color.white;
                dropdown.captionText.fontSize = 24f;
            }
        }
    }

    private static void ApplyButtonLabel(Button button)
    {
        Transform panel = GetParentPanel(button.transform);
        string panelName = panel != null ? panel.name : string.Empty;
        string buttonName = button.gameObject.name;

        string label = buttonName switch
        {
            "Button_Play" => "Начать выживание",
            "ButtonResume" => "Продолжить",
            "Button_Settings" => "Настройки",
            "BackButton" => "Назад",
            "Button_Exit" when panelName == "PauseMenu" => "В главное меню",
            "Button_Exit" => "Выход",
            _ => null
        };

        if (label == null)
        {
            return;
        }

        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
        {
            text.text = label;
        }
    }

    private static bool IsDangerButton(Button button)
    {
        if (button.gameObject.name != "Button_Exit")
        {
            return false;
        }

        Transform panel = GetParentPanel(button.transform);
        return panel == null || panel.name == "MainMenu";
    }

    private static bool IsDropdownPart(Transform node)
    {
        Transform current = node;
        while (current != null)
        {
            if (current.name is "Template" or "Dropdown List" or "Viewport" or "Content")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static Transform GetParentPanel(Transform node)
    {
        Transform current = node;
        while (current != null)
        {
            if (current.name is "MainMenu" or "PauseMenu" or "SettingPanel")
            {
                return current;
            }

            current = current.parent;
        }

        return null;
    }

    private static T FindChildComponent<T>(Transform parent, string childName) where T : Component
    {
        Transform child = FindDeepChild(parent, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.name == childName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindDeepChild(parent.GetChild(i), childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
