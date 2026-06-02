using UnityEngine;

/// <summary>
/// 本地单机玩家会话：首次运行时创建账户，并将 Player_ID 存入 PlayerPrefs。
/// </summary>
public static class PlayerSession
{
    private const string PlayerIdKey = "LocalPlayerId";

    public static string GetOrCreateLocalPlayerId()
    {
        if (PlayerPrefs.HasKey(PlayerIdKey))
        {
            string existingId = PlayerPrefs.GetString(PlayerIdKey);
            if (!string.IsNullOrEmpty(existingId) && PlayerAccountDAO.GetPlayerByID(existingId) != null)
            {
                return existingId;
            }
        }

        PlayerAccount player = new PlayerAccount("Player");
        if (!PlayerAccountDAO.CreatePlayer(player))
        {
            Debug.LogWarning("[PlayerSession] 创建本地玩家失败，成绩将无法写入数据库。");
            return null;
        }

        PlayerPrefs.SetString(PlayerIdKey, player.Player_ID);
        PlayerPrefs.Save();
        return player.Player_ID;
    }
}
