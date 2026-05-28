using UnityEngine;

[CreateAssetMenu(fileName = "player_skill_parameters", menuName = "Roofbound/Player Skill Parameters")]
public class PlayerSkillParameters : ScriptableObject
{
    [Header("Air Dash")]
    [Tooltip("Air dash horizontal speed.")]
    public float airDashSpeed = 12f;
    [Tooltip("Air dash duration in seconds.")]
    public float airDashDuration = 0.25f;
    [Tooltip("Air dash cooldown in seconds.")]
    public float airDashCooldown = 0.8f;
    [Tooltip("FOV shrink amount during air dash.")]
    public float airDashFovDelta = 8f;

    [Header("Slow Time")]
    [Tooltip("Time.timeScale while slow time is active.")]
    public float slowTimeScale = 0.2f;
    [Tooltip("Scale applied to Time.fixedDeltaTime while slow time is active.")]
    public float slowTimeFixedDeltaScale = 0.2f;
    [Tooltip("Target volume weight when slow time is active.")]
    public float slowTimeVolumeTargetWeight = 0.3f;
    [Tooltip("Speed to lerp the slow time volume weight.")]
    public float slowTimeVolumeLerpSpeed = 6f;
    [Tooltip("Max duration for slow time activation (seconds).")]
    public float slowTimeMaxDuration = 2f;
    [Tooltip("Cooldown after slow time ends (seconds).")]
    public float slowTimeCooldown = 6f;

    [Header("Jet Pack")]
    [Tooltip("Upward speed while jet pack is held.")]
    public float jetPackUpSpeed = 6f;
    [Tooltip("Max duration of jet pack thrust (seconds).")]
    public float jetPackMaxUpTime = 1.2f;
    [Tooltip("Cooldown after jet pack use (seconds).")]
    public float jetPackCooldown = 1.5f;

    [Header("Levitation")]
    [Tooltip("Max duration of levitation (seconds).")]
    public float levitationMaxTime = 1.2f;
    [Tooltip("Cooldown after levitation (seconds).")]
    public float levitationCooldown = 1.5f;

    [Header("Teleport")]
    [Tooltip("Raycast distance for teleport.")]
    public float teleportRange = 60f;
    [Tooltip("Height offset above hit point when teleporting.")]
    public float teleportHeight = 2f;
    [Tooltip("Cooldown after teleport (seconds).")]
    public float teleportCooldown = 2f;

    [Header("Freeze Trucks")]
    [Tooltip("Freeze duration (seconds).")]
    public float freezeDuration = 2f;
    [Tooltip("Freeze cooldown (seconds).")]
    public float freezeCooldown = 6f;
}
