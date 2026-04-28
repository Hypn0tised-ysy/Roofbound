using System;
using UnityEngine;

/// <summary>
/// 统一封装移动求解：输入方向、冲刺倍率、重力、跳跃与平台速度合成。
/// </summary>
public sealed class PlayerMovementSolver
{
    private readonly CharacterController controller;
    private readonly PlatformMotionTracker platformMotion;
    private readonly PlayerLookController lookController;
    private readonly Vector3 upAxis;
    private readonly Func<bool> checkGrounded;

    public PlayerMovementSolver(
        CharacterController controller,
        PlatformMotionTracker platformMotion,
        PlayerLookController lookController,
        Vector3 upAxis,
        Func<bool> checkGrounded)
    {
        this.controller = controller;
        this.platformMotion = platformMotion;
        this.lookController = lookController;
        this.upAxis = upAxis;
        this.checkGrounded = checkGrounded;
    }

    public void Step(
        PlayerInputSnapshot inputSnapshot,
        PlayerLocomotionRuntime locomotionRuntime,
        float speed,
        float sprintMultiplier,
        float gravityAcceleration,
        float groundedVerticalVelocity,
        float jumpSpeed,
        float deltaTime,
        ref bool isGrounded,
        ref float verticalVelocity,
        ref Vector3 relativeHorizontalVelocity)
    {
        bool wasGroundedBeforeMove = isGrounded;

        // 平台速度先于速度合成计算，保证当前帧即可使用最新平台基速度。
        platformMotion.BeginFrame(isGrounded, deltaTime);

        // 读取 WASD 合成方向。
        Vector2 input = inputSnapshot.Move;

        // 按文档公式：
        // right = forward × up
        // W 对应方向：moveForward = up × right
        Vector3 right = Vector3.Cross(lookController.PlanarForward, upAxis).normalized;
        Vector3 moveForward = Vector3.Cross(upAxis, right).normalized;

        // 保持 WASD 直觉：W/S 使用 moveForward，A/D 使用 -right。
        Vector3 rawMove = moveForward * input.y - right * input.x;
        Vector3 moveDir = rawMove.sqrMagnitude > 0.0001f ? rawMove.normalized : Vector3.zero;

        // 空中不应用冲刺倍率，避免出现空中冲刺。
        bool isSprinting = isGrounded && locomotionRuntime.IsSprinting();
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

        // 允许接地跳，或在离开平台后的短暂宽限窗口内触发一次跳跃。
        bool canConsumeJump = isGrounded || locomotionRuntime.CanUseAirborneJumpGrace;
        if (canConsumeJump && locomotionRuntime.TryConsumeJumpQueued())
        {
            // 仅接地时锁存当前平台速度；空中宽限跳不应覆盖既有继承速度。
            if (isGrounded)
            {
                platformMotion.HandleJumpTriggered();
            }

            verticalVelocity = jumpSpeed;
        }

        verticalVelocity -= gravityAcceleration * deltaTime;

        Vector3 effectivePlatformVelocity = platformMotion.GetEffectivePlatformVelocity(isGrounded);
        Vector3 frameMotion = (relativeHorizontalVelocity + effectivePlatformVelocity) * deltaTime
            + upAxis * (verticalVelocity * deltaTime);

        CollisionFlags flags = controller.Move(frameMotion);
        if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < 0f)
        {
            verticalVelocity = groundedVerticalVelocity;
        }

        isGrounded = checkGrounded();

        // Move 后更新平台跟踪，处理离地继承与平台切换。
        platformMotion.AfterMove(wasGroundedBeforeMove, isGrounded);
    }
}
