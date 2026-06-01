using UnityEngine;
using TMPro;
using DG.Tweening; // 神级动效插件

public class LevelComplete : MonoBehaviour
{
    [Header("UI 引用")]
    public TextMeshProUGUI finalTimeText; // 拖拽结算成绩的 Text
    public Transform panelContent; 

    // 改动：删除了 ShowVictory()。
    // 原因：在你的架构中，UIManager.SetState(UIState.Finished) 会自动把这个物体 SetActive(true)。
    // 所以，我们直接用 Unity 的 OnEnable() 生命周期来拦截面板弹出的瞬间！
    private void OnEnable()
    {
        // 1. 尝试去获取 HUD 里的最终成绩
        float finalTime = 0f;
        Panel_HUD hud = FindObjectOfType<Panel_HUD>();
        if (hud != null)
        {
            finalTime = hud.GetFinalTime();
        }

        // 2. 格式化并显示时间
        int m = Mathf.FloorToInt(finalTime / 60F);
        int s = Mathf.FloorToInt(finalTime % 60F);
        int ms = Mathf.FloorToInt((finalTime * 100F) % 100F);

        if (finalTimeText != null)
        {
            finalTimeText.text = "Your Time: " + string.Format("{0:00}:{1:00}.{2:00}", m, s, ms);
        }

        // ================= 🪄 魔法动效时间 =================
        if (panelContent != null)
        {
            // 杀掉之前的动画防止鬼畜
            panelContent.DOKill();

            // a. 瞬间缩小为 0 (肉眼看不见)
            panelContent.localScale = Vector3.zero;

            // b. 花 0.5 秒，用带弹簧效果的曲线 (OutBack) 弹出来
            // 🔴 必须加 .SetUpdate(true)，因为 UIManager 会在结算时暂停游戏(Time.timeScale=0)
            panelContent.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    // ================== 按钮点击事件 ==================

    // 点击 "Main Menu" 按钮 — 返回主菜单场景
    public void OnClickMainMenu()
    {
        gameObject.SetActive(false);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RequestShowMainMenuOnNextSceneLoad();
        }

        // 注意：因为游戏在结算时是被暂停的 (Time.timeScale = 0)，切场景前必须恢复时间！
        Time.timeScale = 1f;

        UnityEngine.SceneManagement.SceneManager.LoadScene("main");
    }

    // 点击 "Next Level" 按钮
    public void OnClickNextLevel()
    {
        gameObject.SetActive(false);

        // 切关卡前，也必须把时间恢复正常流动！
        Time.timeScale = 1f;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.PlayNextLevel();
        }
        Debug.Log("UI -> 请求加载下一关");
    }
}