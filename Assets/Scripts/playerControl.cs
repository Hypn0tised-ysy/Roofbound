using UnityEngine;
using UnityEngine.Rendering;

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

    public bool IsAirDashing => airDashTimer > 0f;
    public float AirDashFovDelta => airDashFovDelta;
    public string CurrentMovementSkillName => movementSkillName;
    public string CurrentUtilitySkillName => utilitySkillName;

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

    // Sprint cooldown info exposed for UI
    public float SprintCooldownRemaining
    {
        get
        {
            return locomotionRuntime != null ? locomotionRuntime.SprintCooldownTimer : 0f;
        }
    }

    public float SprintCooldownDuration
    {
        get { return sprintCooldown; }
    }

    // Expose jump qualification for UI/debug
    public bool CanJump
    {
        get { return locomotionRuntime != null && locomotionRuntime.CanJump; }
    }

    [SerializeField] public player_config configAsset;

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
    [Tooltip("从平台离开进入空中后，仍允许起跳的宽限时间（秒）。")]
    [SerializeField] private float airborneJumpGraceDuration = 0.12f;

    [Header("技能选择")]
    [SerializeField] private bool useSkillOverrides = true;
    [SerializeField] private MovementSkillId overrideMovementSkillId = MovementSkillId.None;
    [SerializeField] private UtilitySkillId overrideUtilitySkillId = UtilitySkillId.None;

    [Header("技能参数来源")]
    [SerializeField] private PlayerSkillParameters skillParameters;
    [SerializeField] private Volume timeSlowVolume;
    [SerializeField] private GameObject timeSlowVolumePrefab;

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
    private bool isDead;

    private MovementSkillId movementSkillId = MovementSkillId.None;
    private UtilitySkillId utilitySkillId = UtilitySkillId.None;
    private string movementSkillName = "None";
    private string utilitySkillName = "None";

    private float airDashSpeed;
    private float airDashDuration;
    private float airDashCooldown;
    private float airDashFovDelta;

    private float slowTimeScale;
    private float slowTimeFixedDeltaScale;
    private float slowTimeVolumeTargetWeight;
    private float slowTimeVolumeLerpSpeed;
    private float slowTimeMaxDuration;
    private float slowTimeCooldown;

    private float jetPackUpSpeed;
    private float jetPackMaxUpTime;
    private float jetPackCooldown;

    private float levitationMaxTime;
    private float levitationCooldown;

    private float teleportRange;
    private float teleportHeight;
    private float teleportCooldown;

    private float freezeDuration;
    private float freezeCooldown;

    private float airDashTimer;
    private float airDashCooldownTimer;
    private bool airDashAvailable;
    private Vector3 airDashDirection;

    private float jetPackTimer;
    private float jetPackCooldownTimer;

    private float levitationTimer;
    private float levitationCooldownTimer;
    private bool levitationActive;

    private float teleportCooldownTimer;

    private float freezeTimer;
    private float freezeCooldownTimer;
    private bool isFreezingTrucks;

    private float slowTimeTimer;
    private float slowTimeCooldownTimer;

    private float baseFixedDeltaTime;

    // 角色移动状态机（保存当前状态与切换事件）。
    private FiniteStateMachine<PlayerLocomotionState> locomotionFsm;

    // 状态驱动协调器（本阶段默认空节点，后续技能逻辑可按状态挂接）。
    private PlayerLocomotionStateDriver locomotionStateDriver;

    // 暴露给 Inspector/调试窗口观察当前状态。
    [SerializeField] public PlayerLocomotionState debugLocomotionState;

    /// <summary>
    /// Awake 在脚本生命周期中最早执行：
    /// - 缓存 CharacterController
    /// - 尝试从 ScriptableObject 读取参数（未配置时回退默认值）
    /// - 创建 InputAction（代码方式，不依赖 InputActions 资产）
    /// </summary>
    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        baseFixedDeltaTime = Time.fixedDeltaTime;

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

        ApplySkillParameters();

        LoadSkillSelection();
        ResolveSkillOverrides();

        // 初始化跳跃状态：若开局即接地，则允许跳跃。
        isGrounded = controller != null && controller.isGrounded;
        verticalVelocity = 0f;
        relativeHorizontalVelocity = Vector3.zero;
        locomotionRuntime.Initialize(isGrounded);

        airDashAvailable = true;
        if (timeSlowVolume == null)
        {
            GameObject volumeObject = GameObject.Find("TimeSlowVolume");
            if (volumeObject != null)
            {
                timeSlowVolume = volumeObject.GetComponent<Volume>();
            }
        }

        if (utilitySkillId == UtilitySkillId.SlowTime)
        {
            EnsureTimeSlowVolume();
        }

        InitializeLocomotionStateMachine();
    }

    /// <summary>
    /// OnEnable/OnDisable 中启停输入动作，避免无效监听与资源占用。
    /// </summary>
    private void OnEnable()
    {
        inputReader?.Enable();

        SkillSelectionStore.SelectionChanged += OnSkillSelectionChanged;

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

        SkillSelectionStore.SelectionChanged -= OnSkillSelectionChanged;

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

        UpdateMovementSkill(preMoveState, Time.deltaTime);
        UpdateUtilitySkill(Time.deltaTime);

        bool allowExtraJump = movementSkillId == MovementSkillId.DoubleJump && !isDead && !IsMenuPaused();
        locomotionRuntime.UpdateBeforeMovement(
            preMoveState,
            verticalVelocity,
            inputSnapshot.JumpPressedThisFrame,
            inputSnapshot.SprintPressed,
            sprintDuration,
            sprintCooldown,
            airborneJumpGraceDuration,
            allowExtraJump,
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
        isDead = true;
        ResetSlowTime();

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
        PlayerInputSnapshot movementSnapshot = inputSnapshot;
        if (IsAirDashing || levitationActive)
        {
            movementSnapshot.Move = Vector2.zero;
        }

        float gravity = levitationActive ? 0f : gravityAcceleration;

        movementSolver.Step(
            movementSnapshot,
            locomotionRuntime,
            speed,
            sprintMultiplier,
            gravity,
            groundedVerticalVelocity,
            jumpSpeed,
            Time.deltaTime,
            ref isGrounded,
            ref verticalVelocity,
            ref relativeHorizontalVelocity);
    }

    private void UpdateMovementSkill(PlayerLocomotionState preMoveState, float deltaTime)
    {
        ApplySkillParameters();

        switch (movementSkillId)
        {
            case MovementSkillId.AirDash:
                UpdateAirDash(preMoveState, deltaTime);
                break;
            case MovementSkillId.JetPack:
                UpdateJetPack(preMoveState, deltaTime);
                break;
            case MovementSkillId.Levitation:
                UpdateLevitation(preMoveState, deltaTime);
                break;
            case MovementSkillId.Teleport:
                UpdateTeleport(preMoveState, deltaTime);
                break;
            default:
                airDashTimer = 0f;
                levitationActive = false;
                break;
        }
    }

    private void UpdateUtilitySkill(float deltaTime)
    {
        ApplySkillParameters();

        switch (utilitySkillId)
        {
            case UtilitySkillId.SlowTime:
                UpdateSlowTime();
                break;
            case UtilitySkillId.FreezeTrucks:
                UpdateFreezeTrucks(deltaTime);
                break;
            default:
                ResetSlowTime();
                UpdateFreezeTrucksCleanup();
                break;
        }
    }

    private void UpdateAirDash(PlayerLocomotionState preMoveState, float deltaTime)
    {
        ApplySkillParameters();

        if (movementSkillId != MovementSkillId.AirDash || isDead || IsMenuPaused())
        {
            airDashTimer = 0f;
            airDashCooldownTimer = 0f;
            airDashAvailable = false;
            return;
        }

        if (preMoveState != PlayerLocomotionState.Airborne)
        {
            airDashAvailable = true;
            airDashTimer = 0f;
        }

        if (airDashCooldownTimer > 0f)
        {
            airDashCooldownTimer = Mathf.Max(0f, airDashCooldownTimer - deltaTime);
        }

        if (airDashTimer > 0f)
        {
            airDashTimer = Mathf.Max(0f, airDashTimer - deltaTime);
            relativeHorizontalVelocity = airDashDirection * airDashSpeed;
            return;
        }

        bool canStartDash = preMoveState == PlayerLocomotionState.Airborne
            && airDashAvailable
            && airDashCooldownTimer <= 0f
            && inputSnapshot.PrimarySkillPressedThisFrame;

        if (!canStartDash)
        {
            return;
        }

        airDashAvailable = false;
        airDashTimer = airDashDuration;
        airDashCooldownTimer = airDashCooldown;
        airDashDirection = ResolveAirDashDirection();
        relativeHorizontalVelocity = airDashDirection * airDashSpeed;
    }

    private Vector3 ResolveAirDashDirection()
    {
        Vector2 input = inputSnapshot.Move;
        Vector3 right = Vector3.Cross(lookController.PlanarForward, FixedUp).normalized;
        Vector3 moveForward = Vector3.Cross(FixedUp, right).normalized;

        Vector3 rawMove = moveForward * input.y - right * input.x;
        if (rawMove.sqrMagnitude > 0.0001f)
        {
            return rawMove.normalized;
        }

        return lookController.PlanarForward;
    }

    private void UpdateSlowTime()
    {
        if (utilitySkillId != UtilitySkillId.SlowTime || isDead || IsMenuPaused())
        {
            ResetSlowTime();
            return;
        }

        if (slowTimeCooldownTimer > 0f)
        {
            slowTimeCooldownTimer = Mathf.Max(0f, slowTimeCooldownTimer - Time.deltaTime);
        }

        if (slowTimeTimer <= 0f && slowTimeCooldownTimer <= 0f)
        {
            slowTimeTimer = slowTimeMaxDuration;
        }

        if (inputSnapshot.SlowTimeHeld && slowTimeCooldownTimer <= 0f && slowTimeTimer > 0f)
        {
            slowTimeTimer = Mathf.Max(0f, slowTimeTimer - Time.deltaTime);
            ApplySlowTime();

            if (slowTimeTimer <= 0f)
            {
                slowTimeCooldownTimer = slowTimeCooldown;
            }
        }
        else
        {
            if (slowTimeTimer < slowTimeMaxDuration && slowTimeCooldownTimer <= 0f)
            {
                slowTimeCooldownTimer = slowTimeCooldown;
            }

            ResetSlowTime();
        }
    }

    private void ApplySlowTime()
    {
        Time.timeScale = slowTimeScale;
        Time.fixedDeltaTime = baseFixedDeltaTime * slowTimeFixedDeltaScale;
        UpdateSlowTimeVolume(slowTimeVolumeTargetWeight);
    }

    private void ResetSlowTime()
    {
        UpdateSlowTimeVolume(0f);

        if (IsMenuPaused())
        {
            return;
        }

        Time.timeScale = 1f;
        Time.fixedDeltaTime = baseFixedDeltaTime;
    }

    private void UpdateSlowTimeVolume(float targetWeight)
    {
        if (timeSlowVolume == null)
        {
            return;
        }

        float step = slowTimeVolumeLerpSpeed * Time.unscaledDeltaTime;
        timeSlowVolume.weight = Mathf.MoveTowards(timeSlowVolume.weight, targetWeight, step);
    }

    private bool IsMenuPaused()
    {
        return UI_Controller.Instance != null && UI_Controller.Instance.IsMenuPaused;
    }

    private void LoadSkillSelection()
    {
        SkillSelectionData data = SkillSelectionStore.Load();
        ApplySkillSelection(data);
    }

    private void OnSkillSelectionChanged(SkillSelectionData data)
    {
        ApplySkillSelection(data);
    }

    private void ApplySkillSelection(SkillSelectionData data)
    {
        if (data == null)
        {
            movementSkillName = "None";
            utilitySkillName = "None";
            movementSkillId = MovementSkillId.None;
            utilitySkillId = UtilitySkillId.None;
            return;
        }

        movementSkillName = string.IsNullOrEmpty(data.movementSkillId) ? "None" : data.movementSkillId;
        utilitySkillName = string.IsNullOrEmpty(data.utilitySkillId) ? "None" : data.utilitySkillId;
        movementSkillId = SkillSelectionStore.ResolveMovementId(movementSkillName);
        utilitySkillId = SkillSelectionStore.ResolveUtilityId(utilitySkillName);

        if (utilitySkillId == UtilitySkillId.SlowTime)
        {
            EnsureTimeSlowVolume();
        }
    }

    private void ResolveSkillOverrides()
    {
        if (!useSkillOverrides)
        {
            Debug.Log("[playerControl] 技能覆盖已关闭，使用存档的技能选择。");
            return;
        }

        Debug.Log($"[playerControl] 使用技能覆盖：移动技能={overrideMovementSkillId}, 实用技能={overrideUtilitySkillId}");
        movementSkillId = overrideMovementSkillId;
        utilitySkillId = overrideUtilitySkillId;
        movementSkillName = overrideMovementSkillId.ToString();
        utilitySkillName = overrideUtilitySkillId.ToString();

        if (utilitySkillId == UtilitySkillId.SlowTime)
        {
            Debug.Log("[playerControl] 实用技能覆盖为 SlowTime，确保 TimeSlowVolume 已准备就绪。");
            EnsureTimeSlowVolume();
        }
    }

    private void ApplySkillParameters()
    {
        if (skillParameters == null)
        {
            return;
        }

        airDashSpeed = skillParameters.airDashSpeed;
        airDashDuration = skillParameters.airDashDuration;
        airDashCooldown = skillParameters.airDashCooldown;
        airDashFovDelta = skillParameters.airDashFovDelta;

        slowTimeScale = skillParameters.slowTimeScale;
        slowTimeFixedDeltaScale = skillParameters.slowTimeFixedDeltaScale;
        slowTimeVolumeTargetWeight = skillParameters.slowTimeVolumeTargetWeight;
        slowTimeVolumeLerpSpeed = skillParameters.slowTimeVolumeLerpSpeed;
        slowTimeMaxDuration = skillParameters.slowTimeMaxDuration;
        slowTimeCooldown = skillParameters.slowTimeCooldown;

        jetPackUpSpeed = skillParameters.jetPackUpSpeed;
        jetPackMaxUpTime = skillParameters.jetPackMaxUpTime;
        jetPackCooldown = skillParameters.jetPackCooldown;

        levitationMaxTime = skillParameters.levitationMaxTime;
        levitationCooldown = skillParameters.levitationCooldown;

        teleportRange = skillParameters.teleportRange;
        teleportHeight = skillParameters.teleportHeight;
        teleportCooldown = skillParameters.teleportCooldown;

        freezeDuration = skillParameters.freezeDuration;
        freezeCooldown = skillParameters.freezeCooldown;
    }

    public bool TryGetMovementTimerStatus(out float ratio, out bool isActiveOrCooling)
    {
        ratio = 0f;
        isActiveOrCooling = false;

        switch (movementSkillId)
        {
            case MovementSkillId.AirDash:
                if (airDashTimer > 0f && airDashDuration > 0f)
                {
                    ratio = Mathf.Clamp01(airDashTimer / airDashDuration);
                    isActiveOrCooling = true;
                    return true;
                }

                if (airDashCooldown > 0f && airDashCooldownTimer > 0f)
                {
                    ratio = 1f - Mathf.Clamp01(airDashCooldownTimer / airDashCooldown);
                    isActiveOrCooling = true;
                    return true;
                }

                ratio = 1f;
                return true;
            case MovementSkillId.JetPack:
                if (jetPackTimer > 0f)
                {
                    float maxTime = Mathf.Max(0.01f, jetPackMaxUpTime);
                    ratio = Mathf.Clamp01(1f - (jetPackTimer / maxTime));
                    isActiveOrCooling = true;
                    return true;
                }
                if (jetPackCooldown > 0f && jetPackCooldownTimer > 0f)
                {
                    ratio = 1f - Mathf.Clamp01(jetPackCooldownTimer / jetPackCooldown);
                    isActiveOrCooling = true;
                    return true;
                }
                ratio = 1f;
                return true;
            case MovementSkillId.Levitation:
                if (levitationTimer > 0f)
                {
                    float maxTime = Mathf.Max(0.01f, levitationMaxTime);
                    ratio = Mathf.Clamp01(1f - (levitationTimer / maxTime));
                    isActiveOrCooling = true;
                    return true;
                }
                if (levitationCooldown > 0f && levitationCooldownTimer > 0f)
                {
                    ratio = 1f - Mathf.Clamp01(levitationCooldownTimer / levitationCooldown);
                    isActiveOrCooling = true;
                    return true;
                }
                ratio = 1f;
                return true;
            case MovementSkillId.Teleport:
                if (teleportCooldown > 0f && teleportCooldownTimer > 0f)
                {
                    ratio = 1f - Mathf.Clamp01(teleportCooldownTimer / teleportCooldown);
                    isActiveOrCooling = true;
                    return true;
                }
                ratio = 1f;
                return true;
            default:
                return false;
        }
    }

    public bool TryGetUtilityTimerStatus(out float ratio, out bool isActiveOrCooling)
    {
        ratio = 0f;
        isActiveOrCooling = false;

        switch (utilitySkillId)
        {
            case UtilitySkillId.SlowTime:
                if (slowTimeTimer > 0f && slowTimeMaxDuration > 0f && inputSnapshot.SlowTimeHeld)
                {
                    ratio = Mathf.Clamp01(slowTimeTimer / Mathf.Max(0.01f, slowTimeMaxDuration));
                    isActiveOrCooling = true;
                    return true;
                }
                if (slowTimeCooldown > 0f && slowTimeCooldownTimer > 0f)
                {
                    ratio = 1f - Mathf.Clamp01(slowTimeCooldownTimer / slowTimeCooldown);
                    isActiveOrCooling = true;
                    return true;
                }
                ratio = 1f;
                return true;
            case UtilitySkillId.FreezeTrucks:
                if (freezeTimer > 0f && freezeDuration > 0f)
                {
                    ratio = Mathf.Clamp01(freezeTimer / Mathf.Max(0.01f, freezeDuration));
                    isActiveOrCooling = true;
                    return true;
                }
                if (freezeCooldown > 0f && freezeCooldownTimer > 0f)
                {
                    ratio = 1f - Mathf.Clamp01(freezeCooldownTimer / freezeCooldown);
                    isActiveOrCooling = true;
                    return true;
                }
                ratio = 1f;
                return true;
            default:
                return false;
        }
    }

    private void EnsureTimeSlowVolume()
    {
        GameObject existing = GameObject.Find("TimeSlowVolume");
        if (existing != null)
        {
            timeSlowVolume = existing.GetComponent<Volume>();
            if (timeSlowVolume != null)
            {
                Debug.Log("[playerControl] TimeSlowVolume 已绑定，直接使用。");
                return;
            }
        }

        if (timeSlowVolumePrefab == null)
        {
            Debug.LogWarning("[playerControl] 未绑定 timeSlowVolumePrefab，无法自动生成 TimeSlowVolume。", this);
            return;
        }

        GameObject instance = Instantiate(timeSlowVolumePrefab);
        instance.name = "TimeSlowVolume";
        timeSlowVolume = instance.GetComponent<Volume>();
        if (timeSlowVolume == null)
        {
            Debug.LogWarning("[playerControl] timeSlowVolumePrefab 上未找到 Volume 组件。", this);
        }
    }

    private void UpdateJetPack(PlayerLocomotionState preMoveState, float deltaTime)
    {
        if (isDead || IsMenuPaused())
        {
            jetPackTimer = 0f;
            return;
        }

        if (preMoveState != PlayerLocomotionState.Airborne)
        {
            jetPackTimer = 0f;
        }

        if (jetPackCooldownTimer > 0f)
        {
            jetPackCooldownTimer = Mathf.Max(0f, jetPackCooldownTimer - deltaTime);
        }

        bool canUse = preMoveState == PlayerLocomotionState.Airborne
            && jetPackCooldownTimer <= 0f
            && jetPackTimer < jetPackMaxUpTime
            && inputSnapshot.PrimarySkillHeld;

        if (canUse)
        {
            jetPackTimer += deltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, jetPackUpSpeed);
            return;
        }

        if (jetPackTimer > 0f && jetPackCooldownTimer <= 0f)
        {
            jetPackCooldownTimer = jetPackCooldown;
        }
    }

    private void UpdateLevitation(PlayerLocomotionState preMoveState, float deltaTime)
    {
        levitationActive = false;

        if (isDead || IsMenuPaused())
        {
            levitationTimer = 0f;
            return;
        }

        if (preMoveState != PlayerLocomotionState.Airborne)
        {
            levitationTimer = 0f;
        }

        if (levitationCooldownTimer > 0f)
        {
            levitationCooldownTimer = Mathf.Max(0f, levitationCooldownTimer - deltaTime);
        }

        bool canUse = preMoveState == PlayerLocomotionState.Airborne
            && levitationCooldownTimer <= 0f
            && levitationTimer < levitationMaxTime
            && inputSnapshot.PrimarySkillHeld;

        if (canUse)
        {
            levitationActive = true;
            levitationTimer += deltaTime;
            verticalVelocity = 0f;
            return;
        }

        if (levitationTimer > 0f && levitationCooldownTimer <= 0f)
        {
            levitationCooldownTimer = levitationCooldown;
        }
    }

    private void UpdateTeleport(PlayerLocomotionState preMoveState, float deltaTime)
    {
        if (isDead || IsMenuPaused())
        {
            return;
        }

        if (teleportCooldownTimer > 0f)
        {
            teleportCooldownTimer = Mathf.Max(0f, teleportCooldownTimer - deltaTime);
        }

        if (!inputSnapshot.PrimarySkillPressedThisFrame || teleportCooldownTimer > 0f)
        {
            return;
        }

        if (!TryTeleportToTruck())
        {
            return;
        }

        teleportCooldownTimer = teleportCooldown;
    }

    private bool TryTeleportToTruck()
    {
        Transform originTransform = lookTarget != null ? lookTarget : transform;
        Vector3 origin = originTransform.position;
        Vector3 direction = originTransform.forward;

        if (!Physics.Raycast(origin, direction, out RaycastHit hit, teleportRange))
        {
            return false;
        }

        if (!IsTruckHit(hit.collider))
        {
            return false;
        }

        Vector3 targetPosition = hit.point + FixedUp * teleportHeight;
        if (controller != null)
        {
            controller.enabled = false;
        }

        transform.position = targetPosition;
        verticalVelocity = 0f;

        if (controller != null)
        {
            controller.enabled = true;
        }

        return true;
    }

    private bool IsTruckHit(Collider target)
    {
        if (target == null)
        {
            return false;
        }

        if (target.GetComponentInParent<truck_movement>() != null)
        {
            return true;
        }

        Transform tagChild = target.transform.Find("isTruckTag");
        return tagChild != null && tagChild.CompareTag("truck");
    }

    private void UpdateFreezeTrucks(float deltaTime)
    {
        if (isDead || IsMenuPaused())
        {
            UpdateFreezeTrucksCleanup();
            return;
        }

        if (freezeCooldownTimer > 0f)
        {
            freezeCooldownTimer = Mathf.Max(0f, freezeCooldownTimer - deltaTime);
        }

        if (freezeTimer > 0f)
        {
            freezeTimer = Mathf.Max(0f, freezeTimer - deltaTime);
            if (freezeTimer <= 0f)
            {
                SetTrucksFrozen(false);
                isFreezingTrucks = false;
                UpdateSlowTimeVolume(0f);
            }
            return;
        }

        if (!inputSnapshot.SecondarySkillPressedThisFrame || freezeCooldownTimer > 0f)
        {
            return;
        }

        freezeTimer = freezeDuration;
        freezeCooldownTimer = freezeCooldown;
        isFreezingTrucks = true;
        EnsureTimeSlowVolume();
        UpdateSlowTimeVolume(slowTimeVolumeTargetWeight);
        SetTrucksFrozen(true);
    }

    private void UpdateFreezeTrucksCleanup()
    {
        if (!isFreezingTrucks)
        {
            return;
        }

        freezeTimer = 0f;
        isFreezingTrucks = false;
        SetTrucksFrozen(false);
        UpdateSlowTimeVolume(0f);
    }

    private void SetTrucksFrozen(bool frozen)
    {
        truck_movement[] trucks = FindObjectsOfType<truck_movement>();
        for (int i = 0; i < trucks.Length; i++)
        {
            trucks[i].SetFrozen(frozen);
        }
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
        airborneJumpGraceDuration = configAsset.airborneJumpGraceDuration;
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
