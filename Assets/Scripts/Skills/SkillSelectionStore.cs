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
        if (data == null)
        {
            return;
        }

        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetFilePath(), json);
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
        if (string.IsNullOrEmpty(value))
        {
            return MovementSkillId.None;
        }

        return Enum.TryParse(value, true, out MovementSkillId result) ? result : MovementSkillId.None;
    }

    public static UtilitySkillId ParseUtility(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return UtilitySkillId.None;
        }

        return Enum.TryParse(value, true, out UtilitySkillId result) ? result : UtilitySkillId.None;
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
