using UnityEngine;

/// <summary>
/// 玩家侧地面命中检测：由 playerControl 在 OnControllerColliderHit 中调用。
/// 命中地面后通知 levelController 进入 dead 流程。
/// </summary>
public class playerHitGround : MonoBehaviour
{
    [SerializeField] private levelController levelControllerRef;
    [SerializeField] private string groundTag = "ground";
    [SerializeField] private bool requireGroundTag = false;
    [SerializeField] private float minGroundNormalDot = 0.3f;

    private bool hasTriggered;

    private void Awake()
    {
        if (levelControllerRef == null)
        {
            levelControllerRef = FindObjectOfType<levelController>();
        }
    }

    public void TryHandleControllerHit(ControllerColliderHit hit, Vector3 upAxis, GameObject playerObject)
    {
        if (hasTriggered || hit == null || playerObject == null)
        {
            return;
        }

        if (Vector3.Dot(hit.normal, upAxis) < minGroundNormalDot)
        {
            return;
        }

        if (!IsGroundTarget(hit.collider))
        {
            return;
        }

        if (levelControllerRef == null)
        {
            Debug.LogError("[playerHitGround] 未找到 levelController，无法触发 dead 状态。");
            return;
        }

        hasTriggered = true;
        levelControllerRef.NotifyPlayerHitGround(playerObject);
        Debug.Log("[playerHitGround] 玩家命中地面，已触发 dead 通知。");
    }

    public void ResetTriggerState()
    {
        hasTriggered = false;
    }

    private bool IsGroundTarget(Collider target)
    {
        if (target == null)
        {
            return false;
        }

        if (target.GetComponent<ground>() != null)
        {
            return true;
        }

        if (requireGroundTag)
        {
            return target.CompareTag(groundTag);
        }

        return true;
    }
}
