using System;
using System.Collections.Generic;
using UnityEngine;
using Mono.Data.Sqlite;
using System.Data;
using System.IO;

public class DatabaseManager : MonoBehaviour
{
    private static DatabaseManager instance;
    private string connectionString;
    private SqliteConnection connection;

    public static DatabaseManager Instance
    {
        get
        {
            if (instance == null)
            {
                // 先在场景中查找
                instance = FindObjectOfType<DatabaseManager>();

                if (instance == null)
                {
                    // 创建新的GameObject
                    GameObject go = new GameObject("DatabaseManager");
                    instance = go.AddComponent<DatabaseManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        string dbPath = Path.Combine(Application.persistentDataPath, "game_data.db");
        connectionString = $"URI=file:{dbPath}";

        connection = new SqliteConnection(connectionString);
        connection.Open();

        CreateTables();
        Debug.Log($"数据库初始化成功，路径：{dbPath}");
    }

    private void CreateTables()
    {
        // 玩家账户表
        string createPlayerTable = @"
            CREATE TABLE IF NOT EXISTS PLAYER_ACCOUNT (
                Player_ID VARCHAR(36) PRIMARY KEY,
                Username VARCHAR(50) NOT NULL UNIQUE,
                Creation_Date DATETIME NOT NULL,
                Total_Play_Time INT DEFAULT 0,
                Total_Deaths INT DEFAULT 0,
                Points INT DEFAULT 0
            )";

        // 关卡基础配置表
        string createLevelTable = @"
            CREATE TABLE IF NOT EXISTS LEVEL_INFO (
                Level_ID INTEGER PRIMARY KEY AUTOINCREMENT,
                Level_Name VARCHAR(100) NOT NULL,
                Difficulty INT NOT NULL,
                Target_Time_3Star FLOAT NOT NULL,
                Target_Time_2Star FLOAT NOT NULL
            )";

        // 玩家关卡成绩流水表
        string createRunRecordTable = @"
            CREATE TABLE IF NOT EXISTS RUN_RECORD (
                Record_ID VARCHAR(36) PRIMARY KEY,
                Player_ID VARCHAR(36) NOT NULL,
                Level_ID INT NOT NULL,
                Completion_Time FLOAT NOT NULL,
                Earned_Stars INT NOT NULL,
                Record_Date DATETIME NOT NULL,
                FOREIGN KEY (Player_ID) REFERENCES PLAYER_ACCOUNT(Player_ID),
                FOREIGN KEY (Level_ID) REFERENCES LEVEL_INFO(Level_ID)
            )";

        ExecuteNonQuery(createPlayerTable);
        ExecuteNonQuery(createLevelTable);
        ExecuteNonQuery(createRunRecordTable);

        // 插入测试数据（如果没有关卡数据）
        CheckAndInsertTestData();
    }

    private void CheckAndInsertTestData()
    {
        DataTable result = ExecuteQuery("SELECT COUNT(*) FROM LEVEL_INFO");
        if (Convert.ToInt32(result.Rows[0][0]) == 0)
        {
            // 插入测试关卡
            ExecuteNonQuery("INSERT INTO LEVEL_INFO (Level_Name, Difficulty, Target_Time_3Star, Target_Time_2Star) VALUES ('赛博都市-01', 1, 60.0, 90.0)");
            ExecuteNonQuery("INSERT INTO LEVEL_INFO (Level_Name, Difficulty, Target_Time_3Star, Target_Time_2Star) VALUES ('暗黑森林-02', 2, 120.0, 180.0)");
            ExecuteNonQuery("INSERT INTO LEVEL_INFO (Level_Name, Difficulty, Target_Time_3Star, Target_Time_2Star) VALUES ('火焰山-03', 3, 180.0, 240.0)");
            Debug.Log("已插入测试关卡数据");
        }
    }

    public void ExecuteNonQuery(string query, SqliteParameter[] parameters = null)
    {
        using (SqliteCommand cmd = new SqliteCommand(query, connection))
        {
            if (parameters != null)
                cmd.Parameters.AddRange(parameters);
            cmd.ExecuteNonQuery();
        }
    }

    public DataTable ExecuteQuery(string query, SqliteParameter[] parameters = null)
    {
        DataTable dt = new DataTable();
        using (SqliteCommand cmd = new SqliteCommand(query, connection))
        {
            if (parameters != null)
                cmd.Parameters.AddRange(parameters);
            using (SqliteDataReader reader = cmd.ExecuteReader())
            {
                dt.Load(reader);
            }
        }
        return dt;
    }

    public SqliteConnection GetConnection()
    {
        return connection;
    }

    void OnDestroy()
    {
        if (connection != null && connection.State == ConnectionState.Open)
        {
            connection.Close();
            connection.Dispose();
        }
    }
}