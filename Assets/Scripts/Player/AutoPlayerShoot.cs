using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoPlayerShoot : MonoBehaviour
{
    [Header("射击参数")]
    public float damage = 25f;
    public float range = 100f;
    public float fireRate = 0.5f;
    public float rayStartOffset = 0.5f;

    [Header("摄像机设置")]
    public Camera playerCamera;

    [Header("忽略的物体")]
    [Tooltip("指定要忽略的物体（如玩家自身、武器等）")]
    public List<GameObject> ignoredObjects = new List<GameObject>();
    [Tooltip("自动忽略玩家自身")]
    public bool ignoreSelf = true;

    [Header("自动生成效果")]
    public bool autoGenerateEffects = true;

    // 私有变量
    private float nextFireTime;
    private AudioSource audioSource;
    private GameObject simpleHitEffect;
    private List<int> ignoredInstanceIDs = new List<int>(); // 缓存忽略物体的ID

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

        Debug.Log($"射击系统初始化完成！忽略 {ignoredObjects.Count} 个物体");
    }

    void InitializeIgnoreList()
    {
        // 清空缓存
        ignoredInstanceIDs.Clear();

        // 如果启用自动忽略自身，添加玩家自己
        if (ignoreSelf && !ignoredObjects.Contains(gameObject))
        {
            ignoredObjects.Add(gameObject);
        }

        // 缓存所有要忽略物体的InstanceID（性能优化）
        foreach (GameObject obj in ignoredObjects)
        {
            if (obj != null)
            {
                ignoredInstanceIDs.Add(obj.GetInstanceID());
                Debug.Log($"将忽略: {obj.name} (ID: {obj.GetInstanceID()})");
            }
        }
    }

    void Update()
    {
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        // 播放射击音效
        PlayShootSound();

        if (playerCamera != null)
        {
            // 射线起点向前偏移
            Vector3 rayOrigin = playerCamera.transform.position +
                               playerCamera.transform.forward * rayStartOffset;

            Ray ray = new Ray(rayOrigin, playerCamera.transform.forward);
            RaycastHit[] hits = Physics.RaycastAll(ray, range);

            // 按距离排序，找到最近的未忽略的击中点
            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

            foreach (RaycastHit hit in hits)
            {
                // 检查是否在忽略列表中
                if (!IsIgnored(hit.collider.gameObject))
                {
                    // 找到第一个未忽略的目标
                    HandleHit(hit);
                    break;
                }
            }

            // 显示射击射线（调试用）
            Debug.DrawRay(ray.origin, ray.direction * range, Color.red, 0.1f);
        }
    }

    // 检查物体是否在忽略列表中
    bool IsIgnored(GameObject obj)
    {
        // 方法1：直接检查GameObject引用（快速）
        if (ignoredObjects.Contains(obj))
            return true;

        // 方法2：检查InstanceID（更快）
        int id = obj.GetInstanceID();
        if (ignoredInstanceIDs.Contains(id))
            return true;

        // 方法3：检查是否是指定物体的子对象
        foreach (GameObject ignoredObj in ignoredObjects)
        {
            if (ignoredObj != null && obj.transform.IsChildOf(ignoredObj.transform))
            {
                // 添加到缓存以便下次快速检查
                ignoredInstanceIDs.Add(id);
                return true;
            }
        }

        return false;
    }

    // 添加要忽略的物体（可以在运行时动态添加）
    public void AddIgnoredObject(GameObject obj)
    {
        if (obj != null && !ignoredObjects.Contains(obj))
        {
            ignoredObjects.Add(obj);
            ignoredInstanceIDs.Add(obj.GetInstanceID());
            Debug.Log($"添加忽略物体: {obj.name}");
        }
    }

    // 移除忽略的物体
    public void RemoveIgnoredObject(GameObject obj)
    {
        if (ignoredObjects.Remove(obj))
        {
            ignoredInstanceIDs.Remove(obj.GetInstanceID());
            Debug.Log($"移除忽略物体: {obj.name}");
        }
    }

    // 清空所有忽略的物体
    public void ClearIgnoredObjects()
    {
        ignoredObjects.Clear();
        ignoredInstanceIDs.Clear();
        Debug.Log("已清空所有忽略物体");
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
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
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

    // 在编辑器中显示调试信息
    void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            // 绘制射线起点
            Gizmos.color = Color.green;
            Vector3 rayOrigin = playerCamera.transform.position +
                               playerCamera.transform.forward * rayStartOffset;
            Gizmos.DrawWireSphere(rayOrigin, 0.05f);

            // 绘制射线方向
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(rayOrigin, rayOrigin + playerCamera.transform.forward * 2f);

            // 绘制忽略物体的范围
            Gizmos.color = Color.red;
            foreach (GameObject obj in ignoredObjects)
            {
                if (obj != null)
                {
                    Gizmos.DrawWireCube(obj.transform.position,
                                       obj.transform.lossyScale * 1.2f);
                }
            }
        }
    }

    // 在Inspector中显示当前忽略的物体数量
    void OnGUI()
    {
#if UNITY_EDITOR
        if (UnityEditor.Selection.activeGameObject == gameObject)
        {
            GUI.color = Color.yellow;
            GUI.Label(new Rect(10, 10, 300, 20), 
                     $"忽略物体数量: {ignoredObjects.Count}");
        }
#endif
    }
}