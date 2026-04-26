using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;                // 要跟随的卡车
    public Vector3 offset = new Vector3(0, 5, -10); // 相对于卡车的偏移
    public float smoothSpeed = 8f;          // 跟随平滑度

    void LateUpdate()
    {
        if (target == null) return;

        // 计算目标位置（世界坐标）
        Vector3 desiredPos = target.position + target.TransformDirection(offset);
        // 平滑移动
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
        // 让摄像机看向卡车
        transform.LookAt(target);
    }
}
