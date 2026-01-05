using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    [Header("基础设置")]
    public float maxHealth = 500f;  // 普通怪物500，Boss可自定义
    private float currentHealth;
    public bool isDead = false;

    [Header("血条类型选择")]
    public bool isBoss = false; // 勾选=true：Boss固定屏幕血条；不勾选=false：怪物跟随血条
    public Slider screenHealthSlider; // Boss专用：拖入Canvas_1下的Slider（固定屏幕）
    public Slider worldHealthSlider;  // 怪物专用：拖入跟随怪物的Slider

    [Header("跟随血条设置（仅怪物用）")]
    private Canvas worldHealthCanvas;
    [Tooltip("血条是否始终面向摄像机")]
    public bool lookAtCamera = true;

    [Header("视觉效果")]
    public GameObject deathEffect;
    public Material damageMaterial;
    private Material originalMaterial;
    private Renderer enemyRenderer;

    [Header("调试")]
    public bool debugMode = true;

    // 新增：敌人死亡事件（供GameManager监听）
    public System.Action OnEnemyDead;

    void Start()
    {
        // 初始化生命值
        currentHealth = maxHealth;
        isDead = false;

        // 初始化对应类型的血条
        InitHealthBar();

        // 获取渲染器
        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
        {
            originalMaterial = enemyRenderer.material;
        }

        Log($"{(isBoss ? "Boss" : "敌人")}初始化: {gameObject.name}, HP: {currentHealth}/{maxHealth}");
    }

    void Update()
    {
        // 仅怪物跟随血条需要面向摄像机
        if (!isBoss && lookAtCamera && worldHealthCanvas != null)
        {
            worldHealthCanvas.transform.LookAt(Camera.main.transform);
            worldHealthCanvas.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        }
    }

    // 初始化血条（区分Boss/怪物）
    void InitHealthBar()
    {
        if (isBoss)
        {
            // Boss：固定屏幕血条（Canvas_1）
            if (screenHealthSlider != null)
            {
                screenHealthSlider.maxValue = maxHealth;
                screenHealthSlider.value = currentHealth;
                screenHealthSlider.gameObject.SetActive(true); // 显示Boss血条
            }
            else
            {
                LogWarning("Boss模式下未赋值screenHealthSlider！请拖入Canvas_1下的Slider");
            }
        }
        else
        {
            // 普通怪物：跟随血条
            if (worldHealthSlider != null)
            {
                worldHealthSlider.maxValue = maxHealth;
                worldHealthSlider.value = currentHealth;
                // 获取跟随血条的Canvas
                worldHealthCanvas = worldHealthSlider.GetComponentInParent<Canvas>();
                if (worldHealthCanvas != null)
                {
                    worldHealthCanvas.enabled = true;
                    // 强制设置层级避免遮挡
                    Canvas canvasComp = worldHealthCanvas.GetComponent<Canvas>();
                    canvasComp.sortingLayerName = "UI";
                    canvasComp.sortingOrder = 100;
                }
                // 确保填充部分激活
                if (worldHealthSlider.fillRect != null)
                {
                    worldHealthSlider.fillRect.gameObject.SetActive(true);
                }
            }
            else
            {
                LogWarning("怪物模式下未赋值worldHealthSlider！请拖入跟随怪物的Slider");
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
        {
            Log($"{(isBoss ? "Boss" : "敌人")}已死亡，忽略伤害");
            return;
        }

        // 确保伤害值为正
        damage = Mathf.Abs(damage);
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // 同步对应类型的血条
        UpdateHealthBar();

        // 受伤视觉效果
        StartCoroutine(ShowDamageEffect());

        Log($"{gameObject.name} 受到 {damage} 点伤害，剩余HP: {currentHealth}/{maxHealth}");

        // 死亡检查
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // 同步血条数值（区分Boss/怪物）
    void UpdateHealthBar()
    {
        if (isBoss)
        {
            // Boss：同步屏幕血条（Canvas_1）
            if (screenHealthSlider != null)
            {
                screenHealthSlider.value = currentHealth;
                Log($"Boss血条同步: {currentHealth}/{maxHealth} | 血条值: {screenHealthSlider.value}");
            }
        }
        else
        {
            // 怪物：同步跟随血条
            if (worldHealthSlider != null)
            {
                worldHealthSlider.value = currentHealth;
                Log($"怪物血条同步: {currentHealth}/{maxHealth} | 血条值: {worldHealthSlider.value}");
            }
        }
    }

    IEnumerator ShowDamageEffect()
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
        Log($"!!! {gameObject.name} {(isBoss ? "Boss被击败" : "死亡")} !!!");
        isDead = true;

        // 新增：触发敌人死亡事件（通知GameManager检查是否所有敌人都死亡）
        OnEnemyDead?.Invoke();

        // 隐藏对应血条
        HideHealthBar();

        // 禁用碰撞体
        Collider collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;

        // 死亡特效
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // 死亡变色
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = Color.gray;
        }

        // 停止移动
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // 延迟销毁
        Destroy(gameObject, isBoss ? 1f : 0.5f); // Boss延迟久一点
    }

    // 隐藏血条（区分Boss/怪物）
    void HideHealthBar()
    {
        if (isBoss)
        {
            // 隐藏Boss屏幕血条（Canvas_1下的Slider）
            if (screenHealthSlider != null)
            {
                screenHealthSlider.gameObject.SetActive(false);
            }
        }
        else
        {
            // 隐藏怪物跟随血条
            if (worldHealthCanvas != null) worldHealthCanvas.enabled = false;
            if (worldHealthSlider != null) worldHealthSlider.gameObject.SetActive(false);
        }
    }

    void Log(string message)
    {
        if (debugMode) Debug.Log($"[EnemyHealth] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[EnemyHealth] {message}");
    }

    // Scene视图调试生命条
    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Vector3 pos = transform.position + Vector3.up * (isBoss ? 4f : 3f);
            float healthPercent = currentHealth / maxHealth;

            Gizmos.color = GetHealthColor();
            Gizmos.DrawWireCube(pos, new Vector3(isBoss ? 3f : 2f, 0.3f, 0.1f));

            Gizmos.color = Color.red;
            Gizmos.DrawCube(pos - new Vector3((isBoss ? 1.5f : 1f) * (1 - healthPercent), 0, 0),
                           new Vector3((isBoss ? 3f : 2f) * healthPercent, 0.2f, 0.05f));

#if UNITY_EDITOR
            UnityEditor.Handles.Label(pos + Vector3.up * 0.5f,
                                    $"当前HP: {currentHealth:F0}/{maxHealth} | 比例: {healthPercent:P0}",
                                    new GUIStyle() { fontSize = 12, normal = { textColor = Color.white } });
#endif
        }
    }

    Color GetHealthColor()
    {
        if (isDead) return Color.gray;
        float healthPercent = currentHealth / maxHealth;
        return healthPercent > 0.6f ? Color.green : (healthPercent > 0.3f ? Color.yellow : Color.red);
    }

    // 编辑器修改数值后立即同步
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            InitHealthBar();
        }
    }
}