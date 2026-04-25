using UnityEngine;

// TODO: 后续可将 canJump 迁移到状态机(FSM)。
// TODO: 后续可将输入动作迁移到 InputActionAsset。

/// <summary>
/// 玩家基础控制器：
/// 1. 使用 Unity Input System 读取 WASD（2D 方向）输入。
/// 2. 使用 CharacterController 进行运动控制。
/// 3. 使用空格键触发跳跃；地面死亡检测由 ground 相关脚本负责。
///
/// 使用方式：
/// - 将脚本挂到玩家物体上。
/// - 玩家物体必须包含 CharacterController（脚本通过 RequireComponent 强制要求）。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class playerControl : MonoBehaviour
{
    public bool IsSprinting => locomotionRuntime != null && locomotionRuntime.IsSprinting();

    public float CurrentHorizontalSpeed
    {
        get
        {
            if (controller == null)
            {
                return 0f;
            }

            Vector3 horizontalVelocity = controller.velocity;
            horizontalVelocity.y = 0f;
            return horizontalVelocity.magnitude;
        }
    }

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

    [Header("重力参数")]
    [Tooltip("重力加速度（单位：m/s^2）。")]
    [SerializeField] private float gravityAcceleration = 20f;
    [Tooltip("接地时保持轻微向下速度，提升 CharacterController 贴地稳定性。")]
    [SerializeField] private float groundedVerticalVelocity = -2f;

    [Header("测试开关")]
    [Tooltip("地面触发 dead 事件后是否锁定输入。测试阶段可关闭以继续移动。")]
    [SerializeField] private bool lockInputAfterGroundDead = false;

    // CharacterController 用于移动与碰撞。
    private CharacterController controller;

    // 输入读取模块。
    private PlayerInputReader inputReader;

    // 当前帧输入快照。
    private PlayerInputSnapshot inputSnapshot;

    // 当前是否接触地面。
    private bool isGrounded;

    // 当前竖直速度（跳跃/重力均在此累计）。
    private float verticalVelocity;

    // 平台速度跟踪模块。
    private PlatformMotionTracker platformMotion;

    // 角色相对地面的水平速度（由 WASD/冲刺更新，无输入时保持）。
    private Vector3 relativeHorizontalVelocity;

    // 跳跃/冲刺运行时。
    private PlayerLocomotionRuntime locomotionRuntime;

    // 相机与朝向运行时。
    private PlayerLookController lookController;

    // 移动求解运行时。
    private PlayerMovementSolver movementSolver;

    // 关卡控制器（用于监听死亡事件）。
    private levelController levelControllerRef;

    // 死亡后锁定输入读取。
    private bool isInputLockedByDeath;

    // 角色移动状态机（保存当前状态与切换事件）。
    private FiniteStateMachine<PlayerLocomotionState> locomotionFsm;

    // 状态驱动协调器（本阶段默认空节点，后续技能逻辑可按状态挂接）。
    private PlayerLocomotionStateDriver locomotionStateDriver;

    // 暴露给 Inspector/调试窗口观察当前状态。
    [SerializeField] private PlayerLocomotionState debugLocomotionState;

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

        inputReader = new PlayerInputReader();
        inputReader.InitializeDefaultBindings();

        platformMotion = new PlatformMotionTracker();

        lookController = new PlayerLookController(transform, FixedUp);
        lookController.Initialize(lookTarget, Vector3.forward, 0f);
        lookTarget = lookController.LookTarget;

        movementSolver = new PlayerMovementSolver(
            controller,
            platformMotion,
            lookController,
            FixedUp,
            () => controller != null && controller.isGrounded);

        levelControllerRef = FindObjectOfType<levelController>();

        locomotionRuntime = new PlayerLocomotionRuntime();

        // 初始化跳跃状态：若开局即接地，则允许跳跃。
        isGrounded = controller != null && controller.isGrounded;
        verticalVelocity = 0f;
        relativeHorizontalVelocity = Vector3.zero;
        locomotionRuntime.Initialize(isGrounded);

        InitializeLocomotionStateMachine();
    }

    /// <summary>
    /// OnEnable/OnDisable 中启停输入动作，避免无效监听与资源占用。
    /// </summary>
    private void OnEnable()
    {
        inputReader?.Enable();

        if (levelControllerRef != null)
        {
            levelControllerRef.game_dead += OnGameDead;
        }
    }

    private void OnDisable()
    {
        if (levelControllerRef != null)
        {
            levelControllerRef.game_dead -= OnGameDead;
        }

        inputReader?.Disable();
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
        if (isInputLockedByDeath)
        {
            isGrounded = controller != null && controller.isGrounded;
            UpdateLocomotionStateMachine();
            locomotionStateDriver?.Tick(BuildLocomotionFrameContext(), Time.deltaTime);
            return;
        }

        inputSnapshot = inputReader.ReadSnapshot();

        lookController.UpdateFromMouse(inputSnapshot.Look, mouseLookSensitivity, minPitch, maxPitch);

        isGrounded = controller != null && controller.isGrounded;
        PlayerLocomotionState preMoveState = ResolveLocomotionState();
        locomotionRuntime.UpdateBeforeMovement(
            preMoveState,
            verticalVelocity,
            inputSnapshot.JumpPressedThisFrame,
            inputSnapshot.SprintPressed,
            sprintDuration,
            sprintCooldown,
            Time.deltaTime);

        UpdateMovement();
        UpdateLocomotionStateMachine();

        // 状态节点逐帧入口：本阶段默认空节点，不影响现有行为。
        locomotionStateDriver?.Tick(BuildLocomotionFrameContext(), Time.deltaTime);
    }

    private void InitializeLocomotionStateMachine()
    {
        locomotionFsm = new FiniteStateMachine<PlayerLocomotionState>();
        locomotionFsm.OnStateChanged += OnLocomotionStateChanged;

        PlayerLocomotionState initialState = ResolveLocomotionState();
        locomotionFsm.Initialize(initialState);

        locomotionStateDriver = PlayerLocomotionStateDriver.CreateDefaultNoopDriver();
        locomotionStateDriver.Initialize(initialState, BuildLocomotionFrameContext());

        debugLocomotionState = initialState;
    }

    private void UpdateLocomotionStateMachine()
    {
        if (locomotionFsm == null)
        {
            return;
        }

        PlayerLocomotionState nextState = ResolveLocomotionState();
        locomotionFsm.ChangeState(nextState);
        debugLocomotionState = locomotionFsm.CurrentState;
    }

    private PlayerLocomotionState ResolveLocomotionState()
    {
        if (isInputLockedByDeath)
        {
            return PlayerLocomotionState.Dead;
        }

        if (!isGrounded)
        {
            return PlayerLocomotionState.Airborne;
        }

        if (platformMotion != null && platformMotion.CurrentPlatform != null)
        {
            return PlayerLocomotionState.OnPlatform;
        }

        return PlayerLocomotionState.Grounded;
    }

    private void OnGameDead()
    {
        if (!lockInputAfterGroundDead)
        {
            return;
        }

        isInputLockedByDeath = true;
        inputSnapshot = default;
    }

    private void OnLocomotionStateChanged(PlayerLocomotionState previousState, PlayerLocomotionState nextState)
    {
        locomotionStateDriver?.ChangeState(nextState, BuildLocomotionFrameContext());
    }

    private PlayerLocomotionFrameContext BuildLocomotionFrameContext()
    {
        return new PlayerLocomotionFrameContext
        {
            IsGrounded = isGrounded,
            IsSprinting = locomotionRuntime != null && locomotionRuntime.IsSprinting(),
            HasPlatform = platformMotion != null && platformMotion.CurrentPlatform != null,
            MoveInput = inputSnapshot.Move,
        };
    }

    /// <summary>
    /// CharacterController 运动更新：
    /// - 水平速度由输入直接决定
    /// - 竖直速度由重力和跳跃维护
    /// - 每帧通过 Move 推进位移
    /// </summary>
    private void UpdateMovement()
    {
        movementSolver.Step(
            inputSnapshot,
            locomotionRuntime,
            speed,
            sprintMultiplier,
            gravityAcceleration,
            groundedVerticalVelocity,
            jumpSpeed,
            Time.deltaTime,
            ref isGrounded,
            ref verticalVelocity,
            ref relativeHorizontalVelocity);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit == null || hit.collider == null)
        {
            return;
        }

        // ground 由独立脚本处理死亡判定，不应参与平台继承速度。
        if (hit.collider.GetComponent<ground>() != null)
        {
            return;
        }

        platformMotion.RegisterGroundHit(hit, FixedUp);
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
        gravityAcceleration = configAsset.gravityAcceleration;
        groundedVerticalVelocity = configAsset.groundedVerticalVelocity;
    }

    /// <summary>
    /// 在 Scene 视图中显示地面检测球和方向辅助线。
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 辅助显示当前固定 up 与前进方向，便于验证方向约束是否正确。
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + FixedUp * 1.2f);
        Gizmos.color = Color.yellow;
        Vector3 debugForward = lookController != null ? lookController.PlanarForward : Vector3.forward;
        Gizmos.DrawLine(transform.position, transform.position + debugForward * 1.2f);
    }
}
