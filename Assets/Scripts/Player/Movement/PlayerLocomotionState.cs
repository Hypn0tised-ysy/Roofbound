using UnityEngine;

/// <summary>
/// 角色移动状态标识。
/// </summary>
public enum PlayerLocomotionState
{
    Grounded,
    OnPlatform,
    Airborne,
    PostSprint,
}

public struct PlayerLocomotionFrameContext
{
    public bool IsGrounded;
    public bool IsSprinting;
    public bool HasPlatform;
    public Vector2 MoveInput;
}

public interface IPlayerLocomotionStateNode
{
    void OnEnter(PlayerLocomotionFrameContext context);
    void Tick(PlayerLocomotionFrameContext context, float deltaTime);
    void OnExit(PlayerLocomotionFrameContext context);
}
