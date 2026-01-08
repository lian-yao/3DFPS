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

    [Header("枪声音效")]
    public AudioClip shootSound;           // 射击音效
    [Range(0f, 1f)] public float volume = 0.8f; // 音量控制

    [Header("枪口火焰")]
    public GameObject muzzleFlashPrefab;   // 枪口火焰预制体（可拖入）
    public Material muzzleFlashMaterial;   // 枪火材质（可拖入）
    public Transform muzzleFlashPosition;  // 枪口位置
    public float muzzleFlashDuration = 0.1f; // 枪口火焰显示时长
    public Vector3 muzzleFlashScale = new Vector3(0.5f, 0.5f, 0.5f); // 枪口火焰大小
    [Range(0f, 1f)] public float muzzleFlashIntensity = 0.8f; // 枪口火焰强度

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
    private AudioSource audioSource;       // 音效组件
    private GameObject simpleHitEffect;
    private List<int> ignoredInstanceIDs = new List<int>();
    private GameObject currentMuzzleFlash; // 当前枪口火焰实例

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

        // 2. 初始化音频组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.7f; // 半3D音效
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

        // 7. 验证枪口火焰位置
        if (muzzleFlashPosition == null)
        {
            Debug.LogWarning("未设置枪口火焰位置，将使用主摄像机位置");
            muzzleFlashPosition = playerCamera.transform;
        }

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
        // 检查是否正在装填（从WeaponManager获取状态）
        bool isReloading = weaponManager != null && weaponManager.IsReloading();

        // 如果正在装填，不能射击
        if (isReloading) return;

        // 实际射击检测
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

    // 播放射击音效
    void PlayShootSound()
    {
        if (shootSound == null || audioSource == null) return;

        audioSource.PlayOneShot(shootSound, volume);
        Debug.Log("播放射击音效");
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

        // 显示枪口火焰
        ShowMuzzleFlash();

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

    // 显示枪口火焰
    void ShowMuzzleFlash()
    {
        // 如果有预制体，使用预制体
        if (muzzleFlashPrefab != null)
        {
            // 销毁现有的枪口火焰
            if (currentMuzzleFlash != null)
            {
                Destroy(currentMuzzleFlash);
            }

            // 创建新的枪口火焰
            currentMuzzleFlash = Instantiate(muzzleFlashPrefab, muzzleFlashPosition);
            currentMuzzleFlash.transform.localPosition = Vector3.zero;
            currentMuzzleFlash.transform.localRotation = Quaternion.identity;
            currentMuzzleFlash.transform.localScale = muzzleFlashScale;

            // 设置自动销毁
            Destroy(currentMuzzleFlash, muzzleFlashDuration);
        }
        // 如果没有预制体但有材质，创建基本枪口火焰
        else if (muzzleFlashMaterial != null)
        {
            CreateBasicMuzzleFlash();
        }
    }

    // 创建基本枪口火焰（使用提供的材质）
    void CreateBasicMuzzleFlash()
    {
        // 销毁现有的枪口火焰
        if (currentMuzzleFlash != null)
        {
            Destroy(currentMuzzleFlash);
        }

        // 创建四边形作为枪口火焰
        currentMuzzleFlash = GameObject.CreatePrimitive(PrimitiveType.Quad);
        currentMuzzleFlash.name = "MuzzleFlash";

        // 移除碰撞体
        Destroy(currentMuzzleFlash.GetComponent<Collider>());

        // 设置位置和旋转
        currentMuzzleFlash.transform.SetParent(muzzleFlashPosition);
        currentMuzzleFlash.transform.localPosition = Vector3.zero;
        currentMuzzleFlash.transform.localRotation = Quaternion.Euler(0, 180, 0); // 面向摄像机
        currentMuzzleFlash.transform.localScale = muzzleFlashScale;

        // 设置材质
        Renderer renderer = currentMuzzleFlash.GetComponent<Renderer>();
        renderer.material = muzzleFlashMaterial;

        // 设置透明度
        Color materialColor = renderer.material.color;
        materialColor.a = muzzleFlashIntensity;
        renderer.material.color = materialColor;

        // 添加发光效果
        if (renderer.material.HasProperty("_EmissionColor"))
        {
            Color emissionColor = Color.yellow * muzzleFlashIntensity;
            renderer.material.SetColor("_EmissionColor", emissionColor);
        }

        // 添加淡出效果
        StartCoroutine(FadeOutMuzzleFlash());

        // 设置自动销毁
        Destroy(currentMuzzleFlash, muzzleFlashDuration + 0.5f);
    }

    // 枪口火焰淡出效果
    IEnumerator FadeOutMuzzleFlash()
    {
        if (currentMuzzleFlash == null) yield break;

        Renderer renderer = currentMuzzleFlash.GetComponent<Renderer>();
        if (renderer == null) yield break;

        Material material = renderer.material;
        Color originalColor = material.color;
        float elapsedTime = 0f;

        while (elapsedTime < muzzleFlashDuration)
        {
            if (currentMuzzleFlash == null) yield break;

            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(muzzleFlashIntensity, 0f, elapsedTime / muzzleFlashDuration);

            Color newColor = originalColor;
            newColor.a = alpha;
            material.color = newColor;

            // 同时淡出发光效果
            if (material.HasProperty("_EmissionColor"))
            {
                Color emissionColor = Color.yellow * alpha;
                material.SetColor("_EmissionColor", emissionColor);
            }

            yield return null;
        }
    }

    void TriggerShootAnimation()
    {
        if (animationController != null)
        {
            // 触发射击动画
            animationController.TriggerShootAnimation();
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

    void OnGUI()
    {
        // 显示射击状态信息
        GUILayout.BeginArea(new Rect(10, 50, 300, 200));
        GUILayout.Label($"射击系统状态:");
        GUILayout.Label($"武器管理器: {weaponManager != null}");
        if (weaponManager != null)
        {
            GUILayout.Label($"正在换弹: {weaponManager.IsReloading()}");
            var ammoInfo = weaponManager.GetCurrentWeaponAmmo();
            GUILayout.Label($"弹药: {ammoInfo.current} / {ammoInfo.reserve}");
        }
        GUILayout.Label($"枪口火焰: {(muzzleFlashPrefab != null || muzzleFlashMaterial != null ? "已启用" : "未设置")}");
        GUILayout.EndArea();
    }

    // 清理枪口火焰
    void OnDestroy()
    {
        if (currentMuzzleFlash != null)
        {
            Destroy(currentMuzzleFlash);
        }
    }
}