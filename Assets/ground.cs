using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ground : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private levelController levelControllerRef;

    private bool hasTriggered;

    private void Awake()
    {
        if (levelControllerRef == null)
        {
            levelControllerRef = FindObjectOfType<levelController>();
            if (levelControllerRef == null)
            {
                Debug.LogError("[ground] 未找到 levelController，请确保场景中存在一个 levelController 实例。");
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandlePlayerHit(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandlePlayerHit(other.gameObject);
    }

    private void HandlePlayerHit(GameObject target)
    {
        if (hasTriggered)
        {
            return;
        }

        if (target == null || !target.CompareTag(playerTag))
        {
            return;
        }

        if (levelControllerRef == null)
        {
            Debug.LogError("[ground] 未找到 levelController，无法触发 dead 状态。");
            return;
        }

        if (target.CompareTag(playerTag))


        {
            hasTriggered = true;
            levelControllerRef.NotifyPlayerHitGround(target);
            Debug.Log("[ground] 玩家与地面碰撞，触发死状态通知。"); // 占位输出，后续替换为加载死菜单等逻辑
        }

    }
}
