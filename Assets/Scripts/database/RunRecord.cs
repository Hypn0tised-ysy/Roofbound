using System;
using System.Collections.Generic;
using System.Data;
using Mono.Data.Sqlite;
using UnityEngine;

[Serializable]
public class RunRecord
{
    public string Record_ID { get; set; }
    public string Player_ID { get; set; }
    public int Level_ID { get; set; }
    public float Completion_Time { get; set; }
    public int Earned_Stars { get; set; }
    public DateTime Record_Date { get; set; }

    public RunRecord()
    {
        Record_ID = Guid.NewGuid().ToString();
        Record_Date = DateTime.Now;
    }

    public RunRecord(string playerID, int levelID, float completionTime, int stars) : this()
    {
        Player_ID = playerID;
        Level_ID = levelID;
        Completion_Time = completionTime;
        Earned_Stars = stars;
    }
}

public class RunRecordDAO
{
    public static bool AddRunRecord(RunRecord record)
    {
        try
        {
            string query = @"INSERT INTO RUN_RECORD 
                            (Record_ID, Player_ID, Level_ID, Completion_Time, Earned_Stars, Record_Date) 
                            VALUES (@Record_ID, @Player_ID, @Level_ID, @Completion_Time, @Earned_Stars, @Record_Date)";

            SqliteParameter[] parameters = {
                new SqliteParameter("@Record_ID", record.Record_ID),
                new SqliteParameter("@Player_ID", record.Player_ID),
                new SqliteParameter("@Level_ID", record.Level_ID),
                new SqliteParameter("@Completion_Time", record.Completion_Time),
                new SqliteParameter("@Earned_Stars", record.Earned_Stars),
                new SqliteParameter("@Record_Date", record.Record_Date.ToString("yyyy-MM-dd HH:mm:ss"))
            };

            DatabaseManager.Instance.ExecuteNonQuery(query, parameters);

            // ���ݻ�õ��������ӻ��֣�ÿ����10�֣�
            int pointsToAdd = record.Earned_Stars * 10;
            PlayerAccountDAO.AddPoints(record.Player_ID, pointsToAdd);

            Debug.Log($"��¼�ɼ��ɹ������{record.Earned_Stars}�ǣ�����{pointsToAdd}����");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"���ӳɼ���¼ʧ��: {e.Message}");
            return false;
        }
    }

    public static List<RunRecord> GetPlayerRecords(string playerID)
    {
        List<RunRecord> records = new List<RunRecord>();
        try
        {
            string query = "SELECT * FROM RUN_RECORD WHERE Player_ID = @Player_ID ORDER BY Record_Date DESC";
            SqliteParameter[] parameters = { new SqliteParameter("@Player_ID", playerID) };

            DataTable result = DatabaseManager.Instance.ExecuteQuery(query, parameters);

            foreach (DataRow row in result.Rows)
            {
                records.Add(new RunRecord
                {
                    Record_ID = row["Record_ID"].ToString(),
                    Player_ID = row["Player_ID"].ToString(),
                    Level_ID = Convert.ToInt32(row["Level_ID"]),
                    Completion_Time = Convert.ToSingle(row["Completion_Time"]),
                    Earned_Stars = Convert.ToInt32(row["Earned_Stars"]),
                    Record_Date = DateTime.Parse(row["Record_Date"].ToString())
                });
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"��ѯ��ҳɼ�ʧ��: {e.Message}");
        }
        return records;
    }

    public static RunRecord GetPlayerBestRecord(string playerID, int levelID)
    {
        try
        {
            string query = @"SELECT * FROM RUN_RECORD 
                            WHERE Player_ID = @Player_ID AND Level_ID = @Level_ID 
                              AND Completion_Time > 0.01
                            ORDER BY Completion_Time ASC LIMIT 1";

            SqliteParameter[] parameters = {
                new SqliteParameter("@Player_ID", playerID),
                new SqliteParameter("@Level_ID", levelID)
            };

            DataTable result = DatabaseManager.Instance.ExecuteQuery(query, parameters);

            if (result.Rows.Count > 0)
            {
                DataRow row = result.Rows[0];
                return new RunRecord
                {
                    Record_ID = row["Record_ID"].ToString(),
                    Player_ID = row["Player_ID"].ToString(),
                    Level_ID = Convert.ToInt32(row["Level_ID"]),
                    Completion_Time = Convert.ToSingle(row["Completion_Time"]),
                    Earned_Stars = Convert.ToInt32(row["Earned_Stars"]),
                    Record_Date = DateTime.Parse(row["Record_Date"].ToString())
                };
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"��ѯ��ѳɼ�ʧ��: {e.Message}");
        }
        return null;
    }

    public static List<RunRecord> GetTopPlayersByLevel(int levelID, int limit = 10)
    {
        List<RunRecord> records = new List<RunRecord>();
        try
        {
            string query = @"SELECT * FROM RUN_RECORD 
                            WHERE Level_ID = @Level_ID 
                            ORDER BY Completion_Time ASC LIMIT @Limit";

            SqliteParameter[] parameters = {
                new SqliteParameter("@Level_ID", levelID),
                new SqliteParameter("@Limit", limit)
            };

            DataTable result = DatabaseManager.Instance.ExecuteQuery(query, parameters);

            foreach (DataRow row in result.Rows)
            {
                records.Add(new RunRecord
                {
                    Record_ID = row["Record_ID"].ToString(),
                    Player_ID = row["Player_ID"].ToString(),
                    Level_ID = Convert.ToInt32(row["Level_ID"]),
                    Completion_Time = Convert.ToSingle(row["Completion_Time"]),
                    Earned_Stars = Convert.ToInt32(row["Earned_Stars"]),
                    Record_Date = DateTime.Parse(row["Record_Date"].ToString())
                });
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"��ѯ���а�ʧ��: {e.Message}");
        }
        return records;
    }
}