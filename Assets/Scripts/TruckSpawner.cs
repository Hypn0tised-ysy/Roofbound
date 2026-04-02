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

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("TruckSpawner Start called");
        for (int i = 0; i < truckCount; i++)
        {
            float z = startZ + i * spacing;
            Vector3 pos = new Vector3(0f, spawnY, z);
            Debug.Log("生成卡车 at " + pos);
            Instantiate(truckPrefab, pos, Quaternion.identity);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
