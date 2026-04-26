using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Level Config", menuName = "Clustertruck/Level Config")]
public class LevelConfigData : ScriptableObject
{
    [Header("--- 车队阵型设置 ---")]
    public int rows = 10;                  // 一共几排
    [Range(2, 5)]
    public int columns = 3;                // 一排几辆
    public float spacingX = 4.5f;          // 左右间距
    public float spacingZ = 20f;           // 前后间距

    [Header("--- 卡车性能设置 ---")]
    public float truckMaxSpeed = 20f;      // 基础最高速度
    public float speedVariance = 2f;       // 速度随机误差（防整齐划一）

    public float truckMotorForce = 5000f;  // 基础推力
    public float forceVariance = 500f;     // 推力随机误差

    public float steerForce = 50f;         // 转向灵活度
    public float switchDistance = 30f;     // 寻路切换距离
}
