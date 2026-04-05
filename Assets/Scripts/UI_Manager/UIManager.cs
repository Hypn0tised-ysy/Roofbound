using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    // 单例模式，方便其他脚本极速调用
     public static UIManager Instance;

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject hudPanel;
    public GameObject gameOverPanel;

    [Header("HUD Elements")]
    public TextMeshProUGUI timerText; // 做计时器用

    // Start is called before the first frame update
    void Start()
    {
        // 游戏刚启动时，只显示主菜单
        ShowMainMenu();
    }
    private void Update()
    {
        // 测试代码：如果在键盘上按下了 'K' 键 (Kill)
        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log("测试：模拟玩家死亡！");
            ShowGameOver(); // 强行调用死亡界面
        }
    }
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    // Update is called once per frame

    // ================= 公开接口 (给其他成员调用的方法) =================

    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        hudPanel.SetActive(false);
        gameOverPanel.SetActive(false);

        // 解锁鼠标并显示，方便玩家点按钮
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ShowHUD()
    {
        mainMenuPanel.SetActive(false);
        hudPanel.SetActive(true);
        gameOverPanel.SetActive(false);

        // 锁定鼠标并隐藏 ( FPS 控制器刚需)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ShowGameOver()
    {
        mainMenuPanel.SetActive(false);
        hudPanel.SetActive(false);
        gameOverPanel.SetActive(true);

        // 玩家死了，把鼠标还给他们点重试
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ================= 按钮点击事件绑定 =================

    public void OnStartButtonClicked()
    {
        ShowHUD();
        // 通知 GameManager 开始游戏 (你可以先打个日志)
        Debug.Log("UI: 告诉 GameManager 开始游戏啦！");
    }

    public void OnRestartButtonClicked()
    {
        ShowHUD();
        // 通知 GameManager 重新加载场景
        Debug.Log("UI: 告诉 GameManager 重开啦！");
    }
    // 绑定给 "返回主菜单" 按钮
    public void OnMainMenuButtonClicked()
    {
        // 1. 切换回主菜单界面
        ShowMainMenu();

        // 2. 这里后续需要通知 C同学的 GameManager 清理场景等
        Debug.Log("UI: 返回主菜单！");
    }
}
