using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    public AudioSource audioSource;

    public AudioClip lakeMusic;
    public AudioClip volcanoMusic;
    public AudioClip cloudsMusic;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public void PlayLakeMusic()
    {
        PlayMusic(lakeMusic);
    }


    public void PlayVolcanoMusic()
    {
        PlayMusic(volcanoMusic);
    }


    public void PlayCloudsMusic()
    {
        PlayMusic(cloudsMusic);
    }


    void PlayMusic(AudioClip clip)
    {
        if (audioSource.clip == clip)
            return;

        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
    }
}