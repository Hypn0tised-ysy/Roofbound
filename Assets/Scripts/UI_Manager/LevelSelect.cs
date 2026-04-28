using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelSelect : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // 点击 "Back" 按钮绑定此方法
    public void OnClickBack()
    {
        gameObject.SetActive(false);
        UI_Controller.Instance.MainMenu.ShowPanel();
    }

    // 点击 "Level 1" 按钮绑定此方法
    public void OnClickLevel1()
    {
        gameObject.SetActive(false);
        // 通知总控：开始游戏！
        UI_Controller.Instance.StartGameplay();

        Debug.Log("UI -> 发送加载 Level 1 的指令给 GameManager");
    }


    public void ShowPanel()
    {
        gameObject.SetActive(true);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
