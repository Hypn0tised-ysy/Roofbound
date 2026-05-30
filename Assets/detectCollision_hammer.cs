using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class detectCollision_hammer : MonoBehaviour
{
    [SerializeField] private hammerHeadCollisionControl collisionControl;

    private void Awake()
    {
        if (collisionControl == null)
        {
            collisionControl = GetComponentInParent<hammerHeadCollisionControl>();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collisionControl == null || collision == null || collision.collider == null)
        {
            return;
        }

        collisionControl.HandleCollision(collision.collider);
    }
}
