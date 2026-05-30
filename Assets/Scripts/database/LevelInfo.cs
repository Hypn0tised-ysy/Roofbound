using System;
using System.Collections.Generic;
using System.Data;
using Mono.Data.Sqlite;
using UnityEngine;

[Serializable]
public class LevelInfo
{
    public int Level_ID { get; set; }
    public string Level_Name { get; set; }
    public int Difficulty { get; set; }
    public float Target_Time_3Star { get; set; }
    public float Target_Time_2Star { get; set; }

    public int CalculateStars(float completionTime)
    {
        if (completionTime <= Target_Time_3Star)
            return 3;
        else if (completionTime <= Target_Time_2Star)
            return 2;
        else
            return 1;
    }
}

public class LevelInfoDAO
{
    public static bool AddLevel(LevelInfo level)
    {
        try
        {
            string query = @"INSERT INTO LEVEL_INFO 
                            (Level_Name, Difficulty, Target_Time_3Star, Target_Time_2Star) 
                            VALUES (@Level_Name, @Difficulty, @Target_Time_3Star, @Target_Time_2Star)";

            SqliteParameter[] parameters = {
                new SqliteParameter("@Level_Name", level.Level_Name),
                new SqliteParameter("@Difficulty", level.Difficulty),
                new SqliteParameter("@Target_Time_3Star", level.Target_Time_3Star),
                new SqliteParameter("@Target_Time_2Star", level.Target_Time_2Star)
            };

            DatabaseManager.Instance.ExecuteNonQuery(query, parameters);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"添加关卡失败: {e.Message}");
            return false;
        }
    }

    public static LevelInfo GetLevelByID(int levelID)
    {
        try
        {
            string query = "SELECT * FROM LEVEL_INFO WHERE Level_ID = @Level_ID";
            SqliteParameter[] parameters = { new SqliteParameter("@Level_ID", levelID) };

            DataTable result = DatabaseManager.Instance.ExecuteQuery(query, parameters);

            if (result.Rows.Count > 0)
            {
                DataRow row = result.Rows[0];
                return new LevelInfo
                {
                    Level_ID = Convert.ToInt32(row["Level_ID"]),
                    Level_Name = row["Level_Name"].ToString(),
                    Difficulty = Convert.ToInt32(row["Difficulty"]),
                    Target_Time_3Star = Convert.ToSingle(row["Target_Time_3Star"]),
                    Target_Time_2Star = Convert.ToSingle(row["Target_Time_2Star"])
                };
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"查询关卡失败: {e.Message}");
        }
        return null;
    }

    public static List<LevelInfo> GetAllLevels()
    {
        List<LevelInfo> levels = new List<LevelInfo>();
        try
        {
            DataTable result = DatabaseManager.Instance.ExecuteQuery("SELECT * FROM LEVEL_INFO ORDER BY Level_ID");

            foreach (DataRow row in result.Rows)
            {
                levels.Add(new LevelInfo
                {
                    Level_ID = Convert.ToInt32(row["Level_ID"]),
                    Level_Name = row["Level_Name"].ToString(),
                    Difficulty = Convert.ToInt32(row["Difficulty"]),
                    Target_Time_3Star = Convert.ToSingle(row["Target_Time_3Star"]),
                    Target_Time_2Star = Convert.ToSingle(row["Target_Time_2Star"])
                });
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"获取所有关卡失败: {e.Message}");
        }
        return levels;
    }
}