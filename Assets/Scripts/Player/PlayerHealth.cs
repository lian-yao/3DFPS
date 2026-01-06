using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("生命设置")]
    public float maxHealth = 100f;          // 玩家最大生命值
    public float currentHealth;             // 玩家当前生命值
    public Image healthBar;                 // UI血条（Image类型，需设置为填充模式）
    public GameObject deathUI;              // 死亡提示UI（可选，保留原有死亡UI）
    [Tooltip("可选：是否限制生命值不低于0")]
    public bool clampHealth = true;         // 防止生命值为负数

    // 新增：玩家死亡事件（供GameManager监听）
    public System.Action OnPlayerDead;

    void Start()
    {
        // 初始化生命值
        currentHealth = maxHealth;
        // 初始化血条UI
        UpdateHealthUI();
        // 初始隐藏死亡UI
        if (deathUI != null)
        {
            deathUI.SetActive(false);
        }
    }

    /// <summary>
    /// 受到伤害的方法（外部可调用，比如敌人攻击、掉血时）
    /// </summary>
    /// <param name="damage">受到的伤害值</param>
    public void TakeDamage(float damage)
    {
        // 确保伤害值为正数
        damage = Mathf.Abs(damage);
        // 扣除生命值
        currentHealth -= damage;

        // 限制生命值不低于0（可选）
        if (clampHealth)
        {
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }

        // 更新血条UI
        UpdateHealthUI();

        // 生命值为0时触发死亡逻辑
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 恢复生命值的方法（可选，比如加血包）
    /// </summary>
    /// <param name="healAmount">恢复的生命值</param>
    public void Heal(float healAmount)
    {
        // 确保恢复值为正数
        healAmount = Mathf.Abs(healAmount);
        // 增加生命值
        currentHealth += healAmount;
        // 限制生命值不超过最大值
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        // 更新血条UI
        UpdateHealthUI();
    }

    /// <summary>
    /// 更新血条UI显示
    /// </summary>
    void UpdateHealthUI()
    {
        // 防止血条组件未赋值导致空指针错误
        if (healthBar != null)
        {
            // 计算填充比例（0~1）
            float fillRatio = currentHealth / maxHealth;
            // 同步到血条的填充量
            healthBar.fillAmount = fillRatio;
        }
        else
        {
            Debug.LogWarning("血条Image组件未赋值！请在Inspector面板中拖拽血条对象到PlayerHealth脚本的healthBar字段");
        }
    }

    /// <summary>
    /// 玩家死亡逻辑
    /// </summary>
    void Die()
    {
        Debug.Log("玩家死亡！");

        // 新增：触发死亡事件（通知GameManager显示失败界面）
        OnPlayerDead?.Invoke();

        // 保留原有死亡逻辑（可选，若只用GameManager的失败界面，可注释）
        // 暂停游戏（GameManager会处理暂停，这里可注释）
        // Time.timeScale = 0f;

        // 显示原有死亡UI（可选，建议只用GameManager的统一失败界面）
        // if (deathUI != null)
        // {
        //     deathUI.SetActive(true);
        // }
        // else
        // {
        //     Debug.LogWarning("死亡UI未赋值！请在Inspector面板中拖拽死亡UI对象到PlayerHealth脚本的deathUI字段");
        // }

        // 解锁鼠标（GameManager的失败界面需要点击按钮，保留）
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 可选：禁用玩家移动/攻击等组件
        // GetComponent<PlayerMovement>().enabled = false;
        // GetComponent<PlayerShoot>().enabled = false;
    }

    /// <summary>
    /// 重置玩家生命值（比如复活、重新开始游戏时调用）
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
        // 恢复游戏时间
        Time.timeScale = 1f;
        // 隐藏死亡UI
        if (deathUI != null)
        {
            deathUI.SetActive(false);
        }
        // 重新锁定鼠标（可选）
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}