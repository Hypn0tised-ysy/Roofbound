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

        if (deltaTime <= 0f)
        {
            PlatformVelocity = Vector3.zero;
            return;
        }

        Vector3 currentPosition = CurrentPlatform.position;
        PlatformVelocity = (currentPosition - lastPlatformPosition) / deltaTime;
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

    /// <summary>
    /// 暂停恢复后重新对齐平台参考点，避免首帧平台速度异常导致错位。
    /// </summary>
    public void SyncPlatformAnchor()
    {
        if (CurrentPlatform == null)
        {
            PlatformVelocity = Vector3.zero;
            InheritedPlatformVelocity = Vector3.zero;
            return;
        }

        lastPlatformPosition = CurrentPlatform.position;
        PlatformVelocity = Vector3.zero;
        InheritedPlatformVelocity = Vector3.zero;
    }

    private void RefreshCurrentPlatform(bool isGrounded)
    {
        // 离地时清空；接地但本帧未检测到平台时也清空，避免 Grounded 被误判为 OnPlatform。
        if (!isGrounded)
        {
            CurrentPlatform = null;
            PlatformVelocity = Vector3.zero;
            return;
        }

        if (detectedPlatformThisFrame == null)
        {
            CurrentPlatform = null;
            PlatformVelocity = Vector3.zero;
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
