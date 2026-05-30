using System;
using System.Data;
using Mono.Data.Sqlite;
using UnityEngine;

[Serializable]
public class PlayerAccount
{
    public string Player_ID { get; set; }
    public string Username { get; set; }
    public DateTime Creation_Date { get; set; }
    public int Total_Play_Time { get; set; }
    public int Total_Deaths { get; set; }
    public int Points { get; set; }

    public PlayerAccount()
    {
        Player_ID = Guid.NewGuid().ToString();
        Creation_Date = DateTime.Now;
        Total_Play_Time = 0;
        Total_Deaths = 0;
        Points = 0;
    }

    public PlayerAccount(string username) : this()
    {
        Username = username;
    }
}

public class PlayerAccountDAO
{
    public static bool CreatePlayer(PlayerAccount player)
    {
        try
        {
            string query = @"INSERT INTO PLAYER_ACCOUNT 
                            (Player_ID, Username, Creation_Date, Total_Play_Time, Total_Deaths, Points) 
                            VALUES (@Player_ID, @Username, @Creation_Date, @Total_Play_Time, @Total_Deaths, @Points)";

            SqliteParameter[] parameters = {
                new SqliteParameter("@Player_ID", player.Player_ID),
                new SqliteParameter("@Username", player.Username),
                new SqliteParameter("@Creation_Date", player.Creation_Date.ToString("yyyy-MM-dd HH:mm:ss")),
                new SqliteParameter("@Total_Play_Time", player.Total_Play_Time),
                new SqliteParameter("@Total_Deaths", player.Total_Deaths),
                new SqliteParameter("@Points", player.Points)
            };

            DatabaseManager.Instance.ExecuteNonQuery(query, parameters);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"创建玩家失败: {e.Message}");
            return false;
        }
    }

    public static PlayerAccount GetPlayerByID(string playerID)
    {
        try
        {
            string query = "SELECT * FROM PLAYER_ACCOUNT WHERE Player_ID = @Player_ID";
            SqliteParameter[] parameters = { new SqliteParameter("@Player_ID", playerID) };

            DataTable result = DatabaseManager.Instance.ExecuteQuery(query, parameters);

            if (result.Rows.Count > 0)
            {
                DataRow row = result.Rows[0];
                return new PlayerAccount
                {
                    Player_ID = row["Player_ID"].ToString(),
                    Username = row["Username"].ToString(),
                    Creation_Date = DateTime.Parse(row["Creation_Date"].ToString()),
                    Total_Play_Time = Convert.ToInt32(row["Total_Play_Time"]),
                    Total_Deaths = Convert.ToInt32(row["Total_Deaths"]),
                    Points = Convert.ToInt32(row["Points"])
                };
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"查询玩家失败: {e.Message}");
        }
        return null;
    }

    public static PlayerAccount GetPlayerByUsername(string username)
    {
        try
        {
            string query = "SELECT * FROM PLAYER_ACCOUNT WHERE Username = @Username";
            SqliteParameter[] parameters = { new SqliteParameter("@Username", username) };

            DataTable result = DatabaseManager.Instance.ExecuteQuery(query, parameters);

            if (result.Rows.Count > 0)
            {
                DataRow row = result.Rows[0];
                return new PlayerAccount
                {
                    Player_ID = row["Player_ID"].ToString(),
                    Username = row["Username"].ToString(),
                    Creation_Date = DateTime.Parse(row["Creation_Date"].ToString()),
                    Total_Play_Time = Convert.ToInt32(row["Total_Play_Time"]),
                    Total_Deaths = Convert.ToInt32(row["Total_Deaths"]),
                    Points = Convert.ToInt32(row["Points"])
                };
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"查询玩家失败: {e.Message}");
        }
        return null;
    }

    public static bool UpdatePlayerPoints(string playerID, int newPoints)
    {
        try
        {
            string query = "UPDATE PLAYER_ACCOUNT SET Points = @Points WHERE Player_ID = @Player_ID";
            SqliteParameter[] parameters = {
                new SqliteParameter("@Points", newPoints),
                new SqliteParameter("@Player_ID", playerID)
            };

            DatabaseManager.Instance.ExecuteNonQuery(query, parameters);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"更新积分失败: {e.Message}");
            return false;
        }
    }

    public static bool AddPlayTime(string playerID, int seconds)
    {
        try
        {
            string query = "UPDATE PLAYER_ACCOUNT SET Total_Play_Time = Total_Play_Time + @Seconds WHERE Player_ID = @Player_ID";
            SqliteParameter[] parameters = {
                new SqliteParameter("@Seconds", seconds),
                new SqliteParameter("@Player_ID", playerID)
            };

            DatabaseManager.Instance.ExecuteNonQuery(query, parameters);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"更新游戏时长失败: {e.Message}");
            return false;
        }
    }

    public static bool AddDeathCount(string playerID)
    {
        try
        {
            string query = "UPDATE PLAYER_ACCOUNT SET Total_Deaths = Total_Deaths + 1 WHERE Player_ID = @Player_ID";
            SqliteParameter[] parameters = { new SqliteParameter("@Player_ID", playerID) };

            DatabaseManager.Instance.ExecuteNonQuery(query, parameters);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"更新死亡次数失败: {e.Message}");
            return false;
        }
    }

    public static bool AddPoints(string playerID, int points)
    {
        try
        {
            string query = "UPDATE PLAYER_ACCOUNT SET Points = Points + @Points WHERE Player_ID = @Player_ID";
            SqliteParameter[] parameters = {
                new SqliteParameter("@Points", points),
                new SqliteParameter("@Player_ID", playerID)
            };

            DatabaseManager.Instance.ExecuteNonQuery(query, parameters);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"增加积分失败: {e.Message}");
            return false;
        }
    }
}