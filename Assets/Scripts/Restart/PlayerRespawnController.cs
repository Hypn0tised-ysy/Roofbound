using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 角色死亡复位控制器：
/// 1. 按 R 键模拟角色死亡，将角色复位到初始位置。
/// 2. 支持设置初始位置（可在 Inspector 中配置，后续关卡更改）。
/// 3. 复位时重置物理状态、跳跃状态、冲刺状态和镜头朝向。
/// </summary>
public class PlayerRespawnController : MonoBehaviour
{
    [Header("复位设置")]
    [Tooltip("角色初始位置（死亡后复位到此位置）。后续关卡可动态更改。")]
    [SerializeField] private Vector3 initialPosition = new Vector3(0f, 1f, 0f);

    [Tooltip("角色初始朝向（Y轴旋转角度）。")]
    [SerializeField] private float initialYaw = 0f;

    [Tooltip("是否在 Start 时自动设置初始位置。")]
    [SerializeField] private bool setInitialPositionOnStart = true;

    [Header("调试")]
    [Tooltip("是否启用调试日志。")]
    [SerializeField] private bool enableDebugLog = true;

    // 组件引用
    private playerControl playerControl;
    private Rigidbody rb;
    private Transform playerTransform;

    // 输入动作
    private InputAction respawnAction;

    // 初始旋转
    private Quaternion initialRotation;

    // 当前是否已死亡（用于防止重复复位）
    private bool isRespawning = false;

    private void Awake()
    {
        // 获取组件引用
        playerTransform = transform;
        rb = GetComponent<Rigidbody>();
        playerControl = GetComponent<playerControl>();

        // 创建复位输入动作（R 键）
        respawnAction = new InputAction(name: "Respawn", type: InputActionType.Button, binding: "<Keyboard>/r");

        // 保存初始旋转
        initialRotation = Quaternion.Euler(0f, initialYaw, 0f);
    }

    private void Start()
    {
        if (setInitialPositionOnStart)
        {
            SetInitialPosition();
        }
    }

    private void OnEnable()
    {
        respawnAction.Enable();
        // 订阅按键事件
        respawnAction.performed += OnRespawnPerformed;
    }

    private void OnDisable()
    {
        respawnAction.Disable();
        respawnAction.performed -= OnRespawnPerformed;
    }

    /// <summary>
    /// 设置初始位置（可在关卡切换时调用）。
    /// </summary>
    public void SetInitialPosition(Vector3 newPosition, float newYaw = -1f)
    {
        initialPosition = newPosition;
        if (newYaw >= 0f)
        {
            initialYaw = newYaw;
            initialRotation = Quaternion.Euler(0f, initialYaw, 0f);
        }

        if (enableDebugLog)
        {
            Debug.Log($"[PlayerRespawn] 初始位置已更新: {initialPosition}, 朝向: {initialYaw}°");
        }
    }

    /// <summary>
    /// 设置初始位置（使用当前 Inspector 配置的值）。
    /// </summary>
    public void SetInitialPosition()
    {
        initialRotation = Quaternion.Euler(0f, initialYaw, 0f);

        if (enableDebugLog)
        {
            Debug.Log($"[PlayerRespawn] 初始位置已设置为: {initialPosition}, 朝向: {initialYaw}°");
        }
    }

    /// <summary>
    /// 复位角色（死亡时调用）。
    /// </summary>
    public void Respawn()
    {
        if (isRespawning)
        {
            return;
        }

        isRespawning = true;

        if (enableDebugLog)
        {
            Debug.Log("[PlayerRespawn] 角色死亡，开始复位...");
        }

        // 1. 停止物理运动
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 2. 重置位置和旋转
        playerTransform.position = initialPosition;
        playerTransform.rotation = initialRotation;

        // 3. 重置玩家控制器内部状态（通过反射或公开方法）
        ResetPlayerControlState();

        // 4. 重置镜头朝向
        ResetCameraOrientation();

        // 延迟一帧重置标志，避免同一帧多次触发
        Invoke(nameof(ResetRespawnFlag), 0.1f);

        if (enableDebugLog)
        {
            Debug.Log("[PlayerRespawn] 角色复位完成");
        }
    }

