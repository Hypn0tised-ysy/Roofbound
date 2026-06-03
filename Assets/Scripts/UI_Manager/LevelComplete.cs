using System;
using System.Collections;
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
        StartCoroutine(ApplyVictoryResultsWhenReady());
    }

    private IEnumerator ApplyVictoryResultsWhenReady()
    {
        yield return null;

        UIManager ui = UIManager.Instance ?? FindObjectOfType<UIManager>();

        float finalTime = 0f;
        int levelIndex = 0;

        if (ui != null)
        {
            finalTime = ui.GetVictoryRunTime();
            levelIndex = ui.SelectedLevelIndex;
        }
        else
        {
            Debug.LogWarning("[LevelComplete] 未找到 UIManager，通关时间为 0。请从 main 场景启动。");
        }

        ResolveTextReferences();

        float bestTime = finalTime;
        bool hadPreviousBest = false;
        bool isNewRecord = true;

        try
        {
            float previousBest = -1f;
            BestTimeService.TryGetBestTime(levelIndex, out previousBest);
            hadPreviousBest = previousBest >= 0f;
            bestTime = BestTimeService.SaveCompletionAndGetBest(levelIndex, finalTime);
            isNewRecord = !hadPreviousBest || finalTime <= previousBest + 0.001f;
        }
        catch (Exception e)
        {
            Debug.LogError("[LevelComplete] 数据库不可用，仅显示本次用时。请更换 Plugins 中的 Mono.Data.Sqlite.dll。\n" + e);
            bestTime = finalTime;
        }

        string yourLine = "Your Time: " + Panel_HUD.FormatTime(finalTime);
        string bestLine = "Best Time: " + Panel_HUD.FormatTime(bestTime);
        if (isNewRecord && hadPreviousBest)
        {
            bestLine += "  (New!)";
        }
        else if (isNewRecord)
        {
            bestLine += "  (First Clear!)";
        }

        if (finalTimeText != null)
        {
            finalTimeText.text = yourLine;
            finalTimeText.ForceMeshUpdate();
        }

        if (bestTimeText != null)
        {
            bestTimeText.text = bestLine;
            bestTimeText.ForceMeshUpdate();
        }
        else if (finalTimeText != null)
        {
            finalTimeText.text = yourLine + "\n" + bestLine;
            finalTimeText.ForceMeshUpdate();
        }

        if (panelContent != null)
        {
            panelContent.DOKill();
            panelContent.localScale = Vector3.zero;
            panelContent.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    private void ResolveTextReferences()
    {
        if (finalTimeText == null)
        {
            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].gameObject.name.IndexOf("Final", StringComparison.OrdinalIgnoreCase) >= 0
                    || texts[i].gameObject.name.IndexOf("Your", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    finalTimeText = texts[i];
                    break;
                }
            }

            if (finalTimeText == null && texts.Length > 0)
            {
                finalTimeText = texts[0];
            }
        }

        if (bestTimeText == null)
        {
            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] == finalTimeText)
                {
                    continue;
                }

                if (texts[i].gameObject.name.IndexOf("Best", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    bestTimeText = texts[i];
                    break;
                }
            }
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
