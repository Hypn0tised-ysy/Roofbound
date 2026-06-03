using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class SkillSelectionData
{
    public string movementSkillId = MovementSkillId.None.ToString();
    public string utilitySkillId = UtilitySkillId.None.ToString();
    public List<string> movementSkills = new List<string>();
    public List<string> utilitySkills = new List<string>();
}

public static class SkillSelectionStore
{
    private const string FileName = "skills.json";
    private const string EmptyDescription = "";

    public static event Action<SkillSelectionData> SelectionChanged;

    public static SkillSelectionData Load()
    {
        string path = GetFilePath();
        if (!File.Exists(path))
        {
            return new SkillSelectionData();
        }

        try
        {
            string json = File.ReadAllText(path);
            SkillSelectionData data = JsonUtility.FromJson<SkillSelectionData>(json);
            return data ?? new SkillSelectionData();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SkillSelectionStore] Failed to load skills.json: {ex.Message}");
            return new SkillSelectionData();
        }
    }

    public static void Save(SkillSelectionData data)
    {
        Save(data, true);
    }

    public static void Save(SkillSelectionData data, bool notifyListeners)
    {
        if (data == null)
        {
            return;
        }

        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetFilePath(), json);
            if (notifyListeners)
            {
                SelectionChanged?.Invoke(data);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SkillSelectionStore] Failed to save skills.json: {ex.Message}");
        }
    }

    public static void RegisterSkill(string skillName, bool isMovement)
    {
        if (string.IsNullOrEmpty(skillName))
        {
            return;
        }

        SkillSelectionData data = Load();
        EnsureLists(data);

        List<string> list = isMovement ? data.movementSkills : data.utilitySkills;
        if (!list.Contains(skillName))
        {
            list.Add(skillName);
            Save(data);
        }
    }

    public static bool TryGetDescription(TextAsset catalog, string skillName, bool isMovement, out string description)
    {
        description = EmptyDescription;

        if (catalog == null || string.IsNullOrEmpty(skillName))
        {
            return false;
        }

        SkillCatalog parsed = JsonUtility.FromJson<SkillCatalog>(catalog.text);
        if (parsed == null)
        {
            return false;
        }

        SkillEntry[] entries = isMovement ? parsed.movement : parsed.utility;
        if (entries == null)
        {
            return false;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            SkillEntry entry = entries[i];
            if (entry != null && string.Equals(entry.name, skillName, StringComparison.OrdinalIgnoreCase))
            {
                description = entry.description ?? EmptyDescription;
                return true;
            }
        }

        return false;
    }

    public static MovementSkillId ParseMovement(string value)
    {
        return ResolveMovementId(value);
    }

    public static UtilitySkillId ParseUtility(string value)
    {
        return ResolveUtilityId(value);
    }

    public static MovementSkillId ResolveMovementId(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return MovementSkillId.None;
        }

        string normalized = value.Replace("_", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
        switch (normalized)
        {
            case "doublejump":
                return MovementSkillId.DoubleJump;
            case "airdash":
                return MovementSkillId.AirDash;
            case "jetpack":
                return MovementSkillId.JetPack;
            case "levitation":
                return MovementSkillId.Levitation;
            case "teleport":
                return MovementSkillId.Teleport;
            case "none":
                return MovementSkillId.None;
        }

        return Enum.TryParse(value, true, out MovementSkillId result) ? result : MovementSkillId.None;
    }

    public static UtilitySkillId ResolveUtilityId(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return UtilitySkillId.None;
        }

        string normalized = value.Replace("_", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
        switch (normalized)
        {
            case "timeslow":
                return UtilitySkillId.SlowTime;
            case "freezetrucks":
                return UtilitySkillId.FreezeTrucks;
            case "epicmode":
                return UtilitySkillId.EpicMode;
            case "none":
                return UtilitySkillId.None;
        }

        return Enum.TryParse(value, true, out UtilitySkillId result) ? result : UtilitySkillId.None;
    }

    /// <summary>与 Ability 面板按钮 GameObject 名称一致（camelCase）。</summary>
    public static string ToUiMovementName(MovementSkillId id)
    {
        switch (id)
        {
            case MovementSkillId.DoubleJump:
                return "doubleJump";
            case MovementSkillId.AirDash:
                return "airDash";
            case MovementSkillId.JetPack:
                return "jetPack";
            case MovementSkillId.Levitation:
                return "levitation";
            case MovementSkillId.Teleport:
                return "teleport";
            default:
                return "None";
        }
    }

    /// <summary>与 Ability 面板按钮 GameObject 名称一致（camelCase）。</summary>
    public static string ToUiUtilityName(UtilitySkillId id)
    {
        switch (id)
        {
            case UtilitySkillId.SlowTime:
                return "timeSlow";
            case UtilitySkillId.FreezeTrucks:
                return "freezeTrucks";
            case UtilitySkillId.EpicMode:
                return "epicMode";
            default:
                return "None";
        }
    }

    public static bool IsMovementSelected(string savedSkillId, string slotSkillName)
    {
        if (string.IsNullOrEmpty(savedSkillId) || savedSkillId == "None")
        {
            return false;
        }

        return ResolveMovementId(savedSkillId) == ResolveMovementId(slotSkillName);
    }

    public static bool IsUtilitySelected(string savedSkillId, string slotSkillName)
    {
        if (string.IsNullOrEmpty(savedSkillId) || savedSkillId == "None")
        {
            return false;
        }

        return ResolveUtilityId(savedSkillId) == ResolveUtilityId(slotSkillName);
    }

    public static void SaveConfiguredSkills(
        MovementSkillId movementSkill,
        UtilitySkillId utilitySkill,
        bool notifyListeners = true)
    {
        SkillSelectionData data = Load();
        data.movementSkillId = ToUiMovementName(movementSkill);
        data.utilitySkillId = ToUiUtilityName(utilitySkill);
        Save(data, notifyListeners);
    }

    public static void RefreshAllAbilitySlots()
    {
        MovementSlotButton[] movementSlots = UnityEngine.Object.FindObjectsOfType<MovementSlotButton>(true);
        for (int i = 0; i < movementSlots.Length; i++)
        {
            movementSlots[i].RefreshFromSavedSelection();
        }

        UtilitySlotButton[] utilitySlots = UnityEngine.Object.FindObjectsOfType<UtilitySlotButton>(true);
        for (int i = 0; i < utilitySlots.Length; i++)
        {
            utilitySlots[i].RefreshFromSavedSelection();
        }
    }

    private static string GetFilePath()
    {
        return Path.Combine(Application.persistentDataPath, FileName);
    }

    private static void EnsureLists(SkillSelectionData data)
    {
        if (data.movementSkills == null)
        {
            data.movementSkills = new List<string>();
        }

        if (data.utilitySkills == null)
        {
            data.utilitySkills = new List<string>();
        }
    }

    [Serializable]
    private class SkillCatalog
    {
        public SkillEntry[] movement;
        public SkillEntry[] utility;
    }

    [Serializable]
    private class SkillEntry
    {
        public string name;
        public string description;
        public int cooldown;
    }
}
