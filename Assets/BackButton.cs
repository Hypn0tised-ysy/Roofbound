using UnityEngine;

public class BackButton : MonoBehaviour
{
    public void OnClickBack()
    {
        if (UIManager.Instance == null)
        {
            return;
        }

        UIManager.Instance.GoBack();
    }
}
