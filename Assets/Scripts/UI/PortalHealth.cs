using UnityEngine;
using UnityEngine.Events;

public class PortalHealth : MonoBehaviour
{
    [Header("传送门属性")]
    public int maxHealth = 1000; // 传送门最大生命值
    public int currentHealth;
    public UnityEvent onPortalDestroyed; // 传送门被摧毁后触发的事件（如开启传送）

    void Start()
    {
        currentHealth = maxHealth;
        // 可在此处添加生命值UI显示逻辑（可选）
    }

    // 被攻击时扣血
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // 血量为0时触发传送
        if (currentHealth <= 0)
        {
            OnPortalDestroyed();
        }
    }

    // 传送门被摧毁（血量为0）
    void OnPortalDestroyed()
    {
        Debug.Log("传送门已被摧毁，开启传送！");
        onPortalDestroyed.Invoke(); // 触发外部绑定的事件（如加载下一关）
        // 可选：添加传送门爆炸特效/音效
    }
}