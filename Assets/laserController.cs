using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class laserController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform beamStartPoint;
    [SerializeField] private Transform hitPoint;
    [SerializeField] private LineRenderer laserBeam;
    [SerializeField] private GameObject endHitVfx;
    [SerializeField] private levelController levelControllerRef;

    [Header("Raycast")]
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Tags")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string[] platformTags = new string[] { "truck", "ground", "platform" };

    private ParticleSystem hitVfxParticle;

    private void Awake()
    {
        if (beamStartPoint == null)
        {
            Transform child = transform.Find("BeamStartPoint");
            if (child != null)
            {
                beamStartPoint = child;
            }
        }

        if (hitPoint == null)
        {
            Transform child = transform.Find("HitPoint");
            if (child != null)
            {
                hitPoint = child;
            }
        }

        if (laserBeam == null)
        {
            Transform child = transform.Find("LaserBeam");
            if (child != null)
            {
                laserBeam = child.GetComponent<LineRenderer>();
            }
        }

        if (endHitVfx == null)
        {
            Transform child = transform.Find("endHitVfx");
            if (child != null)
            {
                endHitVfx = child.gameObject;
            }
        }

        if (endHitVfx != null)
        {
            hitVfxParticle = endHitVfx.GetComponent<ParticleSystem>();
        }

        if (levelControllerRef == null)
        {
            levelControllerRef = FindObjectOfType<levelController>();
        }

        if (laserBeam != null)
        {
            laserBeam.positionCount = 2;
        }

        SetHitVfxActive(false);
    }

    private void Update()
    {
        if (beamStartPoint == null)
        {
            return;
        }

        Vector3 origin = beamStartPoint.position;
        Vector3 direction = transform.right;

        RaycastHit hit;
        bool hasHit = Physics.Raycast(origin, direction, out hit, maxDistance, hitMask, QueryTriggerInteraction.Ignore);

        if (hasHit)
        {
            UpdateHitPoint(hit.point);
            UpdateBeam(origin, hit.point);

            if (hit.collider != null)
            {
                if (hit.collider.CompareTag(playerTag))
                {
                    HandlePlayerHit(hit.collider.gameObject);
                }
                else if (IsPlatformTag(hit.collider.tag))
                {
                    SetHitVfxActive(true);
                }
                else
                {
                    SetHitVfxActive(true);
                }
            }
            else
            {
                SetHitVfxActive(true);
            }
        }
        else
        {
            Vector3 endPoint = origin + direction * maxDistance;
            UpdateHitPoint(endPoint);
            UpdateBeam(origin, endPoint);
            SetHitVfxActive(false);
        }
    }

    private void UpdateHitPoint(Vector3 position)
    {
        if (hitPoint != null)
        {
            hitPoint.position = position;
        }

        if (endHitVfx != null)
        {
            endHitVfx.transform.position = position;
        }
    }

    private void UpdateBeam(Vector3 start, Vector3 end)
    {
        if (laserBeam == null)
        {
            return;
        }

        laserBeam.SetPosition(0, start);
        laserBeam.SetPosition(1, end);
    }

    private void SetHitVfxActive(bool active)
    {
        if (endHitVfx != null)
        {
            endHitVfx.SetActive(active);
            if (active && hitVfxParticle != null)
            {
                hitVfxParticle.Play();
            }
        }
    }

    private void HandlePlayerHit(GameObject player)
    {
        if (levelControllerRef == null || player == null)
        {
            return;
        }

        levelControllerRef.NotifyPlayerKilledByHazard(player);
    }

    private bool IsPlatformTag(string tagValue)
    {
        if (platformTags == null)
        {
            return false;
        }

        for (int i = 0; i < platformTags.Length; i++)
        {
            if (tagValue == platformTags[i])
            {
                return true;
            }
        }

        return false;
    }
}
