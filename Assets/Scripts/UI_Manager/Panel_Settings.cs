using UnityEngine;
using UnityEngine.UI;
using TMPro; // 引入文字插件

public class Panel_Settings : MonoBehaviour
{
    [Header("UI 引用")]
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityValueText; // 显示具体数字的Text

    // 定义一个静态常量，作为存储到电脑硬盘的 Key (键名)
    private const string SENSITIVITY_KEY = "MouseSensitivity";

    // 全局静态变量，留给PlayerController 读取
    public static float GlobalSensitivity = 2.0f;

    private void Start()
    {
        // 1. 设置滑动条的极值 (比如灵敏度最低 0.5，最高 5.0)
        sensitivitySlider.minValue = 0.5f;
        sensitivitySlider.maxValue = 5.0f;

        // 2. 游戏刚打开时，尝试从电脑硬盘读取玩家以前存过的值。如果没有，默认给 2.0f
        GlobalSensitivity = PlayerPrefs.GetFloat(SENSITIVITY_KEY, 2.0f);

        // 3. 把读取到的值，赋给滑动条，让滑动条的位置正确
        sensitivitySlider.value = GlobalSensitivity;
        sensitivityValueText.text = GlobalSensitivity.ToString("F1"); // F1代表保留1位小数

        // 4. 监听滑动条的拖动事件 (一旦拖动，就执行 OnSensitivityChanged 方法)
        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
    }

    // 当滑动条被拖动时，会自动调用这个方法
    private void OnSensitivityChanged(float newValue)
    {
        // 1. 更新静态变量供外部读取
        GlobalSensitivity = newValue;

        // 2. 更新右侧数字的显示
        sensitivityValueText.text = newValue.ToString("F1");

        // 3. 实时存入本地硬盘！这样强退游戏也不会丢失设置
        PlayerPrefs.SetFloat(SENSITIVITY_KEY, GlobalSensitivity);
        PlayerPrefs.Save();
    }

    // ====== 控制面板开关 ======
    public void ShowPanel()
    {
        gameObject.SetActive(true);
    }

    // 绑定给 Back 按钮
    public void OnClickBack()
    {
        gameObject.SetActive(false);
        // 通知总控器，打开主菜单
        UI_Controller.Instance.MainMenu.ShowPanel();
    }
}