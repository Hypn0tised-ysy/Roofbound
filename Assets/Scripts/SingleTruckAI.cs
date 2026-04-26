using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单辆卡车的自动驾驶与物理控制。
/// 通过路径点寻路、推力/转向、防侧滑与防翻车逻辑实现稳定的行驶。
/// 碰撞后进入报废状态（打滑、无动力）。
/// </summary>
public class SingleTruckAI : MonoBehaviour
{
    // ---------- 由外部注入的性能参数（运行时赋值，不在 Inspector 显示） ----------
    [HideInInspector] public float motorForce;      // 发动机推力
    [HideInInspector] public float steerForce;      // 转向力度
    [HideInInspector] public float maxSpeed;        // 最高速度
    [HideInInspector] public float switchDistance;  // 切换路径点的距离阈值
    [HideInInspector] public Transform[] waypoints; // 路径点数组（由生成器传入）

    // ---------- 物理与视觉绑定（在预制体上手动设置） ----------
    [Header("物理与视觉模型绑定")]
    public float stabilizeForce = 10000f;            // 自动扶正扭矩（维持车身直立）
    public Transform[] wheels;                       // 车轮 Transform 数组，用于视觉旋转
    public float wheelSpinSpeed = 50f;               // 车轮旋转速度系数
    public PhysicMaterial slipperyMat;               // 碰撞报废后替换的物理材质（冰块效果）

    // ---------- 内部状态 ----------
    private Rigidbody rb;                // 刚体引用
    private bool isCrashed = false;      // 是否已报废
    private Collider[] allColliders;     // 所有碰撞体（用于统一更换材质）
    private int currentWPIndex = 0;      // 当前追踪的路径点索引

    void Awake()
    {
        // 缓存组件，避免每帧重新查找
        rb = GetComponent<Rigidbody>();
        allColliders = GetComponents<Collider>();

        // 降低重心到车身几何中心以下（防止侧翻，注意数值不宜过低以免穿透地面）
        rb.centerOfMass = new Vector3(0, -1.5f, 0);
        rb.drag = 1.5f;          // 线性阻尼，收油后自然减速
        rb.angularDrag = 5f;     // 角阻尼，抑制转向过度
    }

    // ---------- 外部注入配置 ----------
    /// <summary>
    /// 由车队生成器在生成时调用，传入关卡配置和路径点数组。
    /// </summary>
    public void InitData(LevelConfigData config, Transform[] path)
    {
        waypoints = path;

        // 为每辆车加入小幅度随机波动，避免所有车动作完全一致
        maxSpeed = config.truckMaxSpeed + Random.Range(-config.speedVariance, config.speedVariance);
        motorForce = config.truckMotorForce + Random.Range(-config.forceVariance, config.forceVariance);
        steerForce = config.steerForce;
        switchDistance = config.switchDistance;
    }

    void Update()
    {
        // 更新车轮旋转动画（只要未报废且车轮数组不为空）
        if (!isCrashed && wheels.Length > 0)
        {
            SpinWheels();
        }
    }

    void FixedUpdate()
    {
        if (isCrashed) return;      // 报废后不再执行任何物理驱动

        CheckCrashStatus();         // 检查是否倾倒导致报废

        // 路径存在且未报废时，每一物理帧执行驱动、转向、防滑和稳定
        if (waypoints != null && waypoints.Length > 0)
        {
            Drive();
            Steer();
            ApplyGrip();
            Stabilize();
        }
    }

    /// <summary>
    /// 根据车辆前进速度旋转车轮模型，仅视觉表现。
    /// </summary>
    void SpinWheels()
    {
        float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);

        foreach (Transform wheel in wheels)
        {
            if (wheel != null)
                wheel.Rotate(Vector3.forward, forwardSpeed * wheelSpinSpeed * Time.deltaTime, Space.Self);
        }
    }

    /// <summary>
    /// 沿车头方向施加推进力，速度越接近 maxSpeed 推力越小（线性递减）。
    /// </summary>
    void Drive()
    {
        float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
        float speedGap = Mathf.Max(0, maxSpeed - forwardSpeed);

        // 推力系数：剩余差值比例，防止在最高速时继续加力
        float forceFactor = speedGap / maxSpeed;

        rb.AddForce(transform.forward * motorForce * forceFactor, ForceMode.Acceleration);
    }

    /// <summary>
    /// 根据当前路径点计算转向扭矩，并自动切换到下一个路径点。
    /// </summary>
    void Steer()
    {
        Vector3 targetPos = waypoints[currentWPIndex].position;

        // 计算水平距离（忽略Y轴），避免上坡时因高度差提前切换
        Vector3 pos2D = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 target2D = new Vector3(targetPos.x, 0, targetPos.z);

        if (Vector3.Distance(pos2D, target2D) < switchDistance)
        {
            if (currentWPIndex < waypoints.Length - 1)
            {
                currentWPIndex++;
                targetPos = waypoints[currentWPIndex].position;
            }
        }

        // 转向量 = 当前朝向与目标方向叉积的Y分量（正值右转，负值左转）
        Vector3 dirToTarget = (targetPos - transform.position).normalized;
        dirToTarget.y = 0;
        float turnAmount = Vector3.Cross(transform.forward, dirToTarget).y;
        rb.AddTorque(transform.up * turnAmount * steerForce, ForceMode.Acceleration);
    }

    /// <summary>
    /// 抑制横向侧滑：将局部坐标下的横向速度按比例大幅削弱（接近零）。
    /// 注意：当前为直接修改速度，极端情况可能引起物理不稳定（可改为基于力的阻尼）。
    /// </summary>
    void ApplyGrip()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);
        localVelocity.x *= 0.02f; // 保留2%的侧向速度，模拟极强的横向抓地力
        rb.velocity = transform.TransformDirection(localVelocity);
    }

    /// <summary>
    /// 施加扶正扭矩，将车辆的上方向逐渐修正至世界向上。
    /// </summary>
    void Stabilize()
    {
        Vector3 cross = Vector3.Cross(transform.up, Vector3.up);
        rb.AddTorque(cross * stabilizeForce, ForceMode.Acceleration);
    }

    /// <summary>
    /// 检测车辆是否严重倾斜（超过70度），若满足则触发报废流程。
    /// </summary>
    void CheckCrashStatus()
    {
        if (Vector3.Angle(Vector3.up, transform.up) > 70f)
        {
            TriggerCrash();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isCrashed) return;

        ContactPoint contact = collision.GetContact(0);
        bool isHitAnotherTruck = collision.gameObject.GetComponent<SingleTruckAI>() != null;

        // 只对非卡车的障碍物处理报废碰撞
        if (!isHitAnotherTruck)
        {
            // 条件：接触面接近垂直方向（法线.y 较小）且相对速度足够高
            if (Mathf.Abs(contact.normal.y) < 0.5f && collision.relativeVelocity.magnitude > 5f)
            {
                TriggerCrash();
                // 施加一个向后上方弹飞的效果，并随机旋转
                rb.AddForce(-transform.forward * 8000f + Vector3.up * 3000f, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 8000f, ForceMode.Impulse);
            }
        }
    }

    /// <summary>
    /// 执行报废：标记状态、移除阻力并替换所有碰撞体为打滑物理材质。
    /// </summary>
    void TriggerCrash()
    {
        if (isCrashed) return;
        isCrashed = true;

        // 去除阻尼，使车辆可以自由滑动
        rb.drag = 0f;
        rb.angularDrag = 0.05f;

        if (slipperyMat != null)
        {
            foreach (var col in allColliders)
                col.material = slipperyMat;
        }
    }
}
