using UnityEngine;
using DG.Tweening; // 引入神级动效插件

public class GameOver : MonoBehaviour
{
    [SerializeField] private bool restartOnAnyKey = true;
    public Transform panelContent; 

    private bool isVisible;

    // 核心改动：删除了 ShowGameOver() 与 HidePanel()。
    // 因为 UIManager.SetState(UIState.Dead) 会自动控制此面板的 SetActive()。
    // 我们用 OnEnable() 来拦截显示瞬间，并触发动效！
    private void OnEnable()
    {
        isVisible = true;

        // ================= 🪄 魔法动效时间 =================
        if (panelContent != null)
        {
            // 杀掉之前的动画防止鬼畜
            panelContent.DOKill();

            // a. 瞬间缩小为 0 (肉眼看不见)
            panelContent.localScale = Vector3.zero;

            // b. 花 0.4 秒，用带弹簧效果的曲线 (OutBack) 弹出来
            // 死亡画面不要拖泥带水，所以时间设为 0.4f
            panelContent.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    private void OnDisable()
    {
        isVisible = false;
    }

    // ================= 给 UI 按钮绑定的点击事件 =================

    // 当玩家点击 "Restart" (重开) 按钮时触发
    public void OnClickRestart()
    {
        // 注意：不再需要写 HidePanel() 和 gameObject.SetActive(false)
        // 因为调用 RestartLevel 时，UIManager 会自动清场！

        if (UIManager.Instance != null)
        {
            UIManager.Instance.RestartLevel();
            Debug.Log("UI -> 发送重新加载当前关卡的指令！");
        }
    }

    // 当玩家点击 "Main Menu" (返回主菜单) 按钮时触发
    public void OnClickMainMenu()
    {
        if (UIManager.Instance != null)
        {
            // 给总管留纸条，下个场景切到 main
            UIManager.Instance.RequestShowMainMenuOnNextSceneLoad();
        }

        // 死亡时时间是正常的，所以直接加载场景即可
        UnityEngine.SceneManagement.SceneManager.LoadScene("main");

        Debug.Log("UI -> 玩家放弃重试，返回主菜单！");
    }

    // 跑酷游戏神级体验：按任意键光速重开
    private void Update()
    {
        if (!restartOnAnyKey || !isVisible)
        {
            return;
        }

        // 如果按下了任意键，立刻重开！
        if (Input.anyKeyDown)
        {
            OnClickRestart();
        }
    }
}