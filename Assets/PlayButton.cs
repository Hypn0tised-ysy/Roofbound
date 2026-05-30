using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Image hoverBackground;
    [SerializeField] private mainMenuController menuController;

    private const float HoverAlpha = 192f / 255f;

    private void Awake()
    {
        if (hoverBackground == null)
        {
            Transform hoverTransform = transform.Find("HoverBackGround");
            if (hoverTransform != null)
            {
                hoverBackground = hoverTransform.GetComponent<Image>();
            }
        }

        SetHoverAlpha(0f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHoverAlpha(HoverAlpha);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHoverAlpha(0f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (menuController != null)
        {
            menuController.Play();
        }
    }

    private void SetHoverAlpha(float alpha)
    {
        if (hoverBackground == null)
        {
            return;
        }

        Color color = hoverBackground.color;
        color.a = alpha;
        hoverBackground.color = color;
    }
}
