using UnityEngine;

public class followPlayer : MonoBehaviour
{
    public Transform player;

    private void LateUpdate()
    {
        if (player == null)
        {
            return;
        }

        transform.position = player.position;
    }
}
