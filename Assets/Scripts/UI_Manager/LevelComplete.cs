using UnityEngine;
using TMPro;
using DG.Tweening;

public class LevelComplete : MonoBehaviour
{
    [Header("UI 引用")]
    public TextMeshProUGUI finalTimeText;
    [Tooltip("可选；未绑定时会在 finalTimeText 下方追加一行 Best Time。")]
    public TextMeshProUGUI bestTimeText;
    public Transform panelContent;

    private void OnEnable()
    {
        float finalTime = 0f;
        int levelIndex = 0;

        if (UIManager.Instance != null)
        {
            finalTime = UIManager.Instance.GetRunTime();
            levelIndex = UIManager.Instance.SelectedLevelIndex;
        }

        float previousBest = -1f;
        BestTimeService.TryGetBestTime(levelIndex, out previousBest);
        bool hadPreviousBest = previousBest >= 0f;

        float bestTime = BestTimeService.SaveCompletionAndGetBest(levelIndex, finalTime);
        bool isNewRecord = !hadPreviousBest || finalTime <= previousBest + 0.001f;

        if (finalTimeText != null)
        {
            finalTimeText.text = "Your Time: " + Panel_HUD.FormatTime(finalTime);
        }

        string bestLine = "Best Time: " + Panel_HUD.FormatTime(bestTime);
        if (isNewRecord && hadPreviousBest)
        {
            bestLine += "  (New!)";
        }
        else if (isNewRecord)
        {
            bestLine += "  (First Clear!)";
        }

        if (bestTimeText != null)
        {
            bestTimeText.text = bestLine;
        }
        else if (finalTimeText != null)
        {
            finalTimeText.text += "\n" + bestLine;
        }

        if (panelContent != null)
        {
            panelContent.DOKill();
            panelContent.localScale = Vector3.zero;
            panelContent.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    public void OnClickMainMenu()
    {
        gameObject.SetActive(false);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RequestShowMainMenuOnNextSceneLoad();
        }

        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("main");
    }

    public void OnClickNextLevel()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1f;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.PlayNextLevel();
        }

        Debug.Log("UI -> 请求加载下一关");
    }
}
