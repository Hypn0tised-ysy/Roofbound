using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 统一封装角色输入采样，避免业务逻辑直接依赖 InputAction 细节。
/// </summary>
public sealed class PlayerInputReader
{
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction lookAction;

    public void InitializeDefaultBindings()
    {
        moveAction = new InputAction(name: "Move", type: InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        jumpAction = new InputAction(name: "Jump", type: InputActionType.Button, binding: "<Keyboard>/space");
        sprintAction = new InputAction(name: "Sprint", type: InputActionType.Button, binding: "<Keyboard>/leftShift");
        lookAction = new InputAction(name: "Look", type: InputActionType.Value, binding: "<Mouse>/delta");
    }

    public void Enable()
    {
        moveAction?.Enable();
        jumpAction?.Enable();
        sprintAction?.Enable();
        lookAction?.Enable();
    }

    public void Disable()
    {
        moveAction?.Disable();
        jumpAction?.Disable();
        sprintAction?.Disable();
        lookAction?.Disable();
    }

    public PlayerInputSnapshot ReadSnapshot()
    {
        return new PlayerInputSnapshot
        {
            Move = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero,
            Look = lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero,
            JumpPressedThisFrame = jumpAction != null && jumpAction.WasPressedThisFrame(),
            SprintPressed = sprintAction != null && sprintAction.IsPressed(),
        };
    }
}
