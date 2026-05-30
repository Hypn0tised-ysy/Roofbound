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

    [Header("街机重力魔法")]
    public float extraGravity = 20f;  // 额外向下重力（普通重力是9.8，这里加到40，让它像铁块一样）

    void Awake()
    {
        // 缓存组件，避免每帧重新查找
        rb = GetComponent<Rigidbody>();
        allColliders = GetComponents<Collider>();

        // 降低重心到车身几何中心以下（防止侧翻，注意数值不宜过低以免穿透地面）
        rb.centerOfMass = new Vector3(0, -1.5f, 0);
        rb.drag = 1.5f;          // 线性阻尼，收油后自然减速
        rb.angularDrag = 1.5f;     // 角阻尼，抑制转向过度
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
            ApplyDownforce();
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

        Vector3 dirToTarget = (targetPos - transform.position).normalized;
        dirToTarget.y = 0;
        float turnAmount = Vector3.Cross(transform.forward, dirToTarget).y;

        // === 新增：动态转向衰减魔法 ===
        // 获取当前速度比例 (0 到 1)
        float speedFactor = Mathf.Clamp01(rb.velocity.magnitude / maxSpeed);

        // 核心：速度越快，转向力越小。
        // Mathf.Lerp(1f, 0.4f, speedFactor) 意思是：低速时保持 100% 的 steerForce，高速时只剩 40% 的 steerForce。
        float currentSteerForce = steerForce * Mathf.Lerp(1f, 0.4f, speedFactor);

        rb.AddTorque(transform.up * turnAmount * steerForce, ForceMode.Acceleration);
    }

    /// <summary>
    /// 抑制横向侧滑
    /// </summary>
    void ApplyGrip()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);

        localVelocity.x *= 0.02f; // 保持强抓地力

        rb.velocity = transform.TransformDirection(localVelocity);
    }

    /// <summary>
    /// 施加扶正扭矩：只限制左右侧翻，允许自然上下坡（抬头/点头）
    /// </summary>
    void Stabilize()
    {
        // 1. 计算将当前朝向纠正到绝对直立所需的总扭矩
        Vector3 torqueVector = Vector3.Cross(transform.up, Vector3.up);

        // 2. 将这个世界坐标系下的扭矩，转换到卡车自身的局部坐标系下
        Vector3 localTorque = transform.InverseTransformDirection(torqueVector);

        // 3. 核心魔法：消除前后方向的限制！
        // 局部坐标系下：
        // x 轴代表前后仰角 (Pitch)，我们把它设为 0，允许卡车自然上下坡
        // y 轴代表左右转向 (Yaw)，不归这里管，设为 0
        // z 轴代表左右侧翻 (Roll)，保留这个值，死死按住防止侧翻
        localTorque.x = 0f;
        localTorque.y = 0f;

        // 4. 将处理后的扭矩转回世界坐标系，并施加给刚体
        Vector3 finalTorque = transform.TransformDirection(localTorque);
        rb.AddTorque(finalTorque * stabilizeForce, ForceMode.Acceleration);
    }

    /// <summary>
    /// 施加街机下压力：解决高速冲坡后“轻飘飘飞天”的问题
    /// </summary>
    void ApplyDownforce()
    {
        // 1. 绝对重力：让卡车只要悬空，就像铅球一样急速下坠
        rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);

        // 2. 空气动力学下压力（F1赛车原理）：沿着车顶方向的反方向往下压
        // 车速越快，这个下压力越大，让车轮死死咬住坡面！
        float currentSpeed = rb.velocity.magnitude;
        float speedFactor = Mathf.Clamp01(currentSpeed / maxSpeed); // 0到1的比例
        rb.AddForce(-transform.up * (extraGravity * 0.5f) * speedFactor, ForceMode.Acceleration);
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
