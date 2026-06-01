using UnityEngine;
using TMPro; // 如果使用 TextMeshPro

public class Panel_HUD : MonoBehaviour
{
    [Header("UI 引用")]
    public TextMeshProUGUI timerText; // 去 Unity 拖拽显示时间的 Text

    private float currentRunTime = 0f;

    // 当面板被 UIManager 显示出来时（意味着游戏开始或恢复）
    private void OnEnable()
    {
        // 这里不需要写额外逻辑，因为重新开始关卡会重置场景
    }

    private void Update()
    {
        // 累加真实时间。当游戏暂停(Time.timeScale=0)时，这个值会自动停止增加！
        currentRunTime += Time.deltaTime;

        // 格式化为 00:00.00
        int minutes = Mathf.FloorToInt(currentRunTime / 60F);
        int seconds = Mathf.FloorToInt(currentRunTime % 60F);
        int milliseconds = Mathf.FloorToInt((currentRunTime * 100F) % 100F);

        if (timerText != null)
        {
            timerText.text = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
        }
    }

    // 供结算面板 (FinishMenu) 调用，获取最终成绩
    public float GetFinalTime()
    {
        return currentRunTime;
    }
}