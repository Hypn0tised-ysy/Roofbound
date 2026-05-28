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
}
