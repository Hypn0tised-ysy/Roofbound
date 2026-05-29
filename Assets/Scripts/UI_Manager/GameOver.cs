using UnityEngine;

public class GameOver : MonoBehaviour
{
    [SerializeField] private bool restartOnAnyKey = true;
    private bool isVisible;
    // ================= 给总控 UI_Controller 调用的方法 =================

    public void ShowGameOver()
    {
        gameObject.SetActive(true); // 显示自己
        isVisible = true;

        // 玩家死了，需要把鼠标指针还给玩家，方便点击按钮
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetMenuPaused(false);
            UIManager.Instance.SetInputLocked(true);
        }
    }

    public void HidePanel()
    {
        gameObject.SetActive(false); // 隐藏自己
        isVisible = false;
    }

    // ================= 给 UI 按钮绑定的点击事件 =================

    // 当玩家点击 "Restart" (重开) 按钮时触发
    public void OnClickRestart()
    {
        HidePanel(); // 自己隐藏

        // 通知总控：重新开始游戏流程！
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RestartLevel();
        }

        // 【重要联调点】：这里后续需要发消息给 GameManager，告诉它重新加载当前关卡
        Debug.Log("UI -> 发送重新加载当前关卡的指令！");
    }

    // 当玩家点击 "Main Menu" (返回主菜单) 按钮时触发
    public void OnClickMainMenu()
    {
        HidePanel(); // 自己隐藏

        // 通知主菜单面板显示出来
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMainMenu();
        }

        // 【重要联调点】：这里后续需要发消息给 GameManager 清理当前场景数据
        Debug.Log("UI -> 玩家放弃重试，返回主菜单！");
    }

    private void Update()
    {
        if (!restartOnAnyKey || !isVisible)
        {
            return;
        }

        if (Input.anyKeyDown)
        {
            OnClickRestart();
        }
    }
}