using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthPack : MonoBehaviour
{
    [Header("血包设置")]
    public float healAmount = 25f;          // 恢复的生命值
    public float rotationSpeed = 60f;       // 旋转速度
    public float floatHeight = 0.5f;        // 浮动高度
    public float floatSpeed = 2f;           // 浮动速度

    [Header("音效设置")]
    public AudioClip pickupSound;           // 拾取音效（在Inspector中拖拽音频文件）
    public float soundVolume = 1f;          // 音量大小（0-1）
    public bool use3DSound = true;          // 是否使用3D音效

    [Header("视觉效果")]
    public GameObject pickupEffect;         // 拾取特效预制体（可选）
    public Color glowColor = Color.red;     // 发光颜色

    private Vector3 startPosition;
    private Renderer objectRenderer;
    private Collider objectCollider;
    private AudioSource audioSource;        // 用于播放音效

    void Start()
    {
        startPosition = transform.position;

        // 获取组件
        objectRenderer = GetComponent<Renderer>();
        objectCollider = GetComponent<Collider>();

        // 获取或添加AudioSource组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 设置AudioSource参数
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;

        if (use3DSound)
        {
            audioSource.spatialBlend = 1f;          // 3D音效
            audioSource.minDistance = 1f;           // 最小距离
            audioSource.maxDistance = 10f;          // 最大距离
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }
        else
        {
            audioSource.spatialBlend = 0f;          // 2D音效
        }

        // 如果设置了拾取音效，赋值给AudioSource
        if (pickupSound != null)
        {
            audioSource.clip = pickupSound;
        }

        // 设置发光效果（可选）
        SetupGlowEffect();
    }

    void Update()
    {
        // 旋转效果
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // 上下浮动效果
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 获取玩家的生命值组件
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // 治疗玩家
                playerHealth.Heal(healAmount);
                Debug.Log($"玩家拾取血包，恢复 {healAmount} 点生命值");

                // 播放拾取音效
                PlayPickupSound();

                // 播放拾取特效
                PlayPickupEffect();

                // 隐藏血包物体（等待音效播放完再销毁）
                HideAndStartDestruction();
            }
        }
    }

    /// <summary>
    /// 播放拾取音效
    /// </summary>
    void PlayPickupSound()
    {
        // 方法1：使用AudioSource播放（如果已设置音频片段）
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
        // 方法2：使用AudioSource.PlayOneShot（可以同时播放多个音效）
        else if (pickupSound != null)
        {
            audioSource.PlayOneShot(pickupSound, soundVolume);
        }
        // 方法3：使用静态方法在指定位置播放
        else if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, soundVolume);
        }
        else
        {
            Debug.LogWarning("未设置拾取音效！请在Inspector中拖拽音频文件到pickupSound字段");
        }
    }

    /// <summary>
    /// 播放拾取特效
    /// </summary>
    void PlayPickupEffect()
    {
        if (pickupEffect != null)
        {
            // 在血包位置生成特效
            GameObject effect = Instantiate(pickupEffect, transform.position, Quaternion.identity);

            // 设置特效自动销毁
            ParticleSystem particles = effect.GetComponent<ParticleSystem>();
            if (particles != null)
            {
                Destroy(effect, particles.main.duration + particles.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(effect, 3f); // 如果没有粒子系统，3秒后销毁
            }
        }
    }

    /// <summary>
    /// 隐藏血包并延迟销毁
    /// </summary>
    void HideAndStartDestruction()
    {
        // 隐藏渲染器和碰撞体
        if (objectRenderer != null) objectRenderer.enabled = false;
        if (objectCollider != null) objectCollider.enabled = false;

        // 计算销毁延迟时间（等待音效播放完毕）
        float destroyDelay = 0.5f; // 默认延迟

        if (audioSource != null && audioSource.clip != null)
        {
            // 如果音频正在播放，等待音频播放完毕
            if (audioSource.isPlaying)
            {
                destroyDelay = audioSource.clip.length;
            }
        }
        else if (pickupSound != null)
        {
            // 如果没有AudioSource但设置了音效，使用音效长度
            destroyDelay = pickupSound.length;
        }

        // 延迟销毁
        Destroy(gameObject, destroyDelay);

        // 也可以使用协程方式（如需更复杂逻辑）
        // StartCoroutine(DestroyAfterDelay(destroyDelay));
    }

    // 协程版本的销毁方法（可选）
    System.Collections.IEnumerator DestroyAfterDelay(float delay)
    {
        // 隐藏血包
        if (objectRenderer != null) objectRenderer.enabled = false;
        if (objectCollider != null) objectCollider.enabled = false;

        // 等待指定时间
        yield return new WaitForSeconds(delay);

        // 销毁游戏物体
        Destroy(gameObject);
    }

    /// <summary>
    /// 设置发光效果（可选）
    /// </summary>
    void SetupGlowEffect()
    {
        // 如果有材质，设置自发光
        if (objectRenderer != null && objectRenderer.material != null)
        {
            // 启用自发光
            objectRenderer.material.EnableKeyword("_EMISSION");
            objectRenderer.material.SetColor("_EmissionColor", glowColor);

            // 或者使用简单的颜色变化
            // objectRenderer.material.color = glowColor;
        }
    }

    /// <summary>
    /// 在Scene视图中显示调试信息
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // 显示拾取范围（碰撞体范围）
        if (objectCollider != null)
        {
            Gizmos.color = Color.green;
            if (objectCollider is BoxCollider)
            {
                BoxCollider box = objectCollider as BoxCollider;
                Gizmos.DrawWireCube(transform.position + box.center, box.size);
            }
            else if (objectCollider is SphereCollider)
            {
                SphereCollider sphere = objectCollider as SphereCollider;
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
            }
        }

        // 显示浮动范围
        Gizmos.color = Color.yellow;
        Vector3 floatTop = startPosition + Vector3.up * floatHeight;
        Vector3 floatBottom = startPosition - Vector3.up * floatHeight;
        Gizmos.DrawLine(floatTop, floatBottom);
        Gizmos.DrawSphere(floatTop, 0.1f);
        Gizmos.DrawSphere(floatBottom, 0.1f);
    }

    /// <summary>
    /// 在Game视图中显示UI提示（可选）
    /// </summary>
    void OnGUI()
    {
        if (objectRenderer != null && objectRenderer.enabled)
        {
            // 将血包位置转换为屏幕坐标
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1f);

            // 确保血包在屏幕内
            if (screenPos.z > 0)
            {
                // 创建GUI样式
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.MiddleCenter;
                style.normal.textColor = Color.green;
                style.fontSize = 12;

                // 显示血包信息
                GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y, 100, 20),
                         $"生命 +{healAmount}", style);
            }
        }
    }
}