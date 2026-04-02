using System.Collections.Generic;
using UnityEngine;

// Transform 扩展方法集合。
//
// 设计目的：
// 1) 以更直观的调用方式复用常见层级清理逻辑（transform.DestroyImmediateChildren()）。
// 2) 将编辑器/工具脚本中频繁出现的“删除子物体”代码集中管理，减少重复。
//
// 重要说明：
// - 这里使用的是 Object.DestroyImmediate，而不是 Destroy。
// - DestroyImmediate 会立即销毁对象，通常更常见于编辑器流程或需要立刻重建层级的场景。
// - 在运行时频繁调用可能带来不可预期副作用，使用前应确认调用时机。
public static class TransformExtensions
{
    /// <summary>
    /// 立即销毁目标 Transform 的全部子物体（不包含自身）。
    /// </summary>
    /// <param name="transform">被清理的父节点。</param>
    /// <remarks>
    /// 倒序遍历是关键：
    /// 正序删除会导致 child 索引前移，容易跳过元素或访问越界；
    /// 倒序删除可保证每次删除都不影响尚未访问的索引区间。
    /// </remarks>
    public static void DestroyImmediateChildren(this Transform transform)
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// 立即销毁目标 Transform 的子物体，但保留 exceptions 列表中的对象。
    /// </summary>
    /// <param name="transform">被清理的父节点。</param>
    /// <param name="exceptions">需要跳过销毁的子物体列表。</param>
    /// <remarks>
    /// 行为细节：
    /// 1) 仅通过“引用相等”判断是否属于保留对象。
    /// 2) 如果 exceptions 为空列表，将等价于销毁全部子物体。
    /// 3) 当前实现未对 exceptions 为 null 做保护，调用方应确保传入非 null。
    /// </remarks>
    public static void DestroyImmediateChildren(this Transform transform, List<GameObject> exceptions)
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            bool skip = false;
            for (int j = 0; j < exceptions.Count; j++)
            {
                if (exceptions[j] == transform.GetChild(i).gameObject)
                {
                    skip = true;
                    break;
                }
            }
            if (skip)
            {
                continue;
            }
            Object.DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
}