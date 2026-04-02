using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TruckSpawner : MonoBehaviour
{
    public GameObject truckPrefab;
    public int truckCount = 10;           // 卡车数量
    public float startZ = -10f;           // 第一辆卡车的起始Z
    public float spacing = 12f;           // 每两辆卡车的间距
    public float spawnY = 0.4f;

    void Start()
    {
        Debug.Log("TruckSpawner Start called");

        // 预制体是否为空
        if (truckPrefab == null)
        {
            Debug.LogError("【TruckSpawner】卡车预制体 (truckPrefab) 未赋值！请在 Inspector 中拖入 Truck 预制体。");
            return; // 不再执行生成
        }

        // 卡车数量是否合理
        if (truckCount <= 0)
        {
            Debug.LogWarning("【TruckSpawner】truckCount <= 0，不生成任何卡车。");
            return;
        }

        for (int i = 0; i < truckCount; i++)
        {
            float z = startZ + i * spacing;
            Vector3 pos = new Vector3(0f, spawnY, z);
            Debug.Log($"生成卡车 {i + 1}/{truckCount} 位置: {pos}");

            // 实例化
            GameObject newTruck = Instantiate(truckPrefab, pos, Quaternion.identity);
            if (newTruck == null)
            {
                Debug.LogError($"【TruckSpawner】第{i + 1}辆卡车实例化失败！");
            }
        }
    }
}