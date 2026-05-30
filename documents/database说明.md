## 常用操作速查表

| 操作         | 代码示例                                                   |
| :----------- | :--------------------------------------------------------- |
| 创建玩家     | `PlayerAccountDAO.CreatePlayer(new PlayerAccount("名字"))` |
| 查询玩家     | `PlayerAccountDAO.GetPlayerByID("玩家ID")`                 |
| 按用户名查   | `PlayerAccountDAO.GetPlayerByUsername("名字")`             |
| 增加积分     | `PlayerAccountDAO.AddPoints("玩家ID", 100)`                |
| 增加死亡     | `PlayerAccountDAO.AddDeathCount("玩家ID")`                 |
| 获取关卡     | `LevelInfoDAO.GetLevelByID(1)`                             |
| 获取所有关卡 | `LevelInfoDAO.GetAllLevels()`                              |
| 记录成绩     | `RunRecordDAO.AddRunRecord(成绩记录)`                      |
| 查看历史     | `RunRecordDAO.GetPlayerRecords("玩家ID")`                  |
| 查看排行榜   | `RunRecordDAO.GetTopPlayersByLevel(关卡ID)`                |

函数与示例详解

## 一、玩家账户操作 (PlayerAccountDAO)

### 1. 创建玩家

csharp

```
// 函数调用
PlayerAccount newPlayer = new PlayerAccount("张三");
bool result = PlayerAccountDAO.CreatePlayer(newPlayer);

// 返回值示例
result = true  // 创建成功
result = false // 创建失败（如用户名重复、数据库错误）
```



### 2. 通过ID查询玩家

csharp

```
// 函数调用
PlayerAccount player = PlayerAccountDAO.GetPlayerByID("550e8400-e29b-41d4-a716-446655440000");

// 返回值示例（成功）
player = {
    Player_ID = "550e8400-e29b-41d4-a716-446655440000",
    Username = "张三",
    Creation_Date = "2024-01-15 10:30:00",
    Total_Play_Time = 3600,    // 1小时
    Total_Deaths = 5,
    Points = 150
}

// 返回值示例（失败）
player = null  // 玩家不存在或查询失败
```



### 3. 通过用户名查询玩家

csharp

```
// 函数调用
PlayerAccount player = PlayerAccountDAO.GetPlayerByUsername("张三");

// 返回值示例（成功）
player = {
    Player_ID = "550e8400-e29b-41d4-a716-446655440000",
    Username = "张三",
    Creation_Date = "2024-01-15 10:30:00",
    Total_Play_Time = 3600,
    Total_Deaths = 5,
    Points = 150
}

// 返回值示例（失败）
player = null  // 玩家不存在
```



### 4. 更新玩家积分

csharp

```
// 函数调用
bool result = PlayerAccountDAO.UpdatePlayerPoints("550e8400-e29b-41d4-a716-446655440000", 500);

// 返回值示例
result = true  // 更新成功
result = false // 更新失败
```



### 5. 增加游戏时长

csharp

```
// 函数调用（增加100秒）
bool result = PlayerAccountDAO.AddPlayTime("550e8400-e29b-41d4-a716-446655440000", 100);

// 返回值示例
result = true  // 增加成功
result = false // 增加失败
```



### 6. 增加死亡次数

csharp

```
// 函数调用
bool result = PlayerAccountDAO.AddDeathCount("550e8400-e29b-41d4-a716-446655440000");

// 返回值示例
result = true  // 增加成功
result = false // 增加失败
```



### 7. 增加积分

csharp

```
// 函数调用（增加50积分）
bool result = PlayerAccountDAO.AddPoints("550e8400-e29b-41d4-a716-446655440000", 50);

// 返回值示例
result = true  // 增加成功
result = false // 增加失败
```



------

## 二、关卡信息操作 (LevelInfoDAO)

### 1. 添加关卡

csharp

```
// 函数调用
LevelInfo newLevel = new LevelInfo
{
    Level_Name = "神秘森林-04",
    Difficulty = 2,
    Target_Time_3Star = 150.0f,
    Target_Time_2Star = 200.0f
};
bool result = LevelInfoDAO.AddLevel(newLevel);

// 返回值示例
result = true  // 添加成功
result = false // 添加失败
```



### 2. 通过ID查询关卡

