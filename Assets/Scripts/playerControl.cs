using UnityEngine;
using UnityEngine.InputSystem;

// TODO: 后续可将 canJump 迁移到状态机(FSM)。
// TODO: 后续可将输入动作迁移到 InputActionAsset。

/// <summary>
/// 玩家基础控制器：
/// 1. 使用 Unity Input System 读取 WASD（2D 方向）输入。
/// 2. 使用 CharacterController 进行运动控制。
/// 3. 使用空格键触发跳跃，并通过地面检测限制连跳。
///
/// 使用方式：
/// - 将脚本挂到玩家物体上。
/// - 玩家物体必须包含 CharacterController（脚本通过 RequireComponent 强制要求）。
/// - 推荐在 Inspector 中配置 groundCheckPoint 与 groundMask，提升地面检测稳定性。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class playerControl : MonoBehaviour
{
    [SerializeField] private player_config configAsset;

    // 按需求固定世界上方向为 Y 轴正方向，且与重力方向相反。
    private static readonly Vector3 FixedUp = new Vector3(0f, 1f, 0f);

    [Header("移动参数")]
    [Tooltip("玩家常规移动速度。")]
    [SerializeField] private float speed = 6f;

    [Tooltip("冲刺速度倍率（冲刺速度 = speed × sprintMultiplier）。")]
    [SerializeField] private float sprintMultiplier = 1.6f;
    [Tooltip("判定为前进的最小输入阈值。只有输入前进量大于该值时才允许冲刺。")]
    [SerializeField] private float sprintForwardThreshold = 0.1f;
    [Tooltip("冲刺持续时间（秒）。")]
    [SerializeField] private float sprintDuration = 0.25f;
    [Tooltip("冲刺冷却时间（秒）。冷却期间无法再次冲刺。")]
    [SerializeField] private float sprintCooldown = 0.8f;

    [Header("镜头/朝向参数")]
    [Tooltip("鼠标水平/垂直位移影响视角旋转速度（无需按住鼠标键）。")]
    [SerializeField] private float mouseLookSensitivity = 0.15f;
    [Tooltip("用于俯仰的相机挂点（例如 Camera）。为空则不会应用俯仰。")]
    [SerializeField] private Transform lookTarget;
    [Tooltip("俯仰角最小值（向下）。")]
    [SerializeField] private float minPitch = -75f;
    [Tooltip("俯仰角最大值（向上）。")]
    [SerializeField] private float maxPitch = 75f;

    [Header("跳跃参数")]
    [Tooltip("玩家跳跃速度（使用 VelocityChange 直接赋予）。")]
    [SerializeField] private float jumpSpeed = 7f;

    [Header("地面检测")]
    [Tooltip("地面检测点。一般放在角色脚底。为空时会回退使用角色位置附近检测。")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("重力参数")]
    [Tooltip("重力加速度（单位：m/s^2）。")]
    [SerializeField] private float gravityAcceleration = 20f;
    [Tooltip("接地时保持轻微向下速度，提升 CharacterController 贴地稳定性。")]
    [SerializeField] private float groundedVerticalVelocity = -2f;

    // CharacterController 用于移动与碰撞。
    private CharacterController controller;

    // Move: 读取二维方向输入（x=左右, y=前后）。
    private InputAction moveAction;

    // Jump: 读取跳跃按键（空格）。
    private InputAction jumpAction;

    // Sprint: 读取冲刺按键（左 Shift）。
    private InputAction sprintAction;

    // Look: 读取鼠标位移（delta）。
    private InputAction lookAction;

    // 是否在当前物理帧执行跳跃。用于把 Update 的按键触发同步到 FixedUpdate 里处理。
    private bool jumpQueued;

    // 当前是否接触地面。
    private bool isGrounded;

    // 按文档语义：是否允许跳跃。落地瞬间刷新 true，按下空格后置 false。
    private bool canJump;

    // 上一帧是否接地，用于检测“空中 -> 接地”的边沿。
    private bool wasGrounded;

    // 当前鼠标朝向对应的 forward，始终与 FixedUp 垂直。
    private Vector3 planarForward;

    // 当前俯仰角（单位：度）。
    private float pitchAngle;

    // 冲刺剩余持续时间。
    private float sprintTimer;

    // 冲刺冷却剩余时间。
    private float sprintCooldownTimer;

    // 当前竖直速度（跳跃/重力均在此累计）。
    private float verticalVelocity;

    // 当前脚下平台。
    private Transform currentPlatform;

    // 上一帧平台位置，用于估算平台速度。
    private Vector3 lastPlatformPosition;

    // 当前平台速度（世界空间）。
    private Vector3 platformVelocity;

    // 角色离地瞬间继承的平台速度（空中持续使用）。
    private Vector3 inheritedPlatformVelocity;

    // 角色相对地面的水平速度（由 WASD/冲刺更新，无输入时保持）。
    private Vector3 relativeHorizontalVelocity;

    // 本帧 Move 过程中检测到的脚下平台。
    private Transform detectedPlatformThisFrame;

    // 由脚本管理的相机组件。
    private Camera controlledCamera;

    /// <summary>
    /// Awake 在脚本生命周期中最早执行：
    /// - 缓存 CharacterController
    /// - 尝试从 ScriptableObject 读取参数（未配置时回退默认值）
    /// - 创建 InputAction（代码方式，不依赖 InputActions 资产）
    /// </summary>
    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        ApplyConfigAsset();

        // Move 动作：使用 2DVector 复合绑定，将 WASD 映射到一个 Vector2。
        moveAction = new InputAction(name: "Move", type: InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        // Jump 动作：空格键触发。
        jumpAction = new InputAction(name: "Jump", type: InputActionType.Button, binding: "<Keyboard>/space");

        // Sprint 动作：按住左 Shift 冲刺。
        sprintAction = new InputAction(name: "Sprint", type: InputActionType.Button, binding: "<Keyboard>/leftShift");

        // Look 动作：鼠标位移。
        lookAction = new InputAction(name: "Look", type: InputActionType.Value, binding: "<Mouse>/delta");

        // 按文档要求：初始时强制角色朝向世界 Z 轴正方向。
        planarForward = Vector3.forward;
        pitchAngle = 0f;

        // 自动确保相机挂点与 Camera 组件可用。
        EnsureCameraSetup();

        // 初始化跳跃状态：若开局即接地，则允许跳跃。
        isGrounded = CheckGrounded();
        wasGrounded = isGrounded;
        canJump = isGrounded;
        verticalVelocity = isGrounded ? groundedVerticalVelocity : 0f;
        relativeHorizontalVelocity = Vector3.zero;
        inheritedPlatformVelocity = Vector3.zero;

        // 应用一次初始朝向（玩家朝向 +Z，相机俯仰归零）。
        ApplyLookRotation();
    }

    /// <summary>
    /// OnEnable/OnDisable 中启停输入动作，避免无效监听与资源占用。
    /// </summary>
    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        sprintAction.Enable();
        lookAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        sprintAction.Disable();
        lookAction.Disable();
    }

    private bool canSprint()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        return isGrounded
            && sprintCooldownTimer <= 0f
            && sprintTimer <= 0f
            && input.y > sprintForwardThreshold;
    }

    private void update_sprint_status()
    {
        // 维护冲刺持续与冷却计时。
        if (sprintTimer > 0f)
        {
            sprintTimer -= Time.deltaTime;
        }
        if (sprintCooldownTimer > 0f)
        {
            sprintCooldownTimer -= Time.deltaTime;
        }

        // 冲刺触发条件：
        // 1) 按住 Shift；2) 当前接地（空中不可冲刺）；3) 不在冷却；4) 不在持续中；5) 前进输入大于阈值。
        if (canSprint() && sprintAction.IsPressed())
        {
            sprintTimer = sprintDuration;
            sprintCooldownTimer = sprintCooldown;
        }
    }

    private void update_jump_status()
    {
        isGrounded = CheckGrounded();

        // 只要稳定接地，就允许下一次跳跃，避免开局或落地后无法起跳。
        if (isGrounded && verticalVelocity <= 0.01f)
        {
            canJump = true;
        }

        // 仅在“空中 -> 接地”边沿刷新跳跃资格。
        if (isGrounded && !wasGrounded)
        {
            canJump = true;
        }
        wasGrounded = isGrounded;

        // WasPressedThisFrame 用于“按下瞬间”触发，避免长按重复入队。
        if (canJump && jumpAction.WasPressedThisFrame())
        {
            jumpQueued = true;
            canJump = false;
        }

    }

    /// <summary>
    /// Update（逐帧）负责：
    /// - 刷新地面状态与 canJump
    /// - 维护冲刺持续/冷却计时
    /// - 捕捉“本帧按下跳跃键”事件
    /// - 使用 CharacterController 执行移动
    /// </summary>
    private void Update()
    {
        UpdateLookDirectionFromMouse();

        update_jump_status();
        update_sprint_status();

        UpdateMovement();
    }

    /// <summary>
    /// CharacterController 运动更新：
    /// - 水平速度由输入直接决定
    /// - 竖直速度由重力和跳跃维护
    /// - 每帧通过 Move 推进位移
    /// </summary>
    private void UpdateMovement()
    {
        bool wasGroundedBeforeMove = isGrounded;

        // 重置本帧检测结果，等待 OnControllerColliderHit 更新。
        detectedPlatformThisFrame = null;

        // 若角色稳定站在平台上，则将平台速度叠加到角色最终位移中。
        UpdatePlatformVelocity();

        // 读取 WASD 合成方向。
        Vector2 input = moveAction.ReadValue<Vector2>();

        // 按文档公式：
        // right = forward × up
        // W 对应方向：moveForward = up × right
        Vector3 right = Vector3.Cross(planarForward, FixedUp).normalized;
        Vector3 moveForward = Vector3.Cross(FixedUp, right).normalized;

        // 保持 WASD 直觉：W/S 使用 moveForward，A/D 使用 -right。
        Vector3 rawMove = moveForward * input.y - right * input.x;
        Vector3 moveDir = rawMove.sqrMagnitude > 0.0001f ? rawMove.normalized : Vector3.zero;

        // 处于冲刺持续窗口时，使用冲刺倍率。
        bool isSprinting = sprintTimer > 0f;
        float finalMoveSpeed = isSprinting ? speed * sprintMultiplier : speed;

        // 速度机制：
        // 1) 有输入时，relative speed 由输入确定；
        // 2) 地面无输入时，relative speed = 0（仅继承平台速度）；
        // 3) 空中无输入时，relative speed 保持离地时值。
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            relativeHorizontalVelocity = moveDir * finalMoveSpeed;
        }
        else if (isGrounded)
        {
            relativeHorizontalVelocity = Vector3.zero;
        }

        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedVerticalVelocity;
        }

        // 跳跃触发：直接写入起跳竖直速度。
        if (jumpQueued)
        {
            jumpQueued = false;
            verticalVelocity = jumpSpeed;
        }

        verticalVelocity -= gravityAcceleration * Time.deltaTime;

        Vector3 effectivePlatformVelocity = isGrounded ? platformVelocity : inheritedPlatformVelocity;
        Vector3 frameMotion = (relativeHorizontalVelocity + effectivePlatformVelocity) * Time.deltaTime
            + FixedUp * (verticalVelocity * Time.deltaTime);

        CollisionFlags flags = controller.Move(frameMotion);
        if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < groundedVerticalVelocity)
        {
            verticalVelocity = groundedVerticalVelocity;
        }

        isGrounded = CheckGrounded();

        // 若本帧从接地变为空中，则锁存离地瞬间的平台速度，供空中继承。
        if (wasGroundedBeforeMove && !isGrounded)
        {
            inheritedPlatformVelocity = platformVelocity;
        }

        // Move 后根据碰撞信息更新“当前脚下平台”。
        RefreshCurrentPlatform();
    }

    private void UpdatePlatformVelocity()
    {
        if (!isGrounded || currentPlatform == null)
        {
            platformVelocity = Vector3.zero;
            return;
        }

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 currentPosition = currentPlatform.position;
        platformVelocity = (currentPosition - lastPlatformPosition) / dt;
        lastPlatformPosition = currentPosition;

        // 接地期间持续刷新，保证下一次离地时有最新可继承速度。
        inheritedPlatformVelocity = platformVelocity;
    }

    private void RefreshCurrentPlatform()
    {
        if (!isGrounded || detectedPlatformThisFrame == null)
        {
            currentPlatform = null;
            platformVelocity = Vector3.zero;
            return;
        }

        if (currentPlatform != detectedPlatformThisFrame)
        {
            currentPlatform = detectedPlatformThisFrame;
            lastPlatformPosition = currentPlatform.position;
            platformVelocity = Vector3.zero;
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // 仅在脚下碰撞时记录平台，避免侧碰墙体干扰平台判定。
        if (Vector3.Dot(hit.normal, FixedUp) > 0.5f)
        {
            detectedPlatformThisFrame = hit.transform;
        }
    }

    /// <summary>
    /// 地面检测：
    /// - 优先使用 groundCheckPoint 作为球体检测中心。
    /// - 未配置时使用角色位置下方少量偏移作为回退方案。
    /// </summary>
    private bool CheckGrounded()
    {
        bool controllerGrounded = controller != null && controller.isGrounded;

        if (groundCheckPoint != null)
        {
            bool sphereGrounded = Physics.CheckSphere(groundCheckPoint.position, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
            return controllerGrounded || sphereGrounded;
        }

        // 回退方案：未指定检测点时，基于 CharacterController 底部做检测。
        if (controller != null)
        {
            float checkOffset = controller.height * 0.5f - controller.radius + controller.skinWidth + 0.02f;
            Vector3 checkPos = transform.position - FixedUp * checkOffset;
            float radius = Mathf.Max(groundCheckRadius, controller.radius * 0.95f);
            bool sphereGrounded = Physics.CheckSphere(checkPos, radius, groundMask, QueryTriggerInteraction.Ignore);
            return controllerGrounded || sphereGrounded;
        }

        // 最后兜底：无碰撞体时仍使用原始偏移。
        Vector3 fallbackPos = transform.position - FixedUp * 0.9f;
        bool fallbackGrounded = Physics.CheckSphere(fallbackPos, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
        return controllerGrounded || fallbackGrounded;
    }

    private void ApplyConfigAsset()
    {
        if (configAsset == null)
        {
            Debug.LogWarning("[playerControl] 未绑定 player_config 资产，将使用脚本默认值。");
            return;
        }

        speed = configAsset.speed;
        sprintMultiplier = configAsset.sprintMultiplier;
        sprintForwardThreshold = configAsset.sprintForwardThreshold;
        sprintDuration = configAsset.sprintDuration;
        sprintCooldown = configAsset.sprintCooldown;
        mouseLookSensitivity = configAsset.mouseLookSensitivity;
        minPitch = configAsset.minPitch;
        maxPitch = configAsset.maxPitch;
        jumpSpeed = configAsset.jumpSpeed;
        groundCheckRadius = configAsset.groundCheckRadius;
        gravityAcceleration = configAsset.gravityAcceleration;
        groundedVerticalVelocity = configAsset.groundedVerticalVelocity;
        groundMask = configAsset.groundMask;
    }

    /// <summary>
    /// 每帧根据鼠标位移更新视角：
    /// - up 固定为 (0,1,0)
    /// - 水平位移控制 yaw（影响移动前进方向）
    /// - 垂直位移控制 pitch（只影响视角俯仰）
    /// </summary>
    private void UpdateLookDirectionFromMouse()
    {
        Vector2 mouseDelta = lookAction.ReadValue<Vector2>();
        float yawDelta = mouseDelta.x * mouseLookSensitivity;
        float pitchDelta = -mouseDelta.y * mouseLookSensitivity;

        if (Mathf.Abs(yawDelta) <= Mathf.Epsilon && Mathf.Abs(pitchDelta) <= Mathf.Epsilon)
        {
            return;
        }

        // 水平转向：更新平面前进方向。
        planarForward = Quaternion.AngleAxis(yawDelta, FixedUp) * planarForward;
        planarForward = Vector3.ProjectOnPlane(planarForward, FixedUp).normalized;
        if (planarForward.sqrMagnitude < 0.0001f)
        {
            planarForward = Vector3.right;
        }

        // 垂直俯仰：更新并限制 pitch 角度。
        pitchAngle = Mathf.Clamp(pitchAngle + pitchDelta, minPitch, maxPitch);

        ApplyLookRotation();
    }

    /// <summary>
    /// 确保相机挂点与 Camera 组件存在：
    /// - 若未指定 lookTarget，自动创建子物体 Camera。
    /// - 在 lookTarget 上获取 Camera；若不存在则自动添加。
    /// </summary>
    private void EnsureCameraSetup()
    {
        if (lookTarget == null)
        {
            Transform existingPivot = transform.Find("Camera");
            if (existingPivot != null)
            {
                lookTarget = existingPivot;
            }
            else
            {
                GameObject pivot = new GameObject("Camera");
                lookTarget = pivot.transform;
                lookTarget.SetParent(transform, false);
                lookTarget.localPosition = Vector3.zero;
                lookTarget.localRotation = Quaternion.identity;
            }
        }

        controlledCamera = lookTarget.GetComponent<Camera>();
        if (controlledCamera == null)
        {
            controlledCamera = lookTarget.gameObject.AddComponent<Camera>();
        }
    }

    /// <summary>
    /// 应用旋转：
    /// - 玩家本体只应用 yaw（水平旋转），避免刚体碰撞体发生俯仰导致被物理顶飞。
    /// - 相机挂点（lookTarget）只应用 pitch（上下俯仰）。
    /// </summary>
    private void ApplyLookRotation()
    {
        Quaternion yawRotation = Quaternion.LookRotation(planarForward, FixedUp);

        // 玩家本体只做水平旋转。
        transform.rotation = yawRotation;

        // 俯仰只给相机挂点，避免影响玩家刚体。
        if (lookTarget != null)
        {
            Quaternion pitchLocalRotation = Quaternion.Euler(pitchAngle, 0f, 0f);
            lookTarget.localRotation = pitchLocalRotation;

            // 显式同步 Camera 旋转：由 yaw(玩家) + pitch(挂点) 组成最终视角。
            if (controlledCamera != null)
            {
                controlledCamera.transform.rotation = yawRotation * pitchLocalRotation;
            }
        }
    }

    /// <summary>
    /// 在 Scene 视图中显示地面检测球和方向辅助线。
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 checkPos = groundCheckPoint != null
            ? groundCheckPoint.position
            : transform.position - FixedUp * 0.9f;
        Gizmos.DrawWireSphere(checkPos, groundCheckRadius);

        // 辅助显示当前固定 up 与前进方向，便于验证方向约束是否正确。
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + FixedUp * 1.2f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + planarForward * 1.2f);
    }
}
