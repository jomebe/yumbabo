using UnityEngine;

public static class GameSettings
{
    private const string VolumeKey = "LR_Volume";
    private const string SensitivityKey = "LR_Sensitivity";

    private static bool initialized;
    private static float volume = 1f;
    private static float sensitivity = 1f;

    public static float Volume
    {
        get
        {
            EnsureInitialized();
            return volume;
        }
    }

    public static float SensitivityMultiplier
    {
        get
        {
            EnsureInitialized();
            return sensitivity;
        }
    }

    public static void SetVolume(float value)
    {
        EnsureInitialized();
        volume = Mathf.Clamp01(value);
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(VolumeKey, volume);
        PlayerPrefs.Save();
    }

    public static void SetSensitivity(float value)
    {
        EnsureInitialized();
        sensitivity = Mathf.Clamp(value, 0.1f, 5f);
        PlayerPrefs.SetFloat(SensitivityKey, sensitivity);
        PlayerPrefs.Save();
    }

    public static void Apply()
    {
        EnsureInitialized();
        AudioListener.volume = volume;
    }

    private static void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        sensitivity = PlayerPrefs.GetFloat(SensitivityKey, 1f);
        AudioListener.volume = volume;
        initialized = true;
    }
}
