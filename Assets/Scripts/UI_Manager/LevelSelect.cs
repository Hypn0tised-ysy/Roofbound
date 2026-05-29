using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelSelect : MonoBehaviour
{
    [Header("关卡列表")]
    [SerializeField] private string[] levelSceneNames;
    [SerializeField] private int defaultLevelIndex = 0;

    private int selectedLevelIndex;

    private void Awake()
    {
        selectedLevelIndex = Mathf.Clamp(defaultLevelIndex, 0, Mathf.Max(0, levelSceneNames.Length - 1));
    }

    // 点击 "Back" 按钮绑定此方法
    public void OnClickBack()
    {
        if (UI_Controller.Instance == null)
        {
            return;
        }

        gameObject.SetActive(false);
        UI_Controller.Instance.ShowMainMenu();
    }

    public void OnClickSelectLevel(int index)
    {
        if (levelSceneNames == null || levelSceneNames.Length == 0)
        {
            return;
        }

        selectedLevelIndex = Mathf.Clamp(index, 0, levelSceneNames.Length - 1);
    }

    public void OnClickPlay()
    {
        if (UI_Controller.Instance == null)
        {
            return;
        }

        gameObject.SetActive(false);
        UI_Controller.Instance.StartGameplay();

        if (levelSceneNames != null && levelSceneNames.Length > 0)
        {
            string sceneName = levelSceneNames[Mathf.Clamp(selectedLevelIndex, 0, levelSceneNames.Length - 1)];
            if (!string.IsNullOrEmpty(sceneName))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
        }
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
}
