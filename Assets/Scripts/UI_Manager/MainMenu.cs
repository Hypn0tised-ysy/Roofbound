using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    // 点击 "Play" 按钮绑定此方法
    public void OnClickPlay()
    {
        if (UI_Controller.Instance == null)
        {
            return;
        }

        gameObject.SetActive(false);
        UI_Controller.Instance.ShowLevelSelect();
    }

    // 点击 "Abilities" 按钮绑定此方法
    public void OnClickAbilities()
    {
        if (UI_Controller.Instance == null)
        {
            return;
        }

        gameObject.SetActive(false);
        UI_Controller.Instance.ShowAbilityPanel();
    }

    // 点击 "Options" 按钮绑定此方法
    public void OnClickOptions()
    {
        if (UI_Controller.Instance == null)
        {
            return;
        }

        gameObject.SetActive(false);
        UI_Controller.Instance.ShowOptionsPanel();
    }

    // 点击 "Quit" 按钮绑定此方法
    public void OnClickQuit()
    {
        Application.Quit();
    }

    public void ShowPanel()
    {
        gameObject.SetActive(true);
        // 主菜单需要鼠标操作
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (UI_Controller.Instance != null)
        {
            UI_Controller.Instance.SetMenuPaused(true);
            UI_Controller.Instance.SetInputLocked(true);
        }
    }
}
