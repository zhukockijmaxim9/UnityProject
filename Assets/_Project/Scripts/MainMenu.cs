using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    private const string VolumePrefKey = "MasterVolume";

    [Header("Окна меню")]
    public GameObject mainMenuWindow;
    public GameObject settingsPanel;

    [Header("Настройки")]
    public TMP_Dropdown resolutionDropdown;
    public Slider volumeSlider;

    private GameObject mainMenuPanel;
    private Resolution[] resolutions;

    private void Awake()
    {
        if (mainMenuWindow != null)
        {
            mainMenuPanel = mainMenuWindow.name.Contains("ButtonContaier")
                ? mainMenuWindow.transform.parent.gameObject
                : mainMenuWindow;
        }

        if (volumeSlider == null)
        {
            volumeSlider = GetComponentInChildren<Slider>(true);
        }

        Canvas canvas = FindMenuCanvas();
        if (canvas != null)
        {
            MenuVisuals.ApplyMainMenu(canvas);
        }
    }

    private Canvas FindMenuCanvas()
    {
        if (mainMenuWindow != null)
        {
            Canvas canvas = mainMenuWindow.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                return canvas;
            }
        }

        if (settingsPanel != null)
        {
            Canvas canvas = settingsPanel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                return canvas;
            }
        }

        return MenuVisuals.FindCanvasInScene(gameObject.scene);
    }

    private void Start()
    {
        InitializeVolume();
        InitializeResolutionDropdown();
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void OpenSettings()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }
        else if (mainMenuWindow != null)
        {
            mainMenuWindow.SetActive(false);
        }

        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
        else if (mainMenuWindow != null)
        {
            mainMenuWindow.SetActive(true);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(VolumePrefKey, volume);
        PlayerPrefs.Save();
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    private void InitializeVolume()
    {
        float volume = PlayerPrefs.GetFloat(VolumePrefKey, 0.75f);
        AudioListener.volume = volume;

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(volume);
        }
    }

    private void InitializeResolutionDropdown()
    {
        if (resolutionDropdown == null)
        {
            return;
        }

        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }
}
