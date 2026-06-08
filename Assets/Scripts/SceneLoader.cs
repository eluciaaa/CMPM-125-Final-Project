using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject creditsPanel;
    public GameObject controlsPanel;
    public GameObject settingsPanel;

    public Toggle particleToggle;
    public Toggle fullscreenToggle;

    void Start()
    {
        bool fullscreen =
            PlayerPrefs.GetInt("Fullscreen", 1) == 1;

        Screen.fullScreen = fullscreen;
    }
    
    public void StartGame()
    {
        SceneManager.LoadScene("Game");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }


    public void OpenCredits()
    {
        mainMenuPanel.SetActive(false);
        creditsPanel.SetActive(true);
    }

    public void OpenControls()
    {
        mainMenuPanel.SetActive(false);
        controlsPanel.SetActive(true);
    }

    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }


    public void BackToMenuPanel()
    {
        creditsPanel.SetActive(false);
        controlsPanel.SetActive(false);
        settingsPanel.SetActive(false);

        mainMenuPanel.SetActive(true);
    }


    // =========================
    // SETTINGS FUNCTIONS
    // =========================


    public void SetMusicVolume(float volume)
    {
        // Uses Unity's global audio volume
        AudioListener.volume = volume;
    }


    public void SetSFXVolume(float volume)
    {
        // Placeholder for future SFX mixer
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }


    public void ToggleParticles(bool enabled)
    {
        PlayerPrefs.SetInt("ParticlesEnabled", enabled ? 1 : 0);
    }


    public void ToggleFullscreen(bool enabled)
    {
        Screen.fullScreen = enabled;
        PlayerPrefs.SetInt("Fullscreen", enabled ? 1 : 0);
    }
}