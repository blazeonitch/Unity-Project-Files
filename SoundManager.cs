using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource footstepsSource;
    public AudioSource sfxSource;

    [Header("Background Sounds")]
    public AudioClip backgroundMusic;

    [Header("Footstep Sounds")]
    public AudioClip[] concreteFootsteps;
    public AudioClip[] woodFootsteps;
    public AudioClip[] grassFootsteps;
    public AudioClip defaultFootstep;

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

    public void PlayMusic()
    {
        if (!musicSource.isPlaying)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PlayFootstep(string surfaceTag)
    {
        AudioClip[] footstepArray = surfaceTag switch
        {
            "Concrete" => concreteFootsteps,
            "Wood" => woodFootsteps,
            "Grass" => grassFootsteps,
            _ => null
        };

        if (footstepArray != null && footstepArray.Length > 0)
        {
            int randomIndex = Random.Range(0, footstepArray.Length);
            footstepsSource.clip = footstepArray[randomIndex];
            footstepsSource.Play();
        }
        else
        {
            footstepsSource.PlayOneShot(defaultFootstep);
        }
    }

}
