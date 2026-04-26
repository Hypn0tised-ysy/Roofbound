using System;
using UnityEngine;

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
    [SerializeField] private int truckRowCount = 5;
    [SerializeField] private int truckColumnCount = 10;
    [SerializeField] private float truckColumnSpacing = 3f;
    [SerializeField] private float truckRowSpacing = 3f;
    [SerializeField] private float middleRowZ = 0f;
    [SerializeField] private float truckSpawnY = 0f;

    [Header("结束流程")]
    [SerializeField] private GameObject finishMenuPanel;
    [Tooltip("是否在玩家触地时触发 game_dead 事件。测试继续移动时可关闭。")]
    [SerializeField] private bool triggerDeadOnGroundHit = false;

    private GameObject spawnedMainCharacter;
    private bool isGameFinished;
    private bool isGameDead;

    private void Start()
    {
        if (initializeOnStart)
        {
            InitializeLevel();
        }
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

    private void TriggerGameFinish(GameObject player)
    {
        game_finish?.Invoke();
        Debug.Log($"[levelController] game_finish 触发。player={player.name}");

        // finish_menu 暂未实现，先提供占位加载逻辑。
        if (finishMenuPanel != null)
        {
            finishMenuPanel.SetActive(true);
        }
        else
        {
            Debug.Log("[levelController] finish_menu 暂未绑定，后续接入 UI 面板或场景加载。");
        }
    }

    private void TriggerGameDead(GameObject player)
    {
        game_dead?.Invoke();
        Debug.Log($"[levelController] game_dead 触发。player={player.name}");
        Debug.Log("[levelController] dead_menu 暂未实现，当前仅输出调试日志。");
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
            // 文档要求中间一排 z=0，其他排按行距向 +z/-z 方向递进。
            float rowZ = middleRowZ + (row - truckRowCount / 2f) * truckRowSpacing;

            for (int col = 0; col < truckColumnCount; col++)
            {
                float x = -halfWidth + col * truckColumnSpacing;
                Vector3 position = new Vector3(x, truckSpawnY, rowZ);

                GameObject truck = Instantiate(configAsset.truckPrefab, position, truckRotation, truckParent);
                truck.name = $"Truck_{spawnIndex:D2}";
                spawnIndex++;

                // 不覆盖速度，保持 truck_movement 从 truck_config 读取默认参数。
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
