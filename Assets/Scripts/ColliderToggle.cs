using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderToggle : MonoBehaviour
{
    [Header("切换参数")]
    public float activeDuration = 3f;  // 碰撞箱显示（启用）的时长（秒）
    public float inactiveDuration = 2f; // 碰撞箱隐藏（禁用）的时长（秒）

    private Collider[] colliders; // 存储对象上的所有碰撞体
    private bool isColliderActive = true; // 当前碰撞体状态

    void Start()
    {
        // 获取对象上所有碰撞体（适配截图中的Box Collider、Mesh Collider）
        colliders = GetComponents<Collider>();
        // 启动循环切换协程
        StartCoroutine(ToggleColliderCoroutine());
    }

    // 循环切换碰撞体状态的协程
    IEnumerator ToggleColliderCoroutine()
    {
        while (true)
        {
            // 先设置碰撞体为“启用”状态
            SetCollidersActive(true);
            isColliderActive = true;
            Debug.Log($"碰撞箱启用，持续{activeDuration}秒");
            yield return new WaitForSeconds(activeDuration);

            // 再设置碰撞体为“禁用”状态
            SetCollidersActive(false);
            isColliderActive = false;
            Debug.Log($"碰撞箱禁用，持续{inactiveDuration}秒");
            yield return new WaitForSeconds(inactiveDuration);
        }
    }

    // 批量设置所有碰撞体的启用状态
    void SetCollidersActive(bool isActive)
    {
        foreach (Collider collider in colliders)
        {
            collider.enabled = isActive;
        }
    }
}