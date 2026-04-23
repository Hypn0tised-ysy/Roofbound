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
