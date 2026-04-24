using UnityEngine;

/// <summary>
/// 统一管理跳跃与冲刺计时/资格，保持与历史逻辑等价。
/// </summary>
public sealed class PlayerLocomotionRuntime
{
    public float SprintTimer { get; private set; }
    public float SprintCooldownTimer { get; private set; }

    private bool canJump;
    private bool wasGrounded;
    private bool jumpQueued;

    public void Initialize(bool isInitiallyGrounded)
    {
        canJump = isInitiallyGrounded;
        wasGrounded = isInitiallyGrounded;
        jumpQueued = false;
        SprintTimer = 0f;
        SprintCooldownTimer = 0f;
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

    public void UpdateBeforeMovement(
        PlayerLocomotionState preMoveState,
        float verticalVelocity,
        bool jumpPressedThisFrame,
        float forwardInput,
        bool sprintPressed,
        float sprintForwardThreshold,
        float sprintDuration,
        float sprintCooldown,
        float deltaTime)
    {
        bool isOnPlatform = preMoveState == PlayerLocomotionState.OnPlatform;
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

        // 状态机约束：仅站在平台上才允许刷新跳跃资格。
        if (!isOnPlatform)
        {
            canJump = false;
        }

        if (isOnPlatform && verticalVelocity <= 0.01f)
        {
            canJump = true;
        }

        if (isOnPlatform && !wasGrounded)
        {
            canJump = true;
        }

        wasGrounded = isGrounded;

        if (canJump && jumpPressedThisFrame)
        {
            jumpQueued = true;
            canJump = false;
        }

        bool canSprint = isOnPlatform
            && SprintCooldownTimer <= 0f
            && SprintTimer <= 0f
            && forwardInput > sprintForwardThreshold;

        if (canSprint && sprintPressed)
        {
            SprintTimer = sprintDuration;
            SprintCooldownTimer = sprintCooldown;
        }
    }
}
