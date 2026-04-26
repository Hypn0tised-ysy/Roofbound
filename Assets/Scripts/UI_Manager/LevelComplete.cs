using UnityEngine;
using TMPro;

public class LevelComplete : MonoBehaviour
{
    public TextMeshProUGUI finalTimeText; // 拖拽结算成绩的 Text

    // 被总控调用，传入最终成绩
    public void ShowVictory(float finalTime)
    {
        gameObject.SetActive(true);

        int m = Mathf.FloorToInt(finalTime / 60F);
        int s = Mathf.FloorToInt(finalTime % 60F);
        int ms = Mathf.FloorToInt((finalTime * 100F) % 100F);
        finalTimeText.text = "Your Time: " + string.Format("{0:00}:{1:00}.{2:00}", m, s, ms);

        // 呼出鼠标点按钮
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 点击 "Main Menu" 按钮
    public void OnClickMainMenu()
    {
        gameObject.SetActive(false);
        UI_Controller.Instance.MainMenu.ShowPanel();
    }

    // 点击 "Next Level" 按钮
    public void OnClickNextLevel()
    {
        gameObject.SetActive(false);
        UI_Controller.Instance.StartGameplay();
        Debug.Log("UI -> 发送加载下一关的指令");
    }
}