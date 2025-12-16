using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple settings UI controller to be hooked up to sliders/toggles in the Settings panel.
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [Header("Sliders")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider battleSpeedSlider; // 0.5x - 2x

    [Header("Toggles")]
    public Toggle vibrationToggle;

    private void Start()
    {
        SyncFromSettings();

        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        if (battleSpeedSlider != null)
            battleSpeedSlider.onValueChanged.AddListener(OnBattleSpeedChanged);
        if (vibrationToggle != null)
            vibrationToggle.onValueChanged.AddListener(OnVibrationToggled);
    }

    private void OnEnable()
    {
        SyncFromSettings();
    }

    private void SyncFromSettings()
    {
        var sm = SettingsManager.Instance;
        if (sm == null || sm.current == null)
            return;

        if (masterVolumeSlider != null)
            masterVolumeSlider.value = sm.current.masterVolume;
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = sm.current.musicVolume;
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = sm.current.sfxVolume;
        if (battleSpeedSlider != null)
            battleSpeedSlider.value = sm.current.battleSpeed;
        if (vibrationToggle != null)
            vibrationToggle.isOn = sm.current.vibrationEnabled;
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetMasterVolume(value);
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetMusicVolume(value);
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetSfxVolume(value);
    }

    private void OnBattleSpeedChanged(float value)
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetBattleSpeed(value);
    }

    private void OnVibrationToggled(bool value)
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetVibrationEnabled(value);
    }
}


