using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoPlayerShoot : MonoBehaviour
{
    [Header("射击参数")]
    public float damage = 25f;
    public float range = 100f;
    public float fireRate = 0.5f;

    [Header("摄像机设置")]
    public Camera playerCamera;  // 改为公开，让你可以手动拖拽赋值

    [Header("自动生成效果")]
    public bool autoGenerateEffects = true;  // 自动创建所有效果

    // 不需要在Inspector中手动设置这些
    private float nextFireTime;
    private AudioSource audioSource;
    private GameObject simpleHitEffect;

    void Start()
    {
        // 1. 如果摄像机未手动赋值，尝试自动获取
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                Debug.Log("使用主摄像机作为射击摄像机");
            }
            else
            {
                Debug.Log("自动找到子对象中的摄像机");
            }
        }
        else
        {
            Debug.Log("使用手动指定的摄像机: " + playerCamera.name);
        }

        // 2. 自动创建AudioSource（如果需要）
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            Debug.Log("自动添加了AudioSource组件");
        }

        // 3. 自动创建简单的击中效果
        if (autoGenerateEffects)
        {
            CreateSimpleHitEffect();
        }

        Debug.Log("射击系统初始化完成！按鼠标左键射击");
    }

    void Update()
    {
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            SimpleShoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void SimpleShoot()
    {
        // 播放简单音效（无文件也能工作）
        PlayShootSound();

        // 射击逻辑
        if (playerCamera != null)
        {
            Ray ray = new Ray(playerCamera.transform.position,
                             playerCamera.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, range))
            {
                // 处理击中逻辑
                HandleHit(hit);
            }

            // 显示射击射线（调试用）
            Debug.DrawRay(ray.origin, ray.direction * 2f, Color.red, 0.1f);
        }
    }

    void HandleHit(RaycastHit hit)
    {
        string hitName = hit.transform.name;
        Debug.Log($"击中: {hitName}");

        // 对敌人造成伤害
        EnemyHealth enemy = hit.transform.GetComponent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Debug.Log($"造成 {damage} 点伤害");
        }

        // 显示击中效果
        ShowHitEffect(hit.point);
    }

    void PlayShootSound()
    {
        // 如果用户提供了音效文件，播放它
        // 否则播放默认的系统音效
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }

    void CreateSimpleHitEffect()
    {
        // 创建一个简单的红色方块作为击中效果
        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Cube);
        effect.name = "SimpleHitEffect";
        effect.GetComponent<Renderer>().material.color = Color.red;
        effect.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        effect.SetActive(false);  // 先隐藏
        effect.hideFlags = HideFlags.HideInHierarchy;

        simpleHitEffect = effect;
    }

    void ShowHitEffect(Vector3 position)
    {
        if (simpleHitEffect != null)
        {
            simpleHitEffect.transform.position = position;
            simpleHitEffect.SetActive(true);

            // 0.1秒后隐藏
            Invoke(nameof(HideHitEffect), 0.1f);
        }
    }

    void HideHitEffect()
    {
        if (simpleHitEffect != null)
        {
            simpleHitEffect.SetActive(false);
        }
    }
}