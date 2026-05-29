using UnityEngine;
using UnityEngine.UIElements;

public class UI_Controller : MonoBehaviour
{
    public static UI_Controller Instance; // 全局唯一访问点

    [Header("子面板组件引用 (拖拽挂有对应脚本的 Panel)")]
    public MainMenu MainMenu;
    public LevelSelect LevelSelect;
    public HUD HUD;
    public LevelComplete LevelComplete;
    public GameOver GameOver;
    public Panel_Settings Panel_Settings;
    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        // 游戏启动：关闭所有面板，只打开主菜单
        LevelSelect.gameObject.SetActive(false);
        HUD.gameObject.SetActive(false);
        LevelComplete.gameObject.SetActive(false);
        Panel_Settings.gameObject.SetActive(false);
        GameOver.HidePanel();
        MainMenu.ShowPanel();
    }

    // ================== 给外部(如按键事件、GameManager)调用的核心接口 ==================

    public void StartGameplay()
    {
        // 游戏开始，通知 HUD 启动
        HUD.StartHUD();
    }

    public void TriggerVictory()
    {
        // 1. 从 HUD 抢下最终时间并让 HUD 滚蛋
        float finalTime = HUD.StopHUDAndGetTime();

        // 2. 把时间喂给结算面板，让它出来接客
        LevelComplete.ShowVictory(finalTime);
    }

    public void TriggerGameOver()
    {
        // 死亡时，直接强制关闭 HUD
        HUD.StopHUDAndGetTime();

        // 弹出死亡面板并呼出鼠标
        GameOver.ShowGameOver();
    }

    // 供 (PlayerController) 冲刺时调用！
    public void UseDashSkill(float cooldownTime)
    {
        // 总控直接使唤 HUD 面板开始干活
        HUD.TriggerDashCooldownUI(cooldownTime);
    }

    // ================== 临时测试代码 (V:胜利, K:死亡) ==================
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            TriggerVictory();
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            TriggerGameOver();
        }
        // 新增模拟冲刺按键
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            // 模拟玩家按下了冲刺，假设技能冷却需要 3.5 秒
            UseDashSkill(3.5f);
            Debug.Log("测试：玩家冲刺！进度条开始 3.5 秒的冷却！");
        }
    }
}