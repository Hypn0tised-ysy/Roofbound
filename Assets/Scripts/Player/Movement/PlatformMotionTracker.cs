using UnityEngine;

/// <summary>
/// 维护平台速度与离地继承速度，保证平台移动与起跳继承行为稳定。
/// </summary>
public sealed class PlatformMotionTracker
{
    public Transform CurrentPlatform { get; private set; }
    public Vector3 PlatformVelocity { get; private set; }
    public Vector3 InheritedPlatformVelocity { get; private set; }

    private Transform detectedPlatformThisFrame;
    private Vector3 lastPlatformPosition;

    public void BeginFrame(bool isGrounded, float deltaTime)
    {
        detectedPlatformThisFrame = null;

        if (!isGrounded || CurrentPlatform == null)
        {
            PlatformVelocity = Vector3.zero;
            return;
        }

        float dt = Mathf.Max(deltaTime, 0.0001f);
        Vector3 currentPosition = CurrentPlatform.position;
        PlatformVelocity = (currentPosition - lastPlatformPosition) / dt;
        lastPlatformPosition = currentPosition;

        // 接地期间持续刷新，保证离地时继承的是最新平台速度。
        InheritedPlatformVelocity = PlatformVelocity;
    }

    public void RegisterGroundHit(ControllerColliderHit hit, Vector3 upAxis)
    {
        if (Vector3.Dot(hit.normal, upAxis) > 0.5f)
        {
            detectedPlatformThisFrame = hit.transform;
        }
    }

    public void HandleJumpTriggered()
    {
        InheritedPlatformVelocity = PlatformVelocity;
    }

    public Vector3 GetEffectivePlatformVelocity(bool isGrounded)
    {
        return isGrounded ? PlatformVelocity : InheritedPlatformVelocity;
    }

    public void AfterMove(bool wasGroundedBeforeMove, bool isGrounded)
    {
        if (wasGroundedBeforeMove && !isGrounded)
        {
            InheritedPlatformVelocity = PlatformVelocity;
        }

        RefreshCurrentPlatform(isGrounded);
    }

    private void RefreshCurrentPlatform(bool isGrounded)
    {
        // 离地时清空；接地但本帧无回调时保留上帧平台，避免速度抖动。
        if (!isGrounded)
        {
            CurrentPlatform = null;
            PlatformVelocity = Vector3.zero;
            return;
        }

        if (detectedPlatformThisFrame == null)
        {
            return;
        }

        if (CurrentPlatform != detectedPlatformThisFrame)
        {
            CurrentPlatform = detectedPlatformThisFrame;
            lastPlatformPosition = CurrentPlatform.position;
            PlatformVelocity = Vector3.zero;
        }
    }
}
