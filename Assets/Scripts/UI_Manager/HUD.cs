using UnityEngine;
using TMPro;

public class HUD : MonoBehaviour
{
    public TextMeshProUGUI timerText; // 去 Inspector 拖拽对应的 Text

    private float currentTime = 0f;
    private bool isRunning = false;

    // 游戏开始时调用
    public void StartHUD()
    {
        gameObject.SetActive(true);
        currentTime = 0f;
        isRunning = true;

        // 游戏内隐藏鼠标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // 游戏结束(通关/死亡)时调用，并返回当前耗时
    public float StopHUDAndGetTime()
    {
        gameObject.SetActive(false);
        isRunning = false;
        return currentTime;
    }

    private void Update()
    {
        if (isRunning)
        {
            currentTime += Time.deltaTime;

            // 毫秒级格式化: 00:00.00
            int m = Mathf.FloorToInt(currentTime / 60F);
            int s = Mathf.FloorToInt(currentTime % 60F);
            int ms = Mathf.FloorToInt((currentTime * 100F) % 100F);
            timerText.text = string.Format("{0:00}:{1:00}.{2:00}", m, s, ms);
        }
    }
}