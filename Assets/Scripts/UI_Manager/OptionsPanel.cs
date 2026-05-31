using UnityEngine;
using UnityEngine.UI;
using TMPro; // 引入文字插件


public class OptionsPanel : MonoBehaviour
{
    [Header("UI 组件引用")]
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityValueText; // 显示具体数字的Text

    // 存储到电脑硬盘的 Key (键名)
    private const string SENSITIVITY_KEY = "MouseSensitivity";

    // 全局静态变量，专门留给 A同学的 playerControl 读取
    // 使用方法：Panel_Options.GlobalSensitivity
    public static float GlobalSensitivity { get; private set; } = 2.0f;

    private void Awake()
    {
        // 1. 游戏刚打开时，立刻从硬盘读取玩家存过的值。如果没有，默认给 2.0f
        GlobalSensitivity = PlayerPrefs.GetFloat(SENSITIVITY_KEY, 2.0f);
    }

    private void Start()
    {
        // 2. 初始化滑动条的极值
        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 0.5f;
            sensitivitySlider.maxValue = 5.0f;

            // 把读取到的值赋给滑动条
            sensitivitySlider.value = GlobalSensitivity;

            // 监听滑动条的拖动事件
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        // 初始化数值文本的显示
        UpdateValueText(GlobalSensitivity);
    }

    // 当滑动条被拖动时，会自动调用这个方法
    private void OnSensitivityChanged(float newValue)
    {
        // 1. 更新全局静态变量供外部(玩家控制器)实时读取
        GlobalSensitivity = newValue;

        // 2. 更新右侧数字的显示
        UpdateValueText(newValue);

        // 3. 实时存入本地硬盘，确保退出不丢失
        PlayerPrefs.SetFloat(SENSITIVITY_KEY, GlobalSensitivity);
        PlayerPrefs.Save();
    }

    private void UpdateValueText(float val)
    {
        if (sensitivityValueText != null)
        {
            sensitivityValueText.text = val.ToString("F1"); // 保留1位小数 (如 2.5)
        }
    }

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
