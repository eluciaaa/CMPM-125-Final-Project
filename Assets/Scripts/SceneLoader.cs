using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SceneLoader : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject creditsPanel;
    public GameObject controlsPanel;
    public GameObject settingsPanel;

    public AudioMixer audioMixer;
    public Slider musicSlider;
    public Slider sfxSlider;

    public Toggle particleToggle;
    public Toggle fullscreenToggle;

    void Start()
    {
        bool fullscreen =
            PlayerPrefs.GetInt("Fullscreen", 1) == 1;

        Screen.fullScreen = fullscreen;

        float music =
            PlayerPrefs.GetFloat("MusicVolume", 1f);

        float sfx =
            PlayerPrefs.GetFloat("SFXVolume", 1f);

        if (musicSlider != null)
            musicSlider.value = music;

        if (sfxSlider != null)
            sfxSlider.value = sfx;

        SetMusicVolume(music);
        SetSFXVolume(sfx);
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

    public void SetMusicVolume(float volume)
    {
        audioMixer.SetFloat(
            "MusicVolume",
            Mathf.Lerp(-80f, 0f, volume)
        );

        PlayerPrefs.SetFloat("MusicVolume", volume);
        PlayerPrefs.Save();
    }


    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat(
            "SFXVolume",
            Mathf.Lerp(-80f, 0f, volume)
        );

        PlayerPrefs.SetFloat("SFXVolume", volume);
        PlayerPrefs.Save();
    }


    public void ToggleParticles(bool enabled)
    {
        PlayerPrefs.SetInt(
            "ParticlesEnabled",
            enabled ? 1 : 0
        );

        PlayerPrefs.Save();
    }


    public void ToggleFullscreen(bool enabled)
    {
        Screen.fullScreen = enabled;
        PlayerPrefs.SetInt("Fullscreen", enabled ? 1 : 0);
    }
}