using System.Collections;       // 引用，支持协程等（当前暂未使用）
using System.Collections.Generic;   // 引用泛型容器（当前暂未使用）
using UnityEngine;

/// <summary>
/// 卡车车队生成器：按照配置在场景中生成由多行多列组成的卡车矩阵。
/// 自动从指定父物体读取路径点，并传递给每辆卡车。
/// </summary>
public class TruckFleetSpawner : MonoBehaviour
{
    [Header("核心配置")]
    public GameObject truckPrefab;         // 卡车预制体，需挂有SingleTruckAI组件
    public LevelConfigData levelConfig;    // 关卡配置参数（行数、列数、间距、速度等）

    [Header("生成时机")]
    public float startDelay = 0f;          // 启动后延迟生成的时间（秒），用于出场时机控制

    [Header("关卡路线 (只需拖入父物体)")]
    public Transform pathParent;           // 包含所有路径点的父物体，子物体按顺序作为路径点

    // ---------- 生命周期 ----------
    void Start()
    {
        // 根据延迟时间决定是立即生成还是延时生成
        if (startDelay > 0)
            Invoke("GenerateFleet", startDelay);   // 延迟调用
        else
            GenerateFleet();
    }

    // ---------- 工具方法：从父物体提取所有子物体作为路径点数组 ----------
    /// <summary>
    /// 按层级顺序获取pathParent下的所有子物体Transform，构成路径点数组。
    /// 顺序取决于Hierarchy中的排列（从上到下）。
    /// </summary>
    private Transform[] GetWaypoints()
    {
        if (pathParent == null) return new Transform[0];   // 避免空引用

        // 分配与子物体数量等长的数组
        Transform[] points = new Transform[pathParent.childCount];

        // 顺序填充
        for (int i = 0; i < pathParent.childCount; i++)
        {
            points[i] = pathParent.GetChild(i);
        }
        return points;
    }

    // ---------- 核心生成逻辑 ----------
    void GenerateFleet()
    {
        // --- 安全检查 ---
        if (truckPrefab == null || levelConfig == null || pathParent == null)
        {
            Debug.LogError("请检查 Prefab、Config 或 路线父物体 是否为空！");
            return;
        }

        // 自动获取路径点数组
        Transform[] pathWaypoints = GetWaypoints();

        if (pathWaypoints.Length == 0)
        {
            Debug.LogWarning("路线父物体里面没有子节点！");
            return;
        }

        // --- 从配置读取矩阵参数 ---
        int rows = levelConfig.rows;           // 车队纵向行数（z方向）
        int columns = levelConfig.columns;     // 车队横向列数（x方向）
        float spacingX = levelConfig.spacingX; // 横向间距
        float spacingZ = levelConfig.spacingZ; // 纵向间距

        // 计算最左端起始x偏移（使矩阵以中心对齐）
        float startX = -(columns - 1) * spacingX / 2f;

        // 准备收集首末两排坐标，用于调试打印
        System.Text.StringBuilder firstRowMsg = new System.Text.StringBuilder();
        System.Text.StringBuilder lastRowMsg = new System.Text.StringBuilder();

        // --- 双层循环生成所有卡车 ---
        for (int z = 0; z < rows; z++)           // z：从近到远（根据生成坐标，-z方向越远）
        {
            for (int x = 0; x < columns; x++)    // x：从左到右
            {
                // 计算生成位置（世界坐标）
                // 以Spawner自身为原点，使用其右方向（红色轴）作为横向，前方向（蓝色轴）作为纵向
                Vector3 spawnPos = transform.position
                                 + transform.right * (startX + x * spacingX)    // 横向偏移
                                 - transform.forward * (z * spacingZ);         // 纵向偏移（负forward使z=0离Spawner最近）

                // 实例化卡车
                GameObject newTruck = Instantiate(truckPrefab, spawnPos, transform.rotation);
                SingleTruckAI truckAI = newTruck.GetComponent<SingleTruckAI>();

                // 将路径点及配置参数注入每辆卡车
                if (truckAI != null)
                {
                    truckAI.InitData(levelConfig, pathWaypoints);
                }

                // 收集调试信息：第一排（z==0）
                if (z == 0)
                    firstRowMsg.Append($"[{x}] {spawnPos}  ");
                // 最后一排（z == rows-1）
                if (z == rows - 1)
                    lastRowMsg.Append($"[{x}] {spawnPos}  ");

                // 将摄像机跟随目标设为最后一排中间那辆卡车
                if (z == rows - 1 && x == columns / 2)
                {
                    CameraFollow camFollow = Camera.main?.GetComponent<CameraFollow>();
                    if (camFollow != null)
                        camFollow.target = newTruck.transform;
                }
            }
        }

        // 打印首末两排坐标，方便检查生成布局
        Debug.Log($"第一排 (z=0) 坐标：{firstRowMsg}");
        Debug.Log($"最后一排 (z={rows - 1}) 坐标：{lastRowMsg}");
    }

    // ==========================================
    // 编辑器可视化：Gizmos 绘制路线和生成区域
    // ==========================================
    void OnDrawGizmos()
    {
        // 绘制生成区域的大致范围
        if (levelConfig != null)
        {
            Gizmos.color = Color.green;
            // 在Spawner中心画一个线框立方体，宽度为总列宽，高度和深度取固定值以示标识
            Gizmos.DrawWireCube(transform.position, new Vector3(levelConfig.spacingX * levelConfig.columns, 2f, 5f));
            // 绘制前进方向射线（蓝色）
            Gizmos.DrawRay(transform.position, transform.forward * 10f);
        }

        // 实时获取路径点子物体（即使没有运行也能在Scene视图中看到）
        Transform[] points = GetWaypoints();
        if (points == null || points.Length == 0) return;

        // 第一条线：从Spawner到第一个路径点（黄色）
        if (points[0] != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, points[0].position);
        }

        // 后续连线及点标记（青色）
        Gizmos.color = Color.cyan;
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null) continue;

            // 在每个路径点位置画一个小球
            Gizmos.DrawSphere(points[i].position, 1.5f);

            // 画相邻路径点之间的连线
            if (i < points.Length - 1 && points[i + 1] != null)
            {
                Gizmos.DrawLine(points[i].position, points[i + 1].position);
            }
        }
    }
}