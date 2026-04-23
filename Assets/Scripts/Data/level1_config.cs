using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "level1_config", menuName = "Roofbound/Level1 Config")]
public class level1_config : ScriptableObject
{
	[Header("卡车配置")]
	public GameObject truckPrefab;
	public List<TruckInitialState> truckInitialStates = new List<TruckInitialState>();

	[Header("主角配置")]
	public GameObject mainCharacterPrefab;
}

[Serializable]
public struct TruckInitialState
{
	public Vector3 position;
	public Vector3 eulerRotation;
	public bool overrideSpeed;
	public float speed;
}
