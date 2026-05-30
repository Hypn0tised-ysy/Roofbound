using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//由offset控制wall间隔
public class laserGeneratorWall : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject laserPrefab;

    [Header("Layout")]
    [SerializeField] private int laserCount = 20;
    [SerializeField] private float offset = 10f;

    [Header("Swing")]
    [SerializeField] private float swingAngle = 75f;
    [SerializeField] private float angularSpeed = 30f;
    [SerializeField] private float phaseOffset = 15f;

    private readonly List<Transform> laserInstances = new List<Transform>();
    private readonly List<Quaternion> baseLocalRotations = new List<Quaternion>();

    private void Start()
    {
        GenerateLasers();
    }

    private void Update()
    {
        UpdateSwing();
    }

    private void GenerateLasers()
    {
        laserInstances.Clear();
        baseLocalRotations.Clear();

        if (laserPrefab == null)
        {
            Debug.LogError("[laserGenerator] 未绑定 laserPrefab，无法生成激光。", this);
            return;
        }

        if (laserCount <= 0)
        {
            return;
        }

        for (int i = 0; i < laserCount; i++)
        {
            Vector3 localPosition = Vector3.right * (offset * i);
            GameObject instance = Instantiate(laserPrefab, transform);
            instance.name = $"Laser_{i:D2}";
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.FromToRotation(Vector3.right, Vector3.down);

            laserInstances.Add(instance.transform);
            baseLocalRotations.Add(instance.transform.localRotation);
        }
    }

    private void UpdateSwing()
    {
        if (laserInstances.Count == 0)
        {
            return;
        }

        float time = Time.time;
        float speedRad = angularSpeed * Mathf.Deg2Rad;
        float phaseRad = phaseOffset * Mathf.Deg2Rad;

        for (int i = 0; i < laserInstances.Count; i++)
        {
            Transform laser = laserInstances[i];
            if (laser == null)
            {
                continue;
            }

            float angle = Mathf.Sin(time * speedRad + phaseRad * i) * swingAngle;
            laser.localRotation = baseLocalRotations[i] * Quaternion.AngleAxis(angle, Vector3.right);
        }
    }
}
