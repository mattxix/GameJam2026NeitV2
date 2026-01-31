using UnityEngine;

public class SFXService : MonoBehaviour
{
    private AudioSource sfxSource;
    public AudioClip glass;
    public AudioClip pouring;
    public AudioClip grab;
    public AudioClip ice;
    public AudioClip poison;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sfxSource = GetComponent<AudioSource>();
    }

    public void Glass(float vol = 1.0f)
    {
        sfxSource.clip = glass;
        sfxSource.volume = vol;
        sfxSource.Play();
    }
    public void Ice(float vol = 1.0f)
    {
        sfxSource.clip = ice;
        sfxSource.volume = vol;
        sfxSource.Play();
    }
    public void Pouring(float vol = 1.0f)
    {
        sfxSource.clip = pouring;
        sfxSource.volume = vol;
        sfxSource.Play();
    }
    public void Grab(float vol = 1.0f)
    {
        sfxSource.clip = grab;
        sfxSource.volume = vol;
        sfxSource.Play();
    }
    public void Poison(float vol = 1.0f)
    {
        sfxSource.clip = poison;
        sfxSource.volume = vol;
        sfxSource.Play();
    }
}
