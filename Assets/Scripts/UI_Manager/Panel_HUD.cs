using UnityEngine;
using TMPro; // 如果使用 TextMeshPro

public class Panel_HUD : MonoBehaviour
{
    [Header("UI 引用")]
    public TextMeshProUGUI timerText; // 去 Unity 拖拽显示时间的 Text

    private float currentRunTime = 0f;

    private void OnEnable()
    {
        // HUD 每次随关卡开始显示时清零（UIManager 跨场景保留，需手动重置）
        currentRunTime = 0f;
        RefreshTimerText();
    }

    private void Update()
    {
        // timeScale=0（暂停/结算）时不累加，与屏幕计时一致
        if (Time.timeScale <= 0f)
        {
            return;
        }

        currentRunTime += Time.deltaTime;
        RefreshTimerText();
    }

    public float GetFinalTime()
    {
        return currentRunTime;
    }

    public static string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60F);
        int secs = Mathf.FloorToInt(seconds % 60F);
        int milliseconds = Mathf.FloorToInt((seconds * 100F) % 100F);
        return string.Format("{0:00}:{1:00}.{2:00}", minutes, secs, milliseconds);
    }

    private void RefreshTimerText()
    {
        if (timerText != null)
        {
            timerText.text = FormatTime(currentRunTime);
        }
    }
}