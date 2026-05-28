using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // НУЖНО
using UnityEngine.EventSystems;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public GameObject settingsPanel;
    
    // СЮДА В ИНСПЕКТОРЕ ПЕРЕТАЩИ ОБЪЕКТ "PLAYER" (на котором висит Player Input)
    public PlayerInput playerInput; 
    
    public static bool isPaused = false;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Resume()
    {   
    pauseMenuUI.SetActive(false);
    if (settingsPanel != null) settingsPanel.SetActive(false);
    
    Time.timeScale = 1f;
    isPaused = false;

    if (EventSystem.current != null)
        EventSystem.current.SetSelectedGameObject(null);

    if (playerInput != null) 
    {
        playerInput.enabled = true;
        // ЖЕСТКО ГОВОРИМ: ПЕРЕКЛЮЧИСЬ НА ИГРОКА
        playerInput.ActivateInput(); 
        playerInput.SwitchCurrentActionMap("Player"); // Замени "Player" на имя своей карты в Action Asset, если оно другое
    }

    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        // ВЫКЛЮЧАЕМ ВВОД ИГРОКА (Чтобы он не прятал курсор!)
        if (playerInput != null) playerInput.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LoadMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}