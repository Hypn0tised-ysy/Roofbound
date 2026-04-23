using UnityEngine;

[RequireComponent(typeof(Collider))]
public class destination : MonoBehaviour
{
    [Header("检测设置")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private levelController levelControllerRef;
    [SerializeField] private bool forceForwardOnAwake = true;

    private bool hasDetected;

    private void Awake()
    {
        if (forceForwardOnAwake)
        {
            transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        }

        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }

        if (levelControllerRef == null)
        {
            levelControllerRef = FindObjectOfType<levelController>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasDetected)
        {
            return;
        }

        if (!other.CompareTag(playerTag))
        {
            return;
        }

        hasDetected = true;

        if (levelControllerRef == null)
        {
            Debug.LogError("[destination] 未找到 levelController，无法触发 game_finish。");
            return;
        }

        levelControllerRef.NotifyPlayerReachedDestination(other.gameObject);
    }
}
