using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class camera_spinning : MonoBehaviour
{
    [Tooltip("Cone half-angle in degrees.")]
    [SerializeField] private float angle = 30;

    [Tooltip("Angular speed in degrees per second.")]
    [SerializeField] private float speed = 10f;

    [Tooltip("Fixed offset added after reflection (degrees).")]
    [SerializeField] private float reflectionOffset = 15f;

    private Quaternion baseRotation;
    private Vector3 baseForward;
    private Vector3 baseUp;
    private Vector3 currentDir;
    private Vector3 turnAxis;

    void Start()
    {
        baseRotation = transform.rotation;
        baseForward = baseRotation * Vector3.forward;
        baseUp = baseRotation * Vector3.up;

        currentDir = GetRandomDirectionInCone();
        transform.rotation = Quaternion.LookRotation(currentDir, baseUp);
        turnAxis = GetRandomPerpendicularAxis(currentDir);
    }

    void Update()
    {
        float maxAngle = Mathf.Clamp(angle, 0f, 89.9f);
        float step = speed * Time.deltaTime;

        currentDir = transform.forward;
        Vector3 nextDir = Quaternion.AngleAxis(step, turnAxis) * currentDir;

        if (Vector3.Angle(baseForward, nextDir) > maxAngle)
        {
            ReflectTurnAxis(maxAngle);
            nextDir = Quaternion.AngleAxis(step, turnAxis) * currentDir;

            if (Vector3.Angle(baseForward, nextDir) > maxAngle)
            {
                nextDir = Vector3.RotateTowards(currentDir, baseForward, Mathf.Deg2Rad * step, 0f);
            }
        }

        transform.rotation = Quaternion.LookRotation(nextDir, baseUp);
    }

    private Vector3 GetRandomDirectionInCone()
    {
        float maxAngle = Mathf.Clamp(angle, 0f, 89.9f);
        float cosMax = Mathf.Cos(maxAngle * Mathf.Deg2Rad);

        float u = Random.value;
        float v = Random.value;

        float cosTheta = Mathf.Lerp(cosMax, 1f, u);
        float sinTheta = Mathf.Sqrt(1f - cosTheta * cosTheta);
        float phi = 2f * Mathf.PI * v;

        Vector3 localDir = new Vector3(
            sinTheta * Mathf.Cos(phi),
            sinTheta * Mathf.Sin(phi),
            cosTheta);

        return baseRotation * localDir;
    }

    private void ReflectTurnAxis(float maxAngle)
    {
        float maxAngleRad = maxAngle * Mathf.Deg2Rad;
        float cosMax = Mathf.Cos(maxAngleRad);

        Vector3 boundaryDir = Vector3.RotateTowards(baseForward, currentDir, maxAngleRad, 0f);
        Vector3 tangentNormal = baseForward - boundaryDir * cosMax;

        if (tangentNormal.sqrMagnitude < 0.0001f)
        {
            tangentNormal = GetRandomPerpendicularAxis(boundaryDir);
        }

        tangentNormal.Normalize();

        Vector3 tangentVelocity = Vector3.Cross(turnAxis, boundaryDir);
        Vector3 reflectedVelocity = Vector3.Reflect(tangentVelocity, tangentNormal);

        turnAxis = Vector3.Cross(boundaryDir, reflectedVelocity).normalized;
        turnAxis = Quaternion.AngleAxis(reflectionOffset, boundaryDir) * turnAxis;
    }

    private static Vector3 GetRandomPerpendicularAxis(Vector3 direction)
    {
        Vector3 random = Random.onUnitSphere;
        Vector3 axis = Vector3.Cross(direction, random);
        if (axis.sqrMagnitude < 0.0001f)
        {
            axis = Vector3.Cross(direction, Vector3.up);
        }
        return axis.normalized;
    }
}
