using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 敌人健康系统：实现IHealth接口，管理血量、受击、死亡逻辑，包含血条、视觉/动画效果，预留音效扩展
/// </summary>
public class EnemyHealth : MonoBehaviour, IHealth
{
    [Header("基础配置")]
    public float maxHealth = 500f; // 最大血量
    private float currentHealth;   // 当前血量
    public bool IsDead { get; private set; } = false; // 是否死亡（实现IHealth接口）

    [Header("血条显示配置")]
    public bool isBoss = false; // 是否为Boss（区分血条显示方式）
    public Slider screenHealthSlider; // Boss屏幕血条
    public Slider worldHealthSlider;  // 普通敌人世界空间血条
    private Canvas worldHealthCanvas; // 世界血条画布
    public bool lookAtCamera = true;  // 世界血条是否朝向相机

    [Header("视觉效果")]
    public GameObject deathEffectPrefab; // 死亡特效预制体
    public Material damageMaterial;      // 受击变色材质
    private Material originalMaterial;   // 原始材质
    private Renderer enemyRenderer;      // 敌人渲染器

    [Header("动画配置")]
    public Animator enemyAnimator; // 敌人动画器
    public string deathAnimBool = "IsDead"; // 死亡动画参数名
    public float deathAnimDelay = 4f; // 死亡后销毁延迟

    [Header("扩展组件")]
    [SerializeField] private IEnemySound enemySound; // 音效组件（依赖注入）
    public bool debugMode = true; // 调试日志开关

    // 事件（解耦外部逻辑）
    public event Action OnEnemyDamaged;        // 受击事件
    public event Action OnEnemyDeath;          // 死亡事件
    public event Action OnEnemyDead;           // 兼容旧版本死亡事件
    public event Action<float> OnHealthChanged;// 血量变化事件（参数：血量百分比）

    void Awake()
    {
        // 自动获取音效组件
        enemySound ??= GetComponent<IEnemySound>();
    }

    void Start()
    {
        currentHealth = maxHealth;
        IsDead = false;

        InitHealthBar();
        InitRenderer();
        InitAnimator();

        Log($"{(isBoss ? "Boss" : "普通敌人")}初始化 | 名称:{gameObject.name} | 初始HP:{currentHealth}/{maxHealth}");
    }

    void Update()
    {
        // 更新世界血条朝向
        UpdateWorldHealthBarRotation();
    }

    #region 初始化相关方法（单一职责）
    private void InitRenderer()
    {
        // 初始化渲染器，记录原始材质
        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
        {
            originalMaterial = enemyRenderer.material;
            Log($"找到渲染器，原始材质:{originalMaterial.name}");
        }
        else
        {
            LogWarning("未找到Renderer，受击变色失效");
        }
    }

    private void InitAnimator()
    {
        // 初始化动画器
        enemyAnimator ??= GetComponent<Animator>();
        if (enemyAnimator == null)
        {
            LogWarning("未找到Animator，死亡动画失效");
        }
    }

    private void InitHealthBar()
    {
        // 初始化血条（区分Boss/普通敌人）
        if (isBoss) InitBossHealthBar();
        else InitNormalEnemyHealthBar();
    }