csharp

```
// 函数调用
LevelInfo level = LevelInfoDAO.GetLevelByID(1);

// 返回值示例（成功）
level = {
    Level_ID = 1,
    Level_Name = "赛博都市-01",
    Difficulty = 1,           // 1简单 2中等 3困难
    Target_Time_3Star = 60.0,
    Target_Time_2Star = 90.0
}

// 返回值示例（失败）
level = null  // 关卡不存在
```



### 3. 获取所有关卡

csharp

```
// 函数调用
List<LevelInfo> levels = LevelInfoDAO.GetAllLevels();

// 返回值示例（成功）
levels = [
    { Level_ID = 1, Level_Name = "赛博都市-01", Difficulty = 1, ... },
    { Level_ID = 2, Level_Name = "暗黑森林-02", Difficulty = 2, ... },
    { Level_ID = 3, Level_Name = "火焰山-03", Difficulty = 3, ... }
]
// Count = 3

// 返回值示例（失败或无数据）
levels = []  // 空列表，Count = 0
```



### 4. 计算星级

csharp

```
// 函数调用
LevelInfo level = LevelInfoDAO.GetLevelByID(1);
int stars = level.CalculateStars(45.5f);

// 返回值示例
stars = 3  // 45.5秒 ≤ 60秒 → 3星
stars = 2  // 61秒 ~ 90秒 → 2星
stars = 1  // 91秒以上 → 1星
```



------

## 三、成绩记录操作 (RunRecordDAO)

### 1. 添加成绩记录

csharp

```
// 函数调用
RunRecord record = new RunRecord(
    playerID: "550e8400-e29b-41d4-a716-446655440000",
    levelID: 1,
    completionTime: 55.5f,
    stars: 3
);
bool result = RunRecordDAO.AddRunRecord(record);

// 返回值示例
result = true  // 添加成功（会自动增加积分：3星×10=30分）
result = false // 添加失败
```



### 2. 获取玩家所有成绩

csharp

```
// 函数调用
List<RunRecord> records = RunRecordDAO.GetPlayerRecords("550e8400-e29b-41d4-a716-446655440000");

// 返回值示例（成功）
records = [
    {
        Record_ID = "660e8400-e29b-41d4-a716-446655440001",
        Player_ID = "550e8400-e29b-41d4-a716-446655440000",
        Level_ID = 1,
        Completion_Time = 55.5,
        Earned_Stars = 3,
        Record_Date = "2024-01-15 14:30:25"
    },
    {
        Record_ID = "660e8400-e29b-41d4-a716-446655440002",
        Player_ID = "550e8400-e29b-41d4-a716-446655440000",
        Level_ID = 2,
        Completion_Time = 125.3,
        Earned_Stars = 2,
        Record_Date = "2024-01-15 15:45:10"
    }
]
// Count = 2

// 返回值示例（无成绩）
records = []  // 空列表
```



### 3. 获取玩家最佳成绩（特定关卡）

csharp

```
// 函数调用
RunRecord best = RunRecordDAO.GetPlayerBestRecord("550e8400-e29b-41d4-a716-446655440000", 1);

// 返回值示例（有成绩）
best = {
    Record_ID = "660e8400-e29b-41d4-a716-446655440001",
    Player_ID = "550e8400-e29b-41d4-a716-446655440000",
    Level_ID = 1,
    Completion_Time = 55.5,    // 最快时间
    Earned_Stars = 3,
    Record_Date = "2024-01-15 14:30:25"
}

// 返回值示例（无成绩）
best = null  // 玩家未通关此关卡
```



### 4. 获取关卡排行榜

csharp

```
// 函数调用（获取前10名）
List<RunRecord> topPlayers = RunRecordDAO.GetTopPlayersByLevel(1, 10);

// 返回值示例（有数据）
topPlayers = [
    { Player_ID = "xxx1", Completion_Time = 45.2, Earned_Stars = 3, ... },  // 第1名
    { Player_ID = "xxx2", Completion_Time = 52.8, Earned_Stars = 3, ... },  // 第2名
    { Player_ID = "xxx3", Completion_Time = 61.5, Earned_Stars = 2, ... },  // 第3名
    // ... 共10条
]

// 返回值示例（无数据）
topPlayers = []  // 空列表
```