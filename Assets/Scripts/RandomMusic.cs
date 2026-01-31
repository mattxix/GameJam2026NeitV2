using UnityEngine;

public class RandomMusic : MonoBehaviour
{
    private AudioSource musicSource;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        musicSource = GetComponent<AudioSource>();
        musicSource.time = Random.Range(0f, musicSource.clip.length);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
