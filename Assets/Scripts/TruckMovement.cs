using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TruckMovement: MonoBehaviour
{
    public float speed = 5f;           // 移动速度（单位/秒）
    public float destroyZ = 120f;      // 超过此Z坐标则销毁

    void Start()
    {
        
    }

    void Update()
    {
        // 向前移动（沿Z轴正方向）
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // 超出范围销毁
        if (transform.position.z > destroyZ)
        {
            Destroy(gameObject);
        }
    }
}
