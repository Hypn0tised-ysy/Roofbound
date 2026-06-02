using UnityEngine;
using UnityEngine.UI;
using TMPro; // 引入文字插件

public class OptionsPanel : MonoBehaviour
{
    // ==================== 灵敏度设置（原有） ====================
    [Header("灵敏度设置")]
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityValueText; // 显示具体数字的Text

    private const string SENSITIVITY_KEY = "MouseSensitivity";

    public static float GlobalSensitivity { get; private set; } = 2.0f;

    // ==================== 音量设置（新增） ====================
    [Header("音量设置")]
    public Slider volumeSlider;                     // 音量滑动条
    public TextMeshProUGUI volumeValueText;         // 显示音量数值的Text

    private const string VOLUME_KEY = "MasterVolume";   // 存储到硬盘的Key
    public static float GlobalVolume { get; private set; } = 1.0f;   // 全局音量，0~1，其他脚本可读取

    private void Awake()
    {
        // 1. 游戏刚打开时，立刻从硬盘读取玩家存过的灵敏度值。如果没有，默认给 2.0f
        GlobalSensitivity = PlayerPrefs.GetFloat(SENSITIVITY_KEY, 2.0f);

        // 2. 音量不再读取存档用默认满值 1.0
        GlobalVolume = 1.0f;
        AudioListener.volume = 1.0f;   // 确保游戏启动时即满音量
    }

    private void Start()
    {
        // --- 初始化灵敏度滑动条（原有逻辑） ---
        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 0.5f;
            sensitivitySlider.maxValue = 5.0f;
            sensitivitySlider.value = GlobalSensitivity;
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }
        UpdateSensitivityText(GlobalSensitivity);

        // --- 初始化音量滑动条（新增） ---
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.SetValueWithoutNotify(1.0f);      // 满格，无回调
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        UpdateVolumeText(1.0f);
    }

    // ==================== 灵敏度回调（原有） ====================
    private void OnSensitivityChanged(float newValue)
    {
        // 1. 更新全局静态变量供外部(玩家控制器)实时读取
        GlobalSensitivity = newValue;

        // 2. 更新右侧数字的显示
        UpdateSensitivityText(newValue);

        // 3. 实时存入本地硬盘，确保退出不丢失
        PlayerPrefs.SetFloat(SENSITIVITY_KEY, GlobalSensitivity);
        PlayerPrefs.Save();
    }

    // ==================== 音量回调（新增） ====================
    private void OnVolumeChanged(float newValue)
    {
        GlobalVolume = newValue;
        AudioListener.volume = newValue;
        UpdateVolumeText(newValue);
    }

    // --- 将 0~1 的值直接应用到全局音频（新增） ---
    private void ApplyVolume(float normalized)
    {
        AudioListener.volume = normalized;   // 一行控制所有声音的大小
    }

    // --- 更新灵敏度数值显示（原名 UpdateValueText 改名，方便区分） ---
    private void UpdateSensitivityText(float val)
    {
        if (sensitivityValueText != null)
        {
            sensitivityValueText.text = val.ToString("F1"); // 保留1位小数 (如 2.5)
        }
    }

    // --- 更新音量数值显示（新增） ---
    private void UpdateVolumeText(float val)
    {
        if (volumeValueText != null)
        {
            // 显示为百分比，例如 80%
            volumeValueText.text = Mathf.RoundToInt(val * 100) + "%";
        }
    }

    // ==================== 面板控制（原有） ====================
    public void ShowPanel()
    {
        gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (UI_Controller.Instance != null)
        {
            UI_Controller.Instance.SetMenuPaused(true);
            UI_Controller.Instance.SetInputLocked(true);
        }
    }

    public void OnClickBack()
    {
        if (UI_Controller.Instance == null)
        {
            return;
        }

        gameObject.SetActive(false);
        UI_Controller.Instance.ShowMainMenu();
    }
}