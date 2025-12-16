using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private const string PlayerPrefsKey = "Soundborn_Settings";

    [Header("Runtime Settings")]
    public SettingsData current = new SettingsData();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
        ApplySettings();
    }

    public void LoadSettings()
    {
        if (!PlayerPrefs.HasKey(PlayerPrefsKey))
        {
            current = new SettingsData();
            return;
        }

        string json = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                current = JsonUtility.FromJson<SettingsData>(json) ?? new SettingsData();
            }
            catch
            {
                current = new SettingsData();
            }
        }
        else
        {
            current = new SettingsData();
        }
    }

    public void SaveSettings()
    {
        if (current == null)
            current = new SettingsData();

        string json = JsonUtility.ToJson(current);
        PlayerPrefs.SetString(PlayerPrefsKey, json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Apply settings to global systems (audio, battle speed, etc.).
    /// </summary>
    public void ApplySettings()
    {
        // Audio routing
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetVolumes(
                current.masterVolume,
                current.musicVolume,
                current.sfxVolume
            );
        }

        // Battle speed: use Time.timeScale as a simple global multiplier.
        Time.timeScale = Mathf.Clamp(current.battleSpeed, 0.5f, 2f);
    }

    #region Convenience API for UI

    public void SetMasterVolume(float value)
    {
        current.masterVolume = Mathf.Clamp01(value);
        ApplySettings();
        SaveSettings();
    }

    public void SetMusicVolume(float value)
    {
        current.musicVolume = Mathf.Clamp01(value);
        ApplySettings();
        SaveSettings();
    }

    public void SetSfxVolume(float value)
    {
        current.sfxVolume = Mathf.Clamp01(value);
        ApplySettings();
        SaveSettings();
    }

    public void SetBattleSpeed(float value)
    {
        current.battleSpeed = Mathf.Clamp(value, 0.5f, 2f);
        ApplySettings();
        SaveSettings();
    }

    public void SetVibrationEnabled(bool enabled)
    {
        current.vibrationEnabled = enabled;
        SaveSettings();
    }

    #endregion
}


