using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class laserGridGnerator : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject laserGridStand;
    [SerializeField] private GameObject laserGridStandNoLaser;

    [Header("Layout")]
    [SerializeField] private int standCount = 7;
    [SerializeField] private float yOffset = 2.9f;
    [SerializeField] private float xOffset = 100f;

    private void Start()
    {
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        if (standCount <= 0)
        {
            return;
        }

        if (laserGridStand == null || laserGridStandNoLaser == null)
        {
            Debug.LogError("[laserGridGnerator] 未绑定激光立柱预制体。", this);
            return;
        }

        Vector3 basePosition = transform.position;
        Quaternion baseRotation = transform.rotation;
        Quaternion flippedRotation = baseRotation * Quaternion.Euler(0f, 180f, 0f);

        for (int i = 0; i < standCount; i++)
        {
            Vector3 offset = transform.up * (yOffset * i);
            Vector3 position = basePosition + offset;

            GameObject standA = Instantiate(laserGridStand, position, baseRotation, transform);
            standA.name = $"LaserGridStand_{i:D2}";

            Vector3 positionB = position + transform.right * (-xOffset);
            GameObject standB = Instantiate(laserGridStandNoLaser, positionB, flippedRotation, transform);
            standB.name = $"LaserGridStand_NoLaser_{i:D2}";
        }
    }
}
