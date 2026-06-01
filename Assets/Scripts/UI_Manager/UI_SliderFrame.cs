using UnityEngine;
using DG.Tweening; // 引入强大的动效库

public class UI_SliderFrame : MonoBehaviour
{
    [Header("UI 引用")]
    [Tooltip("那个能滑来滑去的空心边框")]
    public RectTransform highlightFrame;

    [Tooltip("所有关卡数字按钮的列表 (按1到10的顺序拖进来)")]
    public RectTransform[] levelButtons;

    [Header("动效参数")]
    public float slideDuration = 0.3f; // 滑动耗时
    public Ease slideEase = Ease.OutBack; // 动画曲线：OutBack 会有一点点越过目标再弹回来的高级果冻感！

    private void Start()
    {
        // 游戏刚打开选关界面时，瞬间把框套在第一个关卡（或当前解锁的最高关卡）上
        // 默认先套在第 0 个索引（即关卡 1）上
        MoveFrameTo(0, true);
    }

    // ================= 供外部或按钮点击调用的核心方法 =================

    /// <summary>
    /// 将高亮边框移动到指定索引的按钮上
    /// </summary>
    /// <param name="index">目标按钮在数组中的索引 (0, 1, 2...)</param>
    /// <param name="isInstant">是否瞬间移动 (不播动画)</param>
    public void MoveFrameTo(int index, bool isInstant = false)
    {
        // 防错处理
        if (levelButtons == null || levelButtons.Length == 0) return;
        if (index < 0 || index >= levelButtons.Length) return;

        // 获取目标按钮
        RectTransform targetButton = levelButtons[index];

        // 杀掉边框身上可能正在进行的旧动画，防止狂点按钮时乱飞
        highlightFrame.DOKill();

        if (isInstant)
        {
            // 瞬间移动（用于刚打开界面时的初始化）
            highlightFrame.position = targetButton.position;
            // 可选：如果你的按钮大小不一样，让框的大小也瞬间变过去
            highlightFrame.sizeDelta = targetButton.rect.size;
        }
        else
        {
            // 丝滑移动动画！
            // 移动位置
            highlightFrame.DOMove(targetButton.position, slideDuration).SetEase(slideEase);

            // 可选：如果你有的按钮大有的按钮小，框的大小也能丝滑地跟着变化
            highlightFrame.DOSizeDelta(targetButton.rect.size, slideDuration).SetEase(slideEase);
        }
    }
}