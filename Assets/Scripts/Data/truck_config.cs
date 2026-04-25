using UnityEngine;

[CreateAssetMenu(fileName = "truck_config", menuName = "Roofbound/Truck Config")]
public class truck_config : ScriptableObject
{
    [Header("移动参数")]
    public float speed = 5f;

    [Header("Rigidbody 参数")]
    public float mass = 10f;
    public float drag = 0f;
    public float angularDrag = 0.50f;
    public bool useGravity = true;
    public bool isKinematic = false;
    public RigidbodyInterpolation interpolation = RigidbodyInterpolation.None;
    public CollisionDetectionMode collisionDetection = CollisionDetectionMode.Discrete;
    public RigidbodyConstraints constraints = RigidbodyConstraints.None;

    [Header("侧翻检测")]
    public bool enableFlipDetection = true;
    public float flippedUpDotThreshold = 0.25f;
    public float flippedLinearDragCoefficient = 4f;
    public float flippedAngularDampingCoefficient = 2f;
}
