using UnityEngine;

/// <summary>玩家设置（PlayerPrefs 持久化）。鼠标灵敏度 / 反转 Y / FOV / 移速 / 亮度。</summary>
public static class GameSettings
{
    const string KeySens = "outdoor.sensitivity";
    const string KeyInvert = "outdoor.invertY";
    const string KeyFov = "outdoor.fov";
    const string KeySpeed = "outdoor.moveSpeed";
    const string KeyBright = "outdoor.brightness";

    public const float DefaultSens = 100f;
    public const bool DefaultInvert = false;
    public const float DefaultFov = 60f;
    public const float DefaultSpeed = 1.0f;
    public const float DefaultBright = 1.0f;

    /// <summary>灵敏度刻度 1~100。100 = 原来的 0.02 倍率（最低档变成最高档）。</summary>
    public static float Sensitivity = DefaultSens;

    /// <summary>实际倍率：1→0.0002，100→0.02。</summary>
    public static float SensitivityMultiplier => Mathf.Clamp(Sensitivity, 1f, 100f) * 0.0002f;
    public static bool InvertY = DefaultInvert;
    public static float Fov = DefaultFov;
    public static float MoveSpeed = DefaultSpeed;
    public static float Brightness = DefaultBright;

    private static bool loaded;

    public static void Load()
    {
        if (loaded) return;
        loaded = true;

        float rawSens = PlayerPrefs.GetFloat(KeySens, DefaultSens);
        if (rawSens < 1f) rawSens = DefaultSens; // 旧版本存的是 0.02~2.0 的倍率，迁移到新刻度
        Sensitivity = Mathf.Clamp(rawSens, 1f, 100f);
        InvertY = PlayerPrefs.GetInt(KeyInvert, DefaultInvert ? 1 : 0) == 1;
        Fov = PlayerPrefs.GetFloat(KeyFov, DefaultFov);
        MoveSpeed = PlayerPrefs.GetFloat(KeySpeed, DefaultSpeed);
        Brightness = PlayerPrefs.GetFloat(KeyBright, DefaultBright);
    }

    public static void Save()
    {
        PlayerPrefs.SetFloat(KeySens, Sensitivity);
        PlayerPrefs.SetInt(KeyInvert, InvertY ? 1 : 0);
        PlayerPrefs.SetFloat(KeyFov, Fov);
        PlayerPrefs.SetFloat(KeySpeed, MoveSpeed);
        PlayerPrefs.SetFloat(KeyBright, Brightness);
        PlayerPrefs.Save();
    }

    public static void ResetDefaults()
    {
        Sensitivity = DefaultSens;
        InvertY = DefaultInvert;
        Fov = DefaultFov;
        MoveSpeed = DefaultSpeed;
        Brightness = DefaultBright;
        Save();
    }
}
