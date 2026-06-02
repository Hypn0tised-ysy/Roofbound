using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // 用于监听鼠标移入移出
using DG.Tweening; // 引入 DOTween 动画库

// 这个标签强制要求挂载这个脚本的物体，必须带有一个 Image 组件
[RequireComponent(typeof(Image))]
public class UI_ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("悬停背景色配置")]
    // 默认提供一个半透明的黑色，你可以在外部统一改它
    public Color hoverColor = new Color(1, 1, 1, 0.6f);
    public float fadeDuration = 0.15f; // 渐变耗时

    private Image buttonBackground;
    private Color transparentColor;

    [Header("可选：文字联动微缩放")]
    public Transform textTransform; // (可选) 把按钮下的文字物体拖进来

    private void Awake()
    {
        buttonBackground = GetComponent<Image>();

        // 记录一张完全透明的颜色 (RGB不变，A设为0)
        transparentColor = new Color(hoverColor.r, hoverColor.g, hoverColor.b, 0f);

        // 游戏启动时，强制背景变为透明
        buttonBackground.color = transparentColor;
    }

    // 1. 当鼠标指针【移入】按钮区域时触发
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 杀掉正在进行的旧动画，防止鬼畜闪烁
        buttonBackground.DOKill();

        // 背景颜色平滑过渡到悬停色
        buttonBackground.DOColor(hoverColor, fadeDuration);

        // (可选) 如果你绑定了文字，让文字轻微放大，增加打击感
        if (textTransform != null)
        {
            textTransform.DOKill();
            textTransform.DOScale(Vector3.one * 1.1f, fadeDuration).SetEase(Ease.OutBack);
        }
    }

    // 2. 当鼠标指针【移出】按钮区域时触发
    public void OnPointerExit(PointerEventData eventData)
    {
        buttonBackground.DOKill();

        // 背景颜色平滑过渡回透明
        buttonBackground.DOColor(transparentColor, fadeDuration);

        if (textTransform != null)
        {
            textTransform.DOKill();
            textTransform.DOScale(Vector3.one, fadeDuration);
        }
    }

    // 3. 当鼠标指针【点击】瞬间触发 (可选：增加点击闪烁反馈)
    public void OnPointerClick(PointerEventData eventData)
    {
        buttonBackground.DOKill();

        // 瞬间变成纯白，然后用 0.1 秒恢复到悬停颜色，模拟闪光灯效果
        buttonBackground.color = Color.white;
        buttonBackground.DOColor(hoverColor, 0.1f);
    }

    // 当物体被隐藏(如切面板)时，重置状态，防止重新打开时卡在悬停色
    private void OnDisable()
    {
        if (buttonBackground != null)
        {
            buttonBackground.color = transparentColor;
        }
        if (textTransform != null)
        {
            textTransform.localScale = Vector3.one;
        }
    }
}