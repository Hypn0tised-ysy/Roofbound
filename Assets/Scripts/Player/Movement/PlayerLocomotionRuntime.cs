using UnityEngine;

/// <summary>
/// 统一管理跳跃与冲刺计时/资格，保持与历史逻辑等价。
/// </summary>
public sealed class PlayerLocomotionRuntime
{
    public float SprintTimer { get; private set; }
    public float SprintCooldownTimer { get; private set; }
    public float AirborneJumpGraceTimer { get; private set; }

    private bool canJump;
    private bool wasGrounded;
    private bool jumpQueued;
    private PlayerLocomotionState previousState;

    public void Initialize(bool isInitiallyGrounded)
    {
        canJump = false;
        wasGrounded = isInitiallyGrounded;
        jumpQueued = false;
        SprintTimer = 0f;
        SprintCooldownTimer = 0f;
        AirborneJumpGraceTimer = 0f;
        previousState = isInitiallyGrounded
            ? PlayerLocomotionState.Grounded
            : PlayerLocomotionState.Airborne;
    }

    public bool IsSprinting()
    {
        return SprintTimer > 0f;
    }

    public bool TryConsumeJumpQueued()
    {
        if (!jumpQueued)
        {
            return false;
        }

        jumpQueued = false;
        return true;
    }

    // Expose whether a jump is currently allowed (刷新资格后的结果)
    public bool CanJump => canJump;

    public bool CanUseAirborneJumpGrace => AirborneJumpGraceTimer > 0f;

    public void UpdateBeforeMovement(
        PlayerLocomotionState preMoveState,
        float verticalVelocity,
        bool jumpPressedThisFrame,
        bool sprintPressed,
        float sprintDuration,
        float sprintCooldown,
        float airborneJumpGraceDuration,
        float deltaTime)
    {
        bool enteredAirborneFromPlatform = preMoveState == PlayerLocomotionState.Airborne
            && previousState == PlayerLocomotionState.OnPlatform;

        if (enteredAirborneFromPlatform)
        {
            AirborneJumpGraceTimer = Mathf.Max(0f, airborneJumpGraceDuration);
        }

        if (preMoveState == PlayerLocomotionState.Airborne && AirborneJumpGraceTimer > 0f)
        {
            AirborneJumpGraceTimer = Mathf.Max(0f, AirborneJumpGraceTimer - deltaTime);
        }
        else if (preMoveState != PlayerLocomotionState.Airborne)
        {
            AirborneJumpGraceTimer = 0f;
        }

        bool canJumpSurface = preMoveState == PlayerLocomotionState.OnPlatform;
        bool canSprintSurface = preMoveState == PlayerLocomotionState.OnPlatform;
        bool canAirborneGraceJump = preMoveState == PlayerLocomotionState.Airborne
            && AirborneJumpGraceTimer > 0f;
        bool isGrounded = preMoveState == PlayerLocomotionState.OnPlatform
            || preMoveState == PlayerLocomotionState.Grounded;

        if (SprintTimer > 0f)
        {
            SprintTimer -= deltaTime;
        }

        if (SprintCooldownTimer > 0f)
        {
            SprintCooldownTimer -= deltaTime;
        }

        // 状态机约束：仅平台或空中宽限窗口内允许跳跃。
        if (!canJumpSurface && !canAirborneGraceJump)
        {
            canJump = false;
            jumpQueued = false;
        }

        if (!canSprintSurface)
        {
            SprintTimer = 0f;
        }

        if (canJumpSurface && verticalVelocity <= 0.01f)
        {
            canJump = true;
        }

        if (canJumpSurface && !wasGrounded)
        {
            canJump = true;
        }

        if (canAirborneGraceJump)
        {
            canJump = true;
        }

        wasGrounded = isGrounded;

        if (canJump && jumpPressedThisFrame)
        {
            jumpQueued = true;
            canJump = false;
        }

        bool canSprint = canSprintSurface
            && SprintCooldownTimer <= 0f
            && SprintTimer <= 0f;

        if (canSprint && sprintPressed)
        {
            SprintTimer = sprintDuration;
            SprintCooldownTimer = sprintCooldown;
        }

        previousState = preMoveState;
    }
}
