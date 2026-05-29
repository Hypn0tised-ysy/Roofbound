using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class HUD : MonoBehaviour
{
    public TextMeshProUGUI timerText; // 去 Inspector 拖拽对应的 Text
    public Slider dashCooldownSlider;

    private float currentTime = 0f;
    private bool isRunning = false;
    // 新增：冲刺冷却变量
    private bool isDashCoolingDown = false;
    private float maxDashCooldown = 0f;
    private float currentDashTimer = 0f;

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

    // 提供给总控调用的接口：告诉 UI 冲刺了，冷却要多久
    public void TriggerDashCooldownUI(float cooldownDuration)
    {
        maxDashCooldown = cooldownDuration;
        currentDashTimer = 0f; // 时间清零
        dashCooldownSlider.value = 0f; // 进度条瞬间抽空

        isDashCoolingDown = true; // 开启自己内部的倒计时
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

        //  进度条自身的平滑填充动画
        if (isDashCoolingDown)
        {
            // 累加冷却时间
            currentDashTimer += Time.deltaTime;

            // 计算当前百分比 (0 到 1 之间)，赋给进度条
            float fillPercentage = currentDashTimer / maxDashCooldown;
            dashCooldownSlider.value = fillPercentage;

            // 当进度条满了，停止冷却动画
            if (currentDashTimer >= maxDashCooldown)
            {
                dashCooldownSlider.value = 1f; // 确保完全填满
                isDashCoolingDown = false;
            }
        }
    }
}