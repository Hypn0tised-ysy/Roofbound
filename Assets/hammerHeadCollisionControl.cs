using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class hammerHeadCollisionControl : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private levelController levelControllerRef;
    [SerializeField] private Rigidbody hammerRigidbody;
    [SerializeField] private Transform pendulumPivot;

    [Header("Tags")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string truckTag = "truck";

    [Header("Truck Hit")]
    [SerializeField] private float truckHitForce = 9000f;
    [SerializeField] private ForceMode truckForceMode = ForceMode.Impulse;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private void Awake()
    {
        if (levelControllerRef == null)
        {
            levelControllerRef = FindObjectOfType<levelController>();
        }

        if (hammerRigidbody == null)
        {
            hammerRigidbody = GetComponentInParent<Rigidbody>();
        }
    }

    public void HandleCollision(Collider targetCollider)
    {
        if (targetCollider == null)
        {
            return;
        }

        if (debugLog)
        {
            Debug.Log($"[hammerHeadCollisionControl] Collision with {targetCollider.name}, tag={targetCollider.tag}");
        }

        if (targetCollider.CompareTag(playerTag))
        {
            HandlePlayerCollision(targetCollider);
            return;
        }

        if (IsTruckTarget(targetCollider))
        {
            HandleTruckCollision(targetCollider);
        }
    }

    private void HandlePlayerCollision(Collider targetCollider)
    {
        if (levelControllerRef == null || targetCollider == null)
        {
            return;
        }

        levelControllerRef.NotifyPlayerKilledByHazard(targetCollider.gameObject);
    }

    private void HandleTruckCollision(Collider targetCollider)
    {
        if (targetCollider == null)
        {
            return;
        }

        Rigidbody truckBody = targetCollider.attachedRigidbody;
        if (truckBody == null)
        {
            return;
        }

        Vector3 tangent = GetSwingTangent(targetCollider.transform.position);
        truckBody.AddForce(tangent * truckHitForce, truckForceMode);
    }

    private bool IsTruckTarget(Collider targetCollider)
    {
        Transform targetTransform = targetCollider.transform;
        Transform tagChild = targetTransform.Find("isTruckTag");
        if (tagChild == null)
        {
            return false;
        }

        return tagChild.CompareTag(truckTag);
    }

    private Vector3 GetSwingTangent(Vector3 hitPosition)
    {
        if (hammerRigidbody != null && hammerRigidbody.velocity.sqrMagnitude > 0.0001f)
        {
            Vector3 velocityDir = hammerRigidbody.velocity.normalized;

            if (pendulumPivot != null)
            {
                Vector3 radius = (hitPosition - pendulumPivot.position).normalized;
                Vector3 projected = Vector3.ProjectOnPlane(velocityDir, radius);
                if (projected.sqrMagnitude > 0.0001f)
                {
                    return projected.normalized;
                }
            }

            return velocityDir;
        }

        return transform.right;
    }
}
