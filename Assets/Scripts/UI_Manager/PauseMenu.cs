using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public void OnClickResume()
    {
        if (UIManager.Instance == null)
        {
            return;
        }

        UIManager.Instance.HidePauseMenu();
    }

    public void OnClickRestart()
    {
        if (UIManager.Instance == null)
        {
            // still reload the scene even if UIManager missing
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        UIManager.Instance.HidePauseMenu();
        UIManager.Instance.StartGameplay();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnClickLevelSelect()
    {
        // When selecting level from pause menu, go back to main scene then show level select
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HidePauseMenu();
            UIManager.Instance.ShowLevelSelect();
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("main");
    }

    public void OnClickMainMenu()
    {
        // Request the UIManager to show MainMenu after the main scene loads, then load it.
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RequestShowMainMenuOnNextSceneLoad();
        }

        SceneManager.LoadScene("main");
    }
}
