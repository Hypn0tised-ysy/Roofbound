using UnityEngine;
using UnityEngine.EventSystems; // 必须引入，用于检测鼠标事件
using DG.Tweening; // 引入刚刚装的强大 DOTween 插件！

// 这个标签表示：只要我被挂上了，鼠标移入移出事件就能生效
public class UI_ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale;

    private void Start()
    {
        originalScale = transform.localScale; // 记录按钮最初的大小
    }

    // 鼠标移入按钮瞬间触发
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 瞬间放大到 1.1倍，耗时 0.2 秒，带有弹簧效果(OutBack)！
        transform.DOScale(originalScale * 1.1f, 0.2f).SetEase(Ease.OutBack);
    }

    // 鼠标移开按钮瞬间触发
    public void OnPointerExit(PointerEventData eventData)
    {
        // 缩回原来的大小
        transform.DOScale(originalScale, 0.15f).SetEase(Ease.OutQuad);
    }
}