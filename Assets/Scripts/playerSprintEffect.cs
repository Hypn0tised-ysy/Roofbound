using UnityEngine;

/// <summary>
/// 冲刺镜头视效：挂在 Camera 上，根据玩家冲刺状态与当前速度动态调整 FOV。
/// </summary>
[RequireComponent(typeof(Camera))]
public class playerSprintEffect : MonoBehaviour
{
	[Header("引用")]
	[SerializeField] private playerControl targetPlayer;

	[Header("FOV 参数")]
	[SerializeField] private float baseFov = 60f;
	[SerializeField] private float maxSprintFovBoost = 10f;
	[SerializeField] private float speedForMaxBoost = 20f;
	[SerializeField] private float fovSmoothSpeed = 10f;

	private Camera targetCamera;

	private void Awake()
	{
		targetCamera = GetComponent<Camera>();

		if (targetPlayer == null)
		{
			targetPlayer = FindObjectOfType<playerControl>();
		}

		if (targetCamera != null)
		{
			baseFov = targetCamera.fieldOfView;
		}
	}

	private void LateUpdate()
	{
		if (targetCamera == null)
		{
			return;
		}

		float targetFov = baseFov;

		if (targetPlayer != null && targetPlayer.IsSprinting)
		{
			float maxBoostDenominator = Mathf.Max(speedForMaxBoost, 0.01f);
			float speedRatio = Mathf.Clamp01(targetPlayer.CurrentHorizontalSpeed / maxBoostDenominator);
			targetFov = baseFov + maxSprintFovBoost * speedRatio;
		}

		float lerpFactor = Mathf.Clamp01(fovSmoothSpeed * Time.deltaTime);
		targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, targetFov, lerpFactor);
	}
}