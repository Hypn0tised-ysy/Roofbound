using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class levelController : MonoBehaviour
{
    public event Action game_finish;
    public event Action game_dead;

    [Header("关卡配置")]
    [SerializeField] private level1_config configAsset;

    [Header("生成点")]
    [SerializeField] private Transform spawnPoint;

    [Header("初始化")]
    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private Transform truckParent;

    [Header("卡车网格生成")]
    [SerializeField] private int truckRowCount = 0;
    [SerializeField] private int truckColumnCount = 0;
    [SerializeField] private float truckColumnSpacing = 3f;
    [SerializeField] private float truckRowSpacing = 3f;
    [SerializeField] private float middleRowZ = 0f;
    [SerializeField] private float truckSpawnY = 0f;

    [Header("关卡 UI（胜利/失败，仅无 UIManager 时使用）")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [Header("结束流程")]
    [SerializeField] private GameObject finishMenuPanel;
    [Tooltip("是否在玩家触地时触发 game_dead 事件。测试继续移动时可关闭。")]
    [SerializeField] private bool triggerDeadOnGroundHit = false;

    private GameObject spawnedMainCharacter;
    private bool isGameFinished;
    private bool isGameDead;

    private void Start()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        if (initializeOnStart)
            InitializeLevel();
    }

    public void InitializeLevel()
    {
        isGameFinished = false;
        isGameDead = false;

        if (configAsset == null)
        {
            Debug.LogError("[levelController] 未绑定 level1_config，无法初始化关卡。");
            return;
        }

        SpawnTrucks();
        SpawnMainCharacter();

        if (finishMenuPanel != null)
        {
            finishMenuPanel.SetActive(false);
        }
    }

    public void NotifyPlayerReachedDestination(GameObject player)
    {
        if (isGameFinished || isGameDead)
        {
            return;
        }

        isGameFinished = true;
        TriggerGameFinish(player);
    }

    public void NotifyPlayerHitGround(GameObject player)
    {
        if (!triggerDeadOnGroundHit)
        {
            Debug.Log("[levelController] 已关闭触地死亡触发，忽略本次触地事件。");
            return;
        }

        if (isGameFinished || isGameDead)
        {
            return;
        }

        isGameDead = true;
        TriggerGameDead(player);
    }

    public void NotifyPlayerKilledByHazard(GameObject player)
    {
        if (isGameFinished || isGameDead)
        {
            return;
        }

        isGameDead = true;
        TriggerGameDead(player);
    }

    private void TriggerGameFinish(GameObject player)
    {
        game_finish?.Invoke();
        Debug.Log($"[levelController] game_finish 触发。player={player.name}");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.TriggerVictory();
        }
        else if (winPanel != null)
        {
            winPanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    private void TriggerGameDead(GameObject player)
    {
        game_dead?.Invoke();
        Debug.Log($"[levelController] game_dead 触发。player={player.name}");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.TriggerGameOver();
        }
        else if (losePanel != null)
        {
            losePanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.RestartLevel();
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void SpawnTrucks()
    {
        if (configAsset.truckPrefab == null)
        {
            Debug.LogError("[levelController] level1_config 未配置 truckPrefab。");
            return;
        }

        if (truckRowCount <= 0 || truckColumnCount <= 0)
        {
            Debug.LogWarning("[levelController] 卡车网格行列数非法，不执行生成。");
            return;
        }

        float halfWidth = (truckColumnCount - 1) * truckColumnSpacing * 0.5f;
        Quaternion truckRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

        int spawnIndex = 0;
        for (int row = 0; row < truckRowCount; row++)
        {
            float rowZ = middleRowZ + (row - truckRowCount / 2f) * truckRowSpacing;

            for (int col = 0; col < truckColumnCount; col++)
            {
                float x = -halfWidth + col * truckColumnSpacing;
                Vector3 position = new Vector3(x, truckSpawnY, rowZ);

                GameObject truck = Instantiate(configAsset.truckPrefab, position, truckRotation, truckParent);
                truck.name = $"Truck_{spawnIndex:D2}";
                spawnIndex++;

                truck_movement movement = truck.GetComponent<truck_movement>();
                if (movement != null)
                {
                    movement.SetRuntimeSpeed(0f, false);
                }
            }
        }
    }

    private void SpawnMainCharacter()
    {
        TruckFleetSpawner fleetSpawner = FindObjectOfType<TruckFleetSpawner>();
        if (fleetSpawner != null && fleetSpawner.playerPrefab != null)
        {
            Debug.Log("[levelController] 检测到 TruckFleetSpawner，跳过 levelController 生成主角。");
            return;
        }

        if (configAsset.mainCharacterPrefab == null)
        {
            Debug.LogError("[levelController] level1_config 未配置 mainCharacterPrefab。");
            return;
        }

        Transform anchor = spawnPoint != null ? spawnPoint : transform;
        if (spawnPoint == null)
        {
            Debug.LogWarning("[levelController] 未配置 spawnPoint，回退到 levelController 自身位置生成主角。");
        }

        if (spawnedMainCharacter != null)
        {
            Destroy(spawnedMainCharacter);
        }

        spawnedMainCharacter = Instantiate(
            configAsset.mainCharacterPrefab,
            anchor.position,
            Quaternion.LookRotation(Vector3.forward, Vector3.up));
        spawnedMainCharacter.name = "MainCharacter";
    }
}