    /// <summary>
    /// 重置玩家控制器的内部状态。
    /// 由于 playerControl 的部分字段是 private，这里使用反射或扩展方法。
    /// 更好的做法是在 playerControl 中添加一个公共的 ResetState 方法。
    /// </summary>
    private void ResetPlayerControlState()
    {
        if (playerControl == null) return;

        // 方案1：使用反射（如果不想修改原脚本）
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var type = typeof(playerControl);

        // 重置跳跃相关状态
        var jumpQueuedField = type.GetField("jumpQueued", flags);
        if (jumpQueuedField != null) jumpQueuedField.SetValue(playerControl, false);

        var canJumpField = type.GetField("canJump", flags);
        if (canJumpField != null) canJumpField.SetValue(playerControl, true);

        var isGroundedField = type.GetField("isGrounded", flags);
        if (isGroundedField != null) isGroundedField.SetValue(playerControl, true);

        var wasGroundedField = type.GetField("wasGrounded", flags);
        if (wasGroundedField != null) wasGroundedField.SetValue(playerControl, true);

        // 重置冲刺相关状态
        var sprintTimerField = type.GetField("sprintTimer", flags);
        if (sprintTimerField != null) sprintTimerField.SetValue(playerControl, 0f);

        var sprintCooldownTimerField = type.GetField("sprintCooldownTimer", flags);
        if (sprintCooldownTimerField != null) sprintCooldownTimerField.SetValue(playerControl, 0f);

        if (enableDebugLog)
        {
            Debug.Log("[PlayerRespawn] 玩家控制器状态已重置");
        }
    }

    /// <summary>
    /// 重置镜头朝向。
    /// </summary>
    private void ResetCameraOrientation()
    {
        if (playerControl == null) return;

        // 使用反射重置 planarForward 和 pitchAngle
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var type = typeof(playerControl);

        var planarForwardField = type.GetField("planarForward", flags);
        if (planarForwardField != null)
        {
            // 根据初始朝向计算 forward 方向
            Vector3 initialForward = Quaternion.Euler(0f, initialYaw, 0f) * Vector3.forward;
            planarForwardField.SetValue(playerControl, initialForward);
        }

        var pitchAngleField = type.GetField("pitchAngle", flags);
        if (pitchAngleField != null)
        {
            pitchAngleField.SetValue(playerControl, 0f);
        }

        // 调用私有方法 ApplyLookRotation 来应用旋转
        var applyLookMethod = type.GetMethod("ApplyLookRotation", flags);
        if (applyLookMethod != null)
        {
            applyLookMethod.Invoke(playerControl, null);
        }

        if (enableDebugLog)
        {
            Debug.Log("[PlayerRespawn] 镜头朝向已重置");
        }
    }

    private void ResetRespawnFlag()
    {
        isRespawning = false;
    }

    /// <summary>
    /// 按键回调：按 R 键触发死亡复位。
    /// </summary>
    private void OnRespawnPerformed(InputAction.CallbackContext context)
    {
        Respawn();
    }

    /// <summary>
    /// 公共方法：模拟角色死亡（供外部调用，例如碰到陷阱）。
    /// </summary>
    public void KillPlayer()
    {
        if (enableDebugLog)
        {
            Debug.Log("[PlayerRespawn] 角色死亡（外部调用）");
        }
        Respawn();
    }

    // 在 Scene 视图中显示初始位置标记
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(initialPosition, 0.5f);

        // 绘制朝向箭头
        Gizmos.color = Color.blue;
        Vector3 forwardDir = Quaternion.Euler(0f, initialYaw, 0f) * Vector3.forward;
        Gizmos.DrawRay(initialPosition, forwardDir * 1.5f);

        // 绘制标记文字
        UnityEditor.Handles.Label(initialPosition + Vector3.up * 0.8f, "Initial Position");
    }
}