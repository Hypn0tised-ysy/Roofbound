using UnityEngine;

/// <summary>
/// 统一管理玩家 yaw + 相机 pitch 的朝向逻辑。
/// </summary>
public sealed class PlayerLookController
{
    private readonly Vector3 upAxis;
    private readonly Transform owner;

    public Vector3 PlanarForward { get; private set; }
    public float PitchAngle { get; private set; }
    public Transform LookTarget { get; private set; }

    private Camera controlledCamera;

    public PlayerLookController(Transform owner, Vector3 upAxis)
    {
        this.owner = owner;
        this.upAxis = upAxis;
    }

    public void Initialize(Transform configuredLookTarget, Vector3 initialForward, float initialPitch)
    {
        PlanarForward = initialForward;
        PitchAngle = initialPitch;

        EnsureCameraSetup(configuredLookTarget);
        ApplyRotation();
    }

    public void UpdateFromMouse(Vector2 mouseDelta, float sensitivity, float minPitch, float maxPitch)
    {
        float yawDelta = mouseDelta.x * sensitivity;
        float pitchDelta = -mouseDelta.y * sensitivity;

        if (Mathf.Abs(yawDelta) <= Mathf.Epsilon && Mathf.Abs(pitchDelta) <= Mathf.Epsilon)
        {
            return;
        }

        PlanarForward = Quaternion.AngleAxis(yawDelta, upAxis) * PlanarForward;
        PlanarForward = Vector3.ProjectOnPlane(PlanarForward, upAxis).normalized;
        if (PlanarForward.sqrMagnitude < 0.0001f)
        {
            PlanarForward = Vector3.right;
        }

        PitchAngle = Mathf.Clamp(PitchAngle + pitchDelta, minPitch, maxPitch);
        ApplyRotation();
    }

    private void EnsureCameraSetup(Transform configuredLookTarget)
    {
        if (configuredLookTarget == null)
        {
            Transform existingPivot = owner.Find("Camera");
            if (existingPivot != null)
            {
                LookTarget = existingPivot;
            }
            else
            {
                GameObject pivot = new GameObject("Camera");
                LookTarget = pivot.transform;
                LookTarget.SetParent(owner, false);
                LookTarget.localPosition = Vector3.zero;
                LookTarget.localRotation = Quaternion.identity;
            }
        }
        else
        {
            LookTarget = configuredLookTarget;
        }

        controlledCamera = LookTarget.GetComponent<Camera>();
        if (controlledCamera == null)
        {
            controlledCamera = LookTarget.gameObject.AddComponent<Camera>();
        }
    }

    private void ApplyRotation()
    {
        Quaternion yawRotation = Quaternion.LookRotation(PlanarForward, upAxis);
        owner.rotation = yawRotation;

        if (LookTarget == null)
        {
            return;
        }

        Quaternion pitchLocalRotation = Quaternion.Euler(PitchAngle, 0f, 0f);
        LookTarget.localRotation = pitchLocalRotation;

        if (controlledCamera != null)
        {
            controlledCamera.transform.rotation = yawRotation * pitchLocalRotation;
        }
    }
}
