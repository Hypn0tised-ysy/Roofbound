using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

[Serializable]
public class CharacterArgumentConfig
{
    public float speed = 6f;
    public float sprintMultiplier = 1.6f;
    public float sprintForwardThreshold = 0.1f;
    public float sprintDuration = 0.25f;
    public float sprintCooldown = 0.8f;
    public float mouseLookSensitivity = 0.15f;
    public float minPitch = -75f;
    public float maxPitch = 75f;
    public float jumpSpeed = 7f;
    public float groundCheckRadius = 0.2f;
    public float gravityAcceleration = 20f;
    public float groundedVerticalVelocity = -2f;
    public int groundMask = -1;
}

[Serializable]
internal class CharacterArgumentRaw
{
    public float speed;
    public float sprintMultiplier;
    public float sprintSpeedMultiplier;
    public float sprintForwardThreshold;
    public float sprintDuration;
    public float sprintCooldown;
    public float mouseLookSensitivity;
    public float minPitch;
    public float maxPitch;
    public float jumpSpeed;
    public float groundCheckRadius;
    public float gravityAcceleration;
    public float groundedVerticalVelocity;
    public int groundMask;
}

public static class readJson
{
    private static readonly string CharacterArgumentPath = Path.Combine(Application.dataPath, "Scripts", "character_argument.json");

    public static bool TryLoadCharacterArgumentConfig(out CharacterArgumentConfig config, out string message)
    {
        config = new CharacterArgumentConfig();
        message = string.Empty;

        if (!File.Exists(CharacterArgumentPath))
        {
            message = "character_argument.json 不存在";
            return false;
        }

        string json = File.ReadAllText(CharacterArgumentPath);
        if (string.IsNullOrWhiteSpace(json))
        {
            message = "character_argument.json 为空";
            return false;
        }

        CharacterArgumentRaw raw;
        try
        {
            raw = JsonUtility.FromJson<CharacterArgumentRaw>(json);
        }
        catch (Exception ex)
        {
            message = $"JSON 解析异常: {ex.Message}";
            return false;
        }

        if (raw == null)
        {
            message = "JSON 解析结果为空";
            return false;
        }

        ApplyFloat(json, "speed", raw.speed, ref config.speed);
        ApplyFloat(json, "sprintForwardThreshold", raw.sprintForwardThreshold, ref config.sprintForwardThreshold);
        ApplyFloat(json, "sprintDuration", raw.sprintDuration, ref config.sprintDuration);
        ApplyFloat(json, "sprintCooldown", raw.sprintCooldown, ref config.sprintCooldown);
        ApplyFloat(json, "mouseLookSensitivity", raw.mouseLookSensitivity, ref config.mouseLookSensitivity);
        ApplyFloat(json, "minPitch", raw.minPitch, ref config.minPitch);
        ApplyFloat(json, "maxPitch", raw.maxPitch, ref config.maxPitch);
        ApplyFloat(json, "jumpSpeed", raw.jumpSpeed, ref config.jumpSpeed);
        ApplyFloat(json, "groundCheckRadius", raw.groundCheckRadius, ref config.groundCheckRadius);
        ApplyFloat(json, "gravityAcceleration", raw.gravityAcceleration, ref config.gravityAcceleration);
        ApplyFloat(json, "groundedVerticalVelocity", raw.groundedVerticalVelocity, ref config.groundedVerticalVelocity);

        if (ContainsKey(json, "sprintMultiplier"))
        {
            config.sprintMultiplier = raw.sprintMultiplier;
        }
        else if (ContainsKey(json, "sprintSpeedMultiplier"))
        {
            // 兼容文档中的历史命名。
            config.sprintMultiplier = raw.sprintSpeedMultiplier;
        }

        if (ContainsKey(json, "groundMask"))
        {
            config.groundMask = raw.groundMask;
        }

        return true;
    }

    private static void ApplyFloat(string json, string key, float parsedValue, ref float target)
    {
        if (!ContainsKey(json, key))
        {
            return;
        }

        target = parsedValue;
    }

    private static bool ContainsKey(string json, string key)
    {
        string pattern = $"\"{Regex.Escape(key)}\"\\s*:";
        return Regex.IsMatch(json, pattern, RegexOptions.CultureInvariant);
    }
}
