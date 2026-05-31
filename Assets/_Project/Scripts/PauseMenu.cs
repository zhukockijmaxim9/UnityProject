using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    private const string VolumePrefKey = "MasterVolume";

    public GameObject pauseMenuUI;
    public GameObject settingsPanel;
    public PlayerInput playerInput;
    public TMP_Dropdown resolutionDropdown;
    public Slider volumeSlider;

    public static bool isPaused = false;

    private Resolution[] resolutions;

    private void Awake()
    {
        if (volumeSlider == null)
        {
            volumeSlider = GetComponentInChildren<Slider>(true);
        }

        MenuVisuals.ApplyGameplayMenus(
            pauseMenuUI != null ? pauseMenuUI.transform : null,
            settingsPanel != null ? settingsPanel.transform : null);
    }

    private void Start()
    {
        InitializeResolutionDropdown();
        InitializeVolume();
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        if (GameManager.IsGameOver)
        {
            return;
        }

        if (isPaused)
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
            }
            else
            {
                Resume();
            }
        }
        else
        {
            Pause();
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;

        StartCoroutine(RestoreGameplayInputNextFrame());
    }

    public void Pause()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OpenSettings()
    {
        if (settingsPanel == null)
        {
            return;
        }

        pauseMenuUI.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (pauseMenuUI != null && isPaused)
        {
            pauseMenuUI.SetActive(true);
        }
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(VolumePrefKey, volume);
        PlayerPrefs.Save();
    }

    public void SetResolution(int resolutionIndex)
    {
        if (resolutions == null || resolutionIndex < 0 || resolutionIndex >= resolutions.Length)
        {
            return;
        }

        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void LoadMenu()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    public void ForceClose()
    {
        isPaused = false;

        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
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

        var options = new List<string>();
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

    private IEnumerator RestoreGameplayInputNextFrame()
    {
        yield return null;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
