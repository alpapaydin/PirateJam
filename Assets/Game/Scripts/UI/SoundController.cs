using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)]
    public float volume = 1f;
    [Range(0.1f, 3f)]
    public float pitch = 1f;
    public bool loop = false;

    [HideInInspector]
    public AudioSource source;
}

public class SoundController : MonoBehaviour
{
    public static SoundController Instance { get; private set; }

    [SerializeField]
    private Sound[] sounds;

    [SerializeField]
    private int poolSize = 10;

    private Queue<AudioSource> audioSourcePool;
    private List<AudioSource> activeAudioSources;

    [SerializeField]
    private float masterVolume = 1f;

    [SerializeField]
    private float sfxVolume = 1f;

    [SerializeField]
    private float uiVolume = 1f;

    private Dictionary<string, Sound> soundDictionary;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSoundSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSoundSystem()
    {
        soundDictionary = new Dictionary<string, Sound>();
        audioSourcePool = new Queue<AudioSource>();
        activeAudioSources = new List<AudioSource>();
        foreach (Sound sound in sounds)
        {
            if (soundDictionary.ContainsKey(sound.name))
            {
                Debug.LogWarning($"Duplicate sound name found: {sound.name}");
                continue;
            }
            soundDictionary.Add(sound.name, sound);
        }
        for (int i = 0; i < poolSize; i++)
        {
            CreateNewAudioSource();
        }
    }

    private void CreateNewAudioSource()
    {
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSourcePool.Enqueue(audioSource);
    }

    private AudioSource GetAudioSource()
    {
        if (audioSourcePool.Count == 0)
        {
            CreateNewAudioSource();
        }

        AudioSource source = audioSourcePool.Dequeue();
        activeAudioSources.Add(source);
        return source;
    }

    private void ReleaseAudioSource(AudioSource source)
    {
        source.Stop();
        source.clip = null;
        activeAudioSources.Remove(source);
        audioSourcePool.Enqueue(source);
    }

    public void PlaySound(string soundName, bool isUI = false)
    {
        if (!soundDictionary.TryGetValue(soundName, out Sound sound))
        {
            Debug.LogWarning($"Sound not found: {soundName}");
            return;
        }

        AudioSource audioSource = GetAudioSource();
        audioSource.clip = sound.clip;
        audioSource.volume = sound.volume * (isUI ? uiVolume : sfxVolume) * masterVolume;
        audioSource.pitch = sound.pitch;
        audioSource.loop = sound.loop;

        audioSource.Play();

        if (!sound.loop)
        {
            StartCoroutine(ReleaseWhenFinished(audioSource));
        }
    }

    public void StopSound(string soundName)
    {
        foreach (AudioSource source in activeAudioSources)
        {
            if (source.clip != null && source.clip.name == soundName)
            {
                ReleaseAudioSource(source);
                break;
            }
        }
    }

    public void StopAllSounds()
    {
        foreach (AudioSource source in activeAudioSources.ToArray())
        {
            ReleaseAudioSource(source);
        }
    }

    private IEnumerator ReleaseWhenFinished(AudioSource audioSource)
    {
        yield return new WaitUntil(() => !audioSource.isPlaying);
        ReleaseAudioSource(audioSource);
    }
}