using System;

[Serializable]
public class SettingsData
{
    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float sfxVolume = 1f;

    /// <summary>
    /// Global battle speed multiplier. 1 = normal speed.
    /// </summary>
    public float battleSpeed = 1f;

    public bool vibrationEnabled = true;
}


