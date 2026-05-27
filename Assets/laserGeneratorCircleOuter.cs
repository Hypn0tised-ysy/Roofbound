using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class laserGeneratorCircleOuter : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject laserPrefab;

    [Header("Circle")]
    [SerializeField] private float radiusInner = 5f;
    [SerializeField] private float radiusOuter = 10f;
    [SerializeField] private int sampleCount = 20;

    private void Start()
    {
        GenerateLasers();
    }

    private void GenerateLasers()
    {
        if (laserPrefab == null)
        {
            Debug.LogError("[laserGeneratorCircleOuter] 未绑定 laserPrefab，无法生成激光。", this);
            return;
        }

        if (sampleCount <= 0 || radiusInner <= 0f || radiusOuter <= radiusInner)
        {
            Debug.LogWarning("[laserGeneratorCircleOuter] 参数非法，无法生成激光。", this);
            return;
        }

        Vector3 center = transform.position;
        Vector3 axisY = transform.up;
        Vector3 axisZ = transform.forward;

        float angleStep = Mathf.PI * 2f / sampleCount;

        for (int i = 0; i < sampleCount; i++)
        {
            float theta = angleStep * i;
            Vector2 outerLocal = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) * radiusOuter;
            Vector3 outerPoint = center + axisY * outerLocal.x + axisZ * outerLocal.y;

            Vector2 tangentLocal = GetTangentPointLocal(outerLocal, radiusInner, radiusOuter);
            Vector3 tangentPoint = center + axisY * tangentLocal.x + axisZ * tangentLocal.y;

            Vector3 direction = (tangentPoint - outerPoint).normalized;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.right, -direction);

            GameObject instance = Instantiate(laserPrefab, tangentPoint, rotation, transform);
            instance.name = $"Laser_{i:D2}";
        }
    }

    private static Vector2 GetTangentPointLocal(Vector2 outerLocal, float innerRadius, float outerRadius)
    {
        float theta = Mathf.Atan2(outerLocal.y, outerLocal.x);
        float alpha = Mathf.Acos(Mathf.Clamp(innerRadius / outerRadius, -1f, 1f));
        float tangentAngle = theta + alpha;

        return new Vector2(
            Mathf.Cos(tangentAngle) * innerRadius,
            Mathf.Sin(tangentAngle) * innerRadius);
    }
}
