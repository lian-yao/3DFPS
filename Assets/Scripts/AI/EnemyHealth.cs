using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("基础设置")]
    public float maxHealth = 50f;
    private float currentHealth;
    public bool isDead = false;  // 死亡状态标记

    [Header("视觉效果")]
    public GameObject deathEffect;
    public Material damageMaterial;  // 受伤时显示的材料
    private Material originalMaterial;
    private Renderer enemyRenderer;

    [Header("调试")]
    public bool debugMode = true;

    void Start()
    {
        // 初始化
        currentHealth = maxHealth;
        isDead = false;

        // 获取渲染器
        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
        {
            originalMaterial = enemyRenderer.material;
        }

        Log($"敌人初始化: {gameObject.name}, HP: {currentHealth}");
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
        {
            Log("敌人已死亡，忽略伤害");
            return;
        }

        Log($"{gameObject.name} 受到 {damage} 点伤害");

        currentHealth -= damage;

        // 受伤视觉效果
        StartCoroutine(ShowDamageEffect());

        // 死亡检查
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            Log($"剩余生命: {currentHealth}");
        }
    }

    // 受伤效果（变色）
    System.Collections.IEnumerator ShowDamageEffect()
    {
        if (enemyRenderer != null && damageMaterial != null)
        {
            enemyRenderer.material = damageMaterial;
            yield return new WaitForSeconds(0.1f);
            enemyRenderer.material = originalMaterial;
        }
    }

    void Die()
    {
        Log($"!!! {gameObject.name} 死亡 !!!");
        isDead = true;

        // 1. 禁用碰撞体
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
            Log("禁用碰撞体");
        }

        // 2. 死亡特效
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
            Log("生成死亡特效");
        }

        // 3. 变色表示死亡（可选）
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = Color.gray;
        }

        // 4. 停止移动（如果有Rigidbody）
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // 5. 标记为"Dead"标签
        //gameObject.tag = "Dead";

        // 6. 立即销毁
        Log($"立即销毁 {gameObject.name}");
        Destroy(gameObject);

        // 或者延迟销毁：
        // Destroy(gameObject, 0.5f);
    }

    // 调试日志
    void Log(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[EnemyHealth] {message}");
        }
    }

    // 调试：在Scene视图中显示生命值
    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Vector3 pos = transform.position + Vector3.up * 2f;

            // 根据生命值显示不同颜色
            Gizmos.color = GetHealthColor();
            Gizmos.DrawWireCube(pos, new Vector3(1f, 0.2f, 0.1f));

            // 填充生命条
            float healthPercent = currentHealth / maxHealth;
            Gizmos.color = Color.red;
            Gizmos.DrawCube(pos - new Vector3(0.5f * (1 - healthPercent), 0, 0),
                           new Vector3(healthPercent, 0.15f, 0.05f));

#if UNITY_EDITOR
            // 显示生命值文字
            UnityEditor.Handles.Label(pos + Vector3.up * 0.3f,
                                    $"{currentHealth:F0}/{maxHealth}");
            if (isDead)
            {
                UnityEditor.Handles.Label(pos + Vector3.up * 0.6f, "DEAD",
                                        new GUIStyle()
                                        {
                                            normal = new GUIStyleState()
                                            {
                                                textColor = Color.red
                                            },
                                            fontSize = 14
                                        });
            }
#endif
        }
    }

    Color GetHealthColor()
    {
        if (isDead) return Color.gray;

        float healthPercent = currentHealth / maxHealth;
        if (healthPercent > 0.6f) return Color.green;
        if (healthPercent > 0.3f) return Color.yellow;
        return Color.red;
    }
}
