using UnityEngine;

public class GameOver : MonoBehaviour
{
    // ================= 给总控 UI_Controller 调用的方法 =================

    public void ShowGameOver()
    {
        gameObject.SetActive(true); // 显示自己

        // 玩家死了，需要把鼠标指针还给玩家，方便点击按钮
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void HidePanel()
    {
        gameObject.SetActive(false); // 隐藏自己
    }

    // ================= 给 UI 按钮绑定的点击事件 =================

    // 当玩家点击 "Restart" (重开) 按钮时触发
    public void OnClickRestart()
    {
        HidePanel(); // 自己隐藏

        // 通知总控：重新开始游戏流程！
        UI_Controller.Instance.StartGameplay();

        // 【重要联调点】：这里后续需要发消息给 GameManager，告诉它重新加载当前关卡
        Debug.Log("UI -> 发送重新加载当前关卡的指令！");
    }

    // 当玩家点击 "Main Menu" (返回主菜单) 按钮时触发
    public void OnClickMainMenu()
    {
        HidePanel(); // 自己隐藏

        // 通知主菜单面板显示出来
        UI_Controller.Instance.MainMenu.ShowPanel();

        // 【重要联调点】：这里后续需要发消息给 GameManager 清理当前场景数据
        Debug.Log("UI -> 玩家放弃重试，返回主菜单！");
    }
}