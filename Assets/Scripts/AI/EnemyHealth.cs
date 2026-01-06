using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;


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

    [Header("死亡动画设置")]
    public Animator enemyAnimator; // 拖入敌人的Animator组件
    public string deathAnimTrigger = "IsDead"; // 动画触发参数名（和Animator参数一致）
    public float deathAnimDelay = 4f; // 死亡动画播放时长（根据实际动画调整）

    [Header("调试")]
    public bool debugMode = true;


    public event Action OnEnemyDead;

    void Start()
    {
        // 初始化生命值（强制重置isDead，避免误判）
        currentHealth = maxHealth;
        isDead = false; // 显式重置，防止编辑器误设为true

        // 初始化对应类型的血条
        InitHealthBar();

        // 获取渲染器
        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
        {
            originalMaterial = enemyRenderer.material;
            Log($"找到怪物渲染器，原始材质：{originalMaterial.name}");
        }
        else
        {
            LogWarning("未找到怪物的Renderer组件，受伤变色效果失效！");
        }

        // 检查Animator组件是否赋值
        if (enemyAnimator == null)
        {
            enemyAnimator = GetComponent<Animator>();
            if (enemyAnimator == null)
            {
                LogWarning("未找到Animator组件！请给敌人添加Animator并赋值enemyAnimator变量");
            }
            else
            {
                Log($"自动找到Animator组件：{enemyAnimator.name}");
            }
        }

        Log($"{(isBoss ? "Boss" : "敌人")}初始化完成 | 名称：{gameObject.name} | 初始HP：{currentHealth}/{maxHealth} | isDead：{isDead}");
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
                screenHealthSlider.gameObject.SetActive(true);
                Log($"Boss血条初始化完成，最大值：{screenHealthSlider.maxValue}，当前值：{screenHealthSlider.value}");
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
                    Canvas canvasComp = worldHealthCanvas.GetComponent<Canvas>();
                    canvasComp.sortingLayerName = "UI";
                    canvasComp.sortingOrder = 100;
                    Log($"怪物跟随血条初始化完成，Canvas层级：{canvasComp.sortingLayerName}/{canvasComp.sortingOrder}");
                }
                if (worldHealthSlider.fillRect != null)
                {
                    worldHealthSlider.fillRect.gameObject.SetActive(true);
                }
                Log($"怪物血条初始化完成，最大值：{worldHealthSlider.maxValue}，当前值：{worldHealthSlider.value}");
            }
            else
            {
                LogWarning("怪物模式下未赋值worldHealthSlider！请拖入跟随怪物的Slider");
            }
        }
    }

    // 核心扣血方法（增强调试+修复逻辑）
    public void TakeDamage(float damage)
    {
        // 打印调用日志，确认是否被调用
        Log($"收到扣血调用 | 传入伤害值：{damage} | 当前isDead：{isDead} | 当前血量：{currentHealth}");

        // 死亡后忽略伤害
        if (isDead)
        {
            Log($"{gameObject.name} 已死亡，忽略本次伤害（传入伤害：{damage}）");
            return;
        }

        // 确保伤害值为正，且大于0
        damage = Mathf.Abs(damage);
        if (damage <= 0)
        {
            LogWarning($"传入的伤害值无效（{damage}），必须大于0！");
            return;
        }

        // 扣血并限制范围
        float oldHealth = currentHealth;
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // 同步血条
        UpdateHealthBar();

        // 受伤视觉效果
        StartCoroutine(ShowDamageEffect());

        Log($"{gameObject.name} 扣血完成 | 原始血量：{oldHealth} | 扣除：{damage} | 剩余血量：{currentHealth}");

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
            if (screenHealthSlider != null)
            {
                screenHealthSlider.value = currentHealth;
                Log($"Boss血条同步 | 当前血量：{currentHealth} | 血条显示值：{screenHealthSlider.value}");
            }
        }
        else
        {
            if (worldHealthSlider != null)
            {
                worldHealthSlider.value = currentHealth;
                Log($"怪物血条同步 | 当前血量：{currentHealth} | 血条显示值：{worldHealthSlider.value}");
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
            Log("受伤视觉效果播放完成");
        }
        else
        {
            LogWarning("渲染器或受伤材质未赋值，无法播放受伤效果");
        }
    }

    void Die()
    {
        Log($"=== {gameObject.name} 死亡 ===");
        isDead = true;

        // 新增：禁用AI脚本
        SimpleEnemyAI enemyAI = GetComponent<SimpleEnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.enabled = false;
            Log("禁用怪物AI脚本，停止移动逻辑");
        }

        // 隐藏血条
        HideHealthBar();

        // 禁用碰撞体
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
            Log("禁用怪物碰撞体，防止后续交互");
        }
        OnEnemyDead?.Invoke();
        // 死亡特效
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
            Log("生成死亡特效");
        }

        // 播放死亡动画
        PlayDeathAnimation();

        // 停止移动
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            Log("重置怪物刚体速度为0");
        }

        // 修复后的延迟销毁逻辑
        float destroyDelay = isBoss ? (deathAnimDelay + 1f) : deathAnimDelay;
        destroyDelay = Mathf.Max(destroyDelay, 0.1f); // 防止延迟时间为0/负数
        Destroy(gameObject, destroyDelay);
        Log($"将在 {destroyDelay} 秒后销毁怪物");
    }

    void PlayDeathAnimation()
    {
        if (enemyAnimator != null && !string.IsNullOrEmpty(deathAnimTrigger))
        {
            // 给Animator的Bool参数赋值为true（触发死亡动画）
            enemyAnimator.SetBool(deathAnimTrigger, true);
            Log($"触发死亡动画（Bool参数）：{deathAnimTrigger} = true");
        }
        else
        {
            LogWarning("Animator组件未赋值或动画参数名为空，无法播放死亡动画！");
            if (enemyRenderer != null)
            {
                enemyRenderer.material.color = Color.gray;
                Log("死亡备用效果：怪物变灰色");
            }
        }
    }

    void HideHealthBar()
    {
        if (isBoss)
        {
            if (screenHealthSlider != null) screenHealthSlider.gameObject.SetActive(false);
        }
        else
        {
            if (worldHealthCanvas != null) worldHealthCanvas.enabled = false;
            if (worldHealthSlider != null) worldHealthSlider.gameObject.SetActive(false);
        }
        Log("隐藏怪物血条");
    }

    void Log(string message)
    {
        if (debugMode) Debug.Log($"[EnemyHealth] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[EnemyHealth] {message}");
    }

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

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            InitHealthBar();
        }
    }
}