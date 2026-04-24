using UnityEngine;

[CreateAssetMenu(fileName = "player_config", menuName = "Roofbound/Player Config")]
public class player_config : ScriptableObject
{
    [Header("移动参数")]
    public float speed = 16f;
    public float sprintMultiplier = 1.6f;
    public float sprintForwardThreshold = 0.1f;
    public float sprintDuration = 0.25f;
    public float sprintCooldown = 0.8f;

    [Header("镜头/朝向参数")]
    public float mouseLookSensitivity = 0.15f;
    public float minPitch = -75f;
    public float maxPitch = 75f;

    [Header("跳跃参数")]
    public float jumpSpeed = 27f;

    [Header("地面检测")]
    public float groundCheckRadius = 0.2f;
    public LayerMask groundMask = ~0;

    [Header("重力参数")]
    public float gravityAcceleration = 80f;
    public float groundedVerticalVelocity = -2f;
}
