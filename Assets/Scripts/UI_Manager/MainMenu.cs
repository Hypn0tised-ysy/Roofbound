using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // 点击 "Start Game" 按钮绑定此方法
    public void OnClickStart()
    {
        // 自己隐藏，请求总控打开选关界面
        gameObject.SetActive(false);
        UI_Controller.Instance.LevelSelect.ShowPanel();
    }

    public void ShowPanel()
    {
        gameObject.SetActive(true);
        // 主菜单需要鼠标操作
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
