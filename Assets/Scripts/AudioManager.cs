using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Clips")]
    public AudioClip battleStart;
    public AudioClip basicAttack;
    public AudioClip skill;
    public AudioClip ultimate;
    public AudioClip gachaPull;
    public AudioClip bossIntro;

    [Header("Audio Sources")]
    [Tooltip("Optional dedicated music source. If null, masterSource is used.")]
    public AudioSource musicSource;
    [Tooltip("Optional dedicated SFX source. If null, masterSource is used.")]
    public AudioSource sfxSource;

    private AudioSource masterSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure we have at least one AudioSource available
        masterSource = GetComponent<AudioSource>();
        if (masterSource == null)
            masterSource = gameObject.AddComponent<AudioSource>();

        if (musicSource == null)
            musicSource = masterSource;
        if (sfxSource == null)
            sfxSource = masterSource;

        // Apply any settings that might already be loaded
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ApplySettings();
        }
    }

    public void PlayClip(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void Play(string key)
    {
        switch (key)
        {
            case "battle_start":
                PlayClip(battleStart);
                break;
            case "basic":
                PlayClip(basicAttack);
                break;
            case "skill":
                PlayClip(skill);
                break;
            case "ultimate":
                PlayClip(ultimate);
                break;
            case "gacha":
                PlayClip(gachaPull);
                break;
            case "boss_intro":
                PlayClip(bossIntro);
                break;
        }
    }

    /// <summary>
    /// Called by SettingsManager to update volumes.
    /// </summary>
    public void SetVolumes(float master, float music, float sfx)
    {
        master = Mathf.Clamp01(master);
        music = Mathf.Clamp01(music);
        sfx = Mathf.Clamp01(sfx);

        if (masterSource != null)
            masterSource.volume = master;

        if (musicSource != null)
            musicSource.volume = master * music;

        if (sfxSource != null)
            sfxSource.volume = master * sfx;
    }
}