    private void InitBossHealthBar()
    {
        // 初始化Boss屏幕血条
        if (screenHealthSlider != null)
        {
            screenHealthSlider.maxValue = maxHealth;
            screenHealthSlider.value = currentHealth;
            screenHealthSlider.gameObject.SetActive(true);
            
            // 确保Boss血条所在的Canvas有正确的缩放设置
            Canvas canvas = screenHealthSlider.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                // 检查并配置Canvas Scaler
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null)
                {
                    scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                }
                
                // 配置Canvas Scaler为自适应屏幕
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                scaler.matchWidthOrHeight = 0.5f;
                
                // 确保Canvas设置正确
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000;
            }
        }
        else
        {
            LogWarning("Boss模式未赋值screenHealthSlider");
        }
    }

    private void InitNormalEnemyHealthBar()
    {
        // 初始化普通敌人世界血条
        if (worldHealthSlider != null)
        {
            worldHealthSlider.maxValue = maxHealth;
            worldHealthSlider.value = currentHealth;

            worldHealthCanvas = worldHealthSlider.GetComponentInParent<Canvas>();
            if (worldHealthCanvas != null)
            {
                worldHealthCanvas.enabled = true;
                var canvas = worldHealthCanvas.GetComponent<Canvas>();
                canvas.sortingLayerName = "UI";
                canvas.sortingOrder = 100;
            }
        }
        else
        {
            LogWarning("普通敌人未赋值worldHealthSlider");
        }
    }
    #endregion

    #region 核心逻辑（受击/死亡）
    public void TakeDamage(float damage)
    {
        // 处理受击逻辑（实现IHealth接口）
        if (IsDead)
        {
            Log($"{gameObject.name}已死亡，忽略伤害:{damage}");
            return;
        }

        damage = Mathf.Abs(damage);
        if (damage <= 0)
        {
            LogWarning($"无效伤害值:{damage}");
            return;
        }

        float oldHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);

        // 触发血量相关事件
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
        OnEnemyDamaged?.Invoke();

        // 播放受击音效（扩展点）
        enemySound?.PlayHurtSound();

        UpdateHealthBar();
        StartCoroutine(ShowDamageVisualEffect());

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        // 处理死亡逻辑
        Log($"=== {gameObject.name} 死亡 ===");
        IsDead = true;

        // 触发死亡事件
        OnEnemyDeath?.Invoke();
        OnEnemyDead?.Invoke(); // 触发兼容的死亡事件

        // 播放死亡音效（扩展点）
        enemySound?.PlayDeathSound();

        // 死亡后处理：禁用组件、隐藏血条、播放特效/动画、延迟销毁
        DisableEnemyComponents();
        HideHealthBar();
        PlayDeathVisualEffect();
        PlayDeathAnimation();
        ScheduleDestroy();
    }

    private void DisableEnemyComponents()
    {
        // 禁用敌人核心组件（AI、碰撞体），停止音效
        var ai = GetComponent<SimpleEnemyAI>();
        if (ai != null) ai.enabled = false;

        var collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;

        enemySound?.StopAllSounds();
    }

    private void PlayDeathVisualEffect()
    {
        // 播放死亡特效
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }
    }

    private void PlayDeathAnimation()
    {
        // 播放死亡动画（无动画时降级为变灰色）
        if (enemyAnimator != null && !string.IsNullOrEmpty(deathAnimBool))
        {
            enemyAnimator.SetBool(deathAnimBool, true);
        }
        else
        {
            enemyRenderer.material.color = Color.gray; // 降级处理
        }
    }

    private void ScheduleDestroy()
    {
        // 设置死亡后延迟销毁
        float destroyDelay = isBoss ? (deathAnimDelay + 1f) : deathAnimDelay;
        destroyDelay = Mathf.Max(destroyDelay, 0.1f);
        Destroy(gameObject, destroyDelay);
    }
    #endregion

    #region 辅助方法
    private void UpdateWorldHealthBarRotation()
    {
        // 更新世界血条朝向相机
        if (!isBoss && lookAtCamera && worldHealthCanvas != null)
        {
            worldHealthCanvas.transform.LookAt(Camera.main.transform);
            worldHealthCanvas.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        }
    }

    private void UpdateHealthBar()
    {
        // 更新血条显示值（添加空引用检查）
        if (isBoss)
        {
            if (screenHealthSlider != null)
            {
                screenHealthSlider.value = currentHealth;
            }
        }
        else
        {
            if (worldHealthSlider != null)
            {
                worldHealthSlider.value = currentHealth;
            }
        }
    }

    private IEnumerator ShowDamageVisualEffect()
    {
        // 播放受击视觉效果（材质切换）
        if (enemyRenderer != null && damageMaterial != null)
        {
            enemyRenderer.material = damageMaterial;
            yield return new WaitForSeconds(0.1f);
            enemyRenderer.material = originalMaterial;
        }
    }

    private void HideHealthBar()
    {
        // 隐藏血条
        if (isBoss)
        {
            if (screenHealthSlider != null)
                screenHealthSlider.gameObject.SetActive(false);
        }
        else
        {
            if (worldHealthCanvas != null)
                worldHealthCanvas.enabled = false;
            if (worldHealthSlider != null)
                worldHealthSlider.gameObject.SetActive(false);
        }
    }
    #endregion

    #region 日志/编辑器方法
    private void Log(string message)
    {
        // 调试日志（带开关）
        if (debugMode) Debug.Log($"[EnemyHealth] {message}");
    }

    private void LogWarning(string message)
    {
        // 调试警告日志
        Debug.LogWarning($"[EnemyHealth] {message}");
    }

    void OnDrawGizmos()
    {
        // 编辑器Gizmos：可视化显示血量
        if (Application.isPlaying)
        {
            Vector3 pos = transform.position + Vector3.up * (isBoss ? 4f : 3f);
            float healthPercent = currentHealth / maxHealth;

            Gizmos.color = IsDead ? Color.gray :
                (healthPercent > 0.6f ? Color.green : (healthPercent > 0.3f ? Color.yellow : Color.red));
            Gizmos.DrawWireCube(pos, new Vector3(isBoss ? 3f : 2f, 0.3f, 0.1f));

            Gizmos.color = Color.red;
            Gizmos.DrawCube(pos - new Vector3((isBoss ? 1.5f : 1f) * (1 - healthPercent), 0, 0),
                           new Vector3((isBoss ? 3f : 2f) * healthPercent, 0.2f, 0.05f));

#if UNITY_EDITOR
            UnityEditor.Handles.Label(pos + Vector3.up * 0.5f,
                                    $"当前HP: {currentHealth:F0}/{maxHealth} | 百分比: {healthPercent:P0}",
                                    new GUIStyle() { fontSize = 12, normal = { textColor = Color.white } });
#endif
        }
    }

    private void OnValidate()
    {
        // 编辑器验证：运行时重新初始化血条
        if (Application.isPlaying) InitHealthBar();
    }
    #endregion
}