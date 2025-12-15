using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioClip battleStart;
    public AudioClip basicAttack;
    public AudioClip skill;
    public AudioClip ultimate;
    public AudioClip gachaPull;
    public AudioClip bossIntro;

    private AudioSource source;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        source = gameObject.AddComponent<AudioSource>();
    }

    public void PlayClip(AudioClip clip)
    {
        if (clip == null || source == null) return;
        source.PlayOneShot(clip);
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
}

