using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pendulum_swing : MonoBehaviour
{
    [SerializeField] private Transform pendulumParent;
    [SerializeField] private float maxAngle = 130f;
    [SerializeField] private float angularSpeed = 30f;

    private Quaternion initialLocalRotation;

    void Start()
    {
        if (pendulumParent == null)
        {
            pendulumParent = transform.parent;
        }

        initialLocalRotation = transform.localRotation;
    }

    // Update is called once per frame
    void Update()
    {
        float angle = Mathf.PingPong(Time.time * angularSpeed, maxAngle);

        if (pendulumParent != null)
        {
            // Align the swing axis with the parent's X axis.
            transform.rotation = pendulumParent.rotation * initialLocalRotation * Quaternion.AngleAxis(angle, Vector3.right);
        }
        else
        {
            transform.rotation = initialLocalRotation * Quaternion.AngleAxis(angle, Vector3.right);
        }
    }
}
