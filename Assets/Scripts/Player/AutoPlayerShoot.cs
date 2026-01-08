using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoPlayerShoot : MonoBehaviour
{
    [Header("射击参数")]
    public float damage = 25f;
    public float range = 100f;
    public float fireRate = 0.5f;  // 射击间隔时间（秒）
    public float rayStartOffset = 0.5f;
    public float shootAnimationDuration = 0.25f; // 射击动画时长

    [Header("摄像机设置")]
    public Camera playerCamera;

    [Header("忽略的物体")]
    [Tooltip("指定要忽略的物体（如玩家自身、武器等）")]
    public List<GameObject> ignoredObjects = new List<GameObject>();
    [Tooltip("自动忽略玩家自身")]
    public bool ignoreSelf = true;

    [Header("自动生成效果")]
    public bool autoGenerateEffects = true;

    [Header("弹药系统")]
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private bool useAmmoSystem = true;

    [Header("动画控制")]
    public FPSAnimationController animationController;
    public Animator weaponAnimator;

    // 私有变量
    private float nextFireTime;
    private AudioSource audioSource;
    private GameObject simpleHitEffect;
    private List<int> ignoredInstanceIDs = new List<int>();
    private bool isReloading = false;

    void Start()
    {
        // 1. 初始化摄像机
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }

        // 2. 初始化音频
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 3. 初始化忽略列表
        InitializeIgnoreList();

        // 4. 创建击中效果
        if (autoGenerateEffects)
        {
            CreateSimpleHitEffect();
        }

        // 5. 获取武器管理器
        if (weaponManager == null)
        {
            weaponManager = GetComponent<WeaponManager>();
            if (weaponManager == null)
            {
                weaponManager = GetComponentInParent<WeaponManager>();
            }
        }

        // 6. 初始化动画控制器
        InitializeAnimationController();

        Debug.Log($"射击系统初始化完成！射速: {fireRate:F2}s/发, 动画时长: {shootAnimationDuration:F2}s");
    }

    void InitializeAnimationController()
    {
        if (animationController == null)
        {
            animationController = GetComponent<FPSAnimationController>();
            if (animationController == null)
            {
                animationController = GetComponentInChildren<FPSAnimationController>();
            }
        }

        if (animationController != null)
        {
            // 设置射击系统引用
            animationController.SetShootSystem(this);

            // 初始更新动画速度
            UpdateShootAnimationSpeed();
        }
        else
        {
            Debug.LogWarning("未找到 FPSAnimationController，射击动画可能无法正确同步");
        }
    }

    void InitializeIgnoreList()
    {
        ignoredInstanceIDs.Clear();

        if (ignoreSelf && !ignoredObjects.Contains(gameObject))
        {
            ignoredObjects.Add(gameObject);
        }

        foreach (GameObject obj in ignoredObjects)
        {
            if (obj != null)
            {
                ignoredInstanceIDs.Add(obj.GetInstanceID());
            }
        }
    }

    void Update()
    {
        // 检查是否正在装填
        if (weaponManager != null)
        {
            isReloading = weaponManager.IsReloading();
        }

        // 如果正在装填，不能射击
        if (isReloading) return;

        // 实际射击检测 - 只有这里检测射击输入！
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            // 检查弹药
            if (CanShoot())
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
            else
            {
                // 弹药不足，播放空枪声
                PlayEmptySound();
                nextFireTime = Time.time + 0.5f;
            }
        }
    }

    bool CanShoot()
    {
        if (!useAmmoSystem || weaponManager == null) return true;
        return weaponManager.TryShootCurrentWeapon();
    }

    void Shoot()
    {
        // 播放射击音效
        PlayShootSound();

        // 关键：只在真正射击时触发射击动画
        TriggerShootAnimation();

        // 射击逻辑
        if (playerCamera != null)
        {
            Vector3 rayOrigin = playerCamera.transform.position +
                               playerCamera.transform.forward * rayStartOffset;

            Ray ray = new Ray(rayOrigin, playerCamera.transform.forward);
            RaycastHit[] hits = Physics.RaycastAll(ray, range);

            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

            foreach (RaycastHit hit in hits)
            {
                if (!IsIgnored(hit.collider.gameObject))
                {
                    HandleHit(hit);
                    break;
                }
            }

            Debug.DrawRay(ray.origin, ray.direction * range, Color.red, 0.1f);
        }

        Debug.Log($"射击完成 - 射击间隔: {fireRate:F2}s, 下次射击时间: {nextFireTime:F2}");
    }

    void TriggerShootAnimation()
    {
        if (animationController != null)
        {
            // 触发射击动画
            animationController.TriggerShootAnimation();

            // 设置射击状态
            // 动画控制器现在会根据射击间隔自动管理状态
        }
    }

    void UpdateShootAnimationSpeed()
    {
        if (animationController != null)
        {
            // 计算适当的动画速度
            float animationSpeed;

            if (fireRate < shootAnimationDuration)
            {
                // 快速射击：射击间隔小于动画时长，需要加速动画
                animationSpeed = shootAnimationDuration / fireRate;
                animationSpeed = Mathf.Clamp(animationSpeed, 1.0f, 3.0f);
                Debug.Log($"快速射击模式: 射击间隔({fireRate:F2}s) < 动画时长({shootAnimationDuration:F2}s)");
                Debug.Log($"动画速度设置为: {animationSpeed:F2}x");
            }
            else
            {
                // 慢速射击：正常速度
                animationSpeed = 1.0f;
                Debug.Log($"慢速射击模式: 射击间隔({fireRate:F2}s) >= 动画时长({shootAnimationDuration:F2}s)");
                Debug.Log($"动画速度设置为: {animationSpeed:F2}x (正常速度)");
            }

            animationController.SetShootAnimationSpeed(animationSpeed);
        }
    }

    // 供动画控制器调用的方法
    public float GetFireRate()
    {
        return fireRate;
    }

    public float GetShootAnimationDuration()
    {
        return shootAnimationDuration;
    }

    public void SetFireRate(float newFireRate)
    {
        if (newFireRate > 0)
        {
            fireRate = newFireRate;
            UpdateShootAnimationSpeed();
            Debug.Log($"射速更新为: {fireRate:F2}s/发");
        }
    }

    public void SetShootAnimationDuration(float duration)
    {
        if (duration > 0)
        {
            shootAnimationDuration = duration;
            UpdateShootAnimationSpeed();
            Debug.Log($"射击动画时长更新为: {shootAnimationDuration:F2}s");
        }
    }

    public void SetShootingAnimationState(bool isShooting)
    {
        if (animationController != null)
        {
            animationController.SetShootingState(isShooting);
        }
    }

    bool IsIgnored(GameObject obj)
    {
        if (ignoredObjects.Contains(obj))
            return true;

        int id = obj.GetInstanceID();
        if (ignoredInstanceIDs.Contains(id))
            return true;

        foreach (GameObject ignoredObj in ignoredObjects)
        {
            if (ignoredObj != null && obj.transform.IsChildOf(ignoredObj.transform))
            {
                ignoredInstanceIDs.Add(id);
                return true;
            }
        }

        return false;
    }

    void HandleHit(RaycastHit hit)
    {
        EnemyHealth enemy = hit.transform.GetComponent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        ShowHitEffect(hit.point);
    }

    void PlayShootSound()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    void PlayEmptySound()
    {
        if (audioSource != null)
        {
            audioSource.PlayOneShot(audioSource.clip, 0.3f);
        }
    }

    void CreateSimpleHitEffect()
    {
        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.name = "SimpleHitEffect";

        Renderer renderer = effect.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Standard"));
        renderer.material.color = Color.red;

        Destroy(effect.GetComponent<Collider>());

        effect.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        effect.SetActive(false);
        effect.hideFlags = HideFlags.HideInHierarchy;

        simpleHitEffect = effect;
    }

    void ShowHitEffect(Vector3 position)
    {
        if (simpleHitEffect != null)
        {
            simpleHitEffect.transform.position = position;
            simpleHitEffect.SetActive(true);
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