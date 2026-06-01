using UnityEngine;

/// <summary>
/// 最短通关时间读写：基于 RUN_RECORD 表，按玩家 + 关卡取 Completion_Time 最小值。
/// </summary>
public static class BestTimeService
{
    /// <summary>关卡选择索引（0-based）对应 LEVEL_INFO.Level_ID（1-based）。</summary>
    public static int ResolveLevelId(int levelIndex)
    {
        return Mathf.Max(1, levelIndex + 1);
    }

    public static bool TryGetBestTime(int levelIndex, out float bestTime)
    {
        bestTime = -1f;

        string playerId = PlayerSession.GetOrCreateLocalPlayerId();
        if (string.IsNullOrEmpty(playerId))
        {
            return false;
        }

        _ = DatabaseManager.Instance;

        int levelId = ResolveLevelId(levelIndex);
        RunRecord best = RunRecordDAO.GetPlayerBestRecord(playerId, levelId);
        if (best == null)
        {
            return false;
        }

        bestTime = best.Completion_Time;
        return true;
    }

    /// <summary>
    /// 保存本次通关并返回该关最短用时（秒）。
    /// </summary>
    public static float SaveCompletionAndGetBest(int levelIndex, float completionTime)
    {
        if (completionTime < 0f)
        {
            completionTime = 0f;
        }

        string playerId = PlayerSession.GetOrCreateLocalPlayerId();
        if (string.IsNullOrEmpty(playerId))
        {
            return completionTime;
        }

        _ = DatabaseManager.Instance;

        int levelId = ResolveLevelId(levelIndex);
        LevelInfo level = LevelInfoDAO.GetLevelByID(levelId);
        int stars = level != null ? level.CalculateStars(completionTime) : 1;

        RunRecord record = new RunRecord(playerId, levelId, completionTime, stars);
        RunRecordDAO.AddRunRecord(record);

        PlayerAccountDAO.AddPlayTime(playerId, Mathf.CeilToInt(completionTime));

        if (TryGetBestTime(levelIndex, out float bestTime))
        {
            return bestTime;
        }

        return completionTime;
    }
}
