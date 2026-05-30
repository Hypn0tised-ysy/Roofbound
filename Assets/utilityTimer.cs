using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class utilityTimer : MonoBehaviour
{
    [SerializeField] private playerControl targetPlayer;
    [SerializeField] private Slider timerSlider;
    [SerializeField] private float hideSkillCoolingStrip = 2f;

    private float idleTimer;
    private bool hasEverActivated;
    private CanvasGroup canvasGroup;

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private void Awake()
    {
        if (targetPlayer == null)
        {
            targetPlayer = FindObjectOfType<playerControl>();
        }

        if (timerSlider == null)
        {
            timerSlider = GetComponentInChildren<Slider>(true);
        }

        if (timerSlider != null)
        {
            canvasGroup = timerSlider.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = timerSlider.gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private void Update()
    {
        if (targetPlayer == null || timerSlider == null)
        {
            return;
        }

        if (targetPlayer.TryGetUtilityTimerStatus(out float ratio, out bool isActiveOrCooling))
        {
            if (isActiveOrCooling)
            {
                hasEverActivated = true;
                idleTimer = 0f;
                SetVisible(true);

                timerSlider.value = Mathf.Clamp01(ratio);
                return;
            }

            if (!hasEverActivated)
            {
                idleTimer = 0f;
                SetVisible(false);
                return;
            }

            idleTimer += Time.deltaTime;
            if (idleTimer >= hideSkillCoolingStrip)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);

            timerSlider.value = Mathf.Clamp01(ratio);
        }
        else
        {
            idleTimer = 0f;
            hasEverActivated = false;
            SetVisible(false);
        }
    }
}
