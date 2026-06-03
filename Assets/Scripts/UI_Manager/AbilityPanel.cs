using UnityEngine;

public class AbilityPanel : MonoBehaviour
{
    private void OnEnable()
    {
        SkillSelectionStore.RefreshAllAbilitySlots();
    }

    public void ShowPanel()
    {
        gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (UI_Controller.Instance != null)
        {
            UI_Controller.Instance.SetMenuPaused(true);
            UI_Controller.Instance.SetInputLocked(true);
        }
    }

    public void OnClickBack()
    {
        if (UI_Controller.Instance == null)
        {
            return;
        }

        gameObject.SetActive(false);
        UI_Controller.Instance.ShowMainMenu();
    }
}
