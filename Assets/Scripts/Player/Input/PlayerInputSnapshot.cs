using UnityEngine;

public struct PlayerInputSnapshot
{
    public Vector2 Move;
    public Vector2 Look;
    public bool JumpPressedThisFrame;
    public bool SprintPressed;
    public bool PrimarySkillPressedThisFrame;
    public bool PrimarySkillHeld;
    public bool SlowTimeHeld;
    public bool SecondarySkillPressedThisFrame;
}
