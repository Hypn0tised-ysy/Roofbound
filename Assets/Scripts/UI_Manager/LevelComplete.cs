using UnityEngine;
using TMPro;
using DG.Tweening;
public class LevelComplete : MonoBehaviour
{
    public TextMeshProUGUI finalTimeText; // 拖拽结算成绩的 Text
    public Transform panelContent;


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

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetMenuPaused(true);
            UIManager.Instance.SetInputLocked(true);
        }
    }

    // 点击 "Main Menu" 按钮 — 返回主菜单场景
    public void OnClickMainMenu()
    {
        gameObject.SetActive(false);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RequestShowMainMenuOnNextSceneLoad();
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("main");
    }

    // 点击 "Next Level" 按钮
    public void OnClickNextLevel()
    {
        gameObject.SetActive(false);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.PlayNextLevel();
        }
        Debug.Log("UI -> 请求加载下一关");
    }
}