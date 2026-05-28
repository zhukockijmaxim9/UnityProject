using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro; 
using System.Collections.Generic; 

public class MainMenu : MonoBehaviour
{
    [Header("Окна меню")]
    public GameObject mainMenuWindow; 
    public GameObject settingsPanel;

    [Header("Настройки разрешения")]
    public TMP_Dropdown resolutionDropdown; // Теперь Unity поймет, что это такое
    Resolution[] resolutions;

    void Start()
    {
        // Получаем все доступные разрешения монитора
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

    public void PlayGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void OpenSettings()
    {
        mainMenuWindow.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        mainMenuWindow.SetActive(true);
        settingsPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Debug.Log("ВЫХОД");
        Application.Quit();
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume; 
        Debug.Log("Громкость: " + volume);
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }
}