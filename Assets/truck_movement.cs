using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class truck_movement : MonoBehaviour
{
    [SerializeField] private truck_config configAsset;

    [Header("移动参数")]
    [SerializeField] private float speed = 5f;

    [Header("Rigidbody 参数")]
    [SerializeField] private float mass = 1f;
    [SerializeField] private float drag = 0f;
    [SerializeField] private float angularDrag = 0.05f;
    [SerializeField] private bool useGravity = true;
    [SerializeField] private bool isKinematic = false;
    [SerializeField] private RigidbodyInterpolation interpolation = RigidbodyInterpolation.None;
    [SerializeField] private CollisionDetectionMode collisionDetection = CollisionDetectionMode.Discrete;
    [SerializeField] private RigidbodyConstraints constraints = RigidbodyConstraints.None;

    [Header("侧翻检测")]
    [Tooltip("是否启用侧翻检测与减速逻辑。")]
    [SerializeField] private bool enableFlipDetection = true;
    [Tooltip("当 dot(transform.up, Vector3.up) 低于该阈值时视为侧翻。")]
    [SerializeField] private float flippedUpDotThreshold = 0.25f;
    [Tooltip("侧翻时施加的线性阻力系数（加速度模式，等效 a = -k * v）。")]
    [SerializeField] private float flippedLinearDragCoefficient = 4f;
    [Tooltip("侧翻时施加的角速度阻尼系数（加速度模式，等效 alpha = -k * w）。")]
    [SerializeField] private float flippedAngularDampingCoefficient = 2f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ApplyConfigAsset();
        ApplyRigidbodyConfig();
    }

    private void Update()
    {
    }

    private void FixedUpdate()
    {
        if (enableFlipDetection && IsFlipped())
        {
            ApplyFlipDragForce();
            return;
        }

        Vector3 nextPos = rb.position + Vector3.forward * speed * Time.fixedDeltaTime;
        rb.MovePosition(nextPos);
    }

    public void SetRuntimeSpeed(float runtimeSpeed, bool overrideSpeed)
    {
        if (!overrideSpeed)
        {
            return;
        }

        speed = Mathf.Max(0f, runtimeSpeed);
    }

    private void ApplyConfigAsset()
    {
        if (configAsset == null)
        {
            Debug.LogWarning("[truck_movement] 未绑定 truck_config 资产，将使用脚本默认值。");
            return;
        }

        speed = configAsset.speed;
        mass = configAsset.mass;
        drag = configAsset.drag;
        angularDrag = configAsset.angularDrag;
        useGravity = configAsset.useGravity;
        isKinematic = configAsset.isKinematic;
        interpolation = configAsset.interpolation;
        collisionDetection = configAsset.collisionDetection;
        constraints = configAsset.constraints;
        enableFlipDetection = configAsset.enableFlipDetection;
        flippedUpDotThreshold = configAsset.flippedUpDotThreshold;
        flippedLinearDragCoefficient = configAsset.flippedLinearDragCoefficient;
        flippedAngularDampingCoefficient = configAsset.flippedAngularDampingCoefficient;
    }

    private bool IsFlipped()
    {
        float clampedThreshold = Mathf.Clamp(flippedUpDotThreshold, -1f, 1f);
        float upDot = Vector3.Dot(transform.up, Vector3.up);
        return upDot < clampedThreshold;
    }

    private void ApplyFlipDragForce()
    {
        if (rb == null)
        {
            return;
        }

        float linearK = Mathf.Max(0f, flippedLinearDragCoefficient);
        if (linearK > 0f)
        {
            rb.AddForce(-rb.velocity * linearK, ForceMode.Acceleration);
        }

        float angularK = Mathf.Max(0f, flippedAngularDampingCoefficient);
        if (angularK > 0f)
        {
            rb.AddTorque(-rb.angularVelocity * angularK, ForceMode.Acceleration);
        }
    }

    private void ApplyRigidbodyConfig()
    {
        rb.mass = Mathf.Max(0.0001f, mass);
        rb.drag = Mathf.Max(0f, drag);
        rb.angularDrag = Mathf.Max(0f, angularDrag);
        rb.useGravity = useGravity;
        rb.isKinematic = isKinematic;
        rb.interpolation = interpolation;
        rb.collisionDetectionMode = collisionDetection;
        rb.constraints = constraints;
    }
}
