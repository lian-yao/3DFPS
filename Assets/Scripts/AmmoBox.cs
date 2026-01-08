using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class AmmoBox : MonoBehaviour
{
    [Header("基础设置")]
    [Tooltip("弹药箱类型：补充当前武器/所有武器")]
    public AmmoBoxType ammoBoxType = AmmoBoxType.CurrentWeapon;
    [Tooltip("是否为一次性道具")]
    public bool isOneTimeUse = true;
    [Tooltip("使用冷却时间（非一次性时）")]
    public float cooldownTime = 10f;

    [Header("补给配置")]
    [Tooltip("当前武器弹匣直接填满")]
    public bool fillCurrentMag = true;
    [Tooltip("补充的后备弹药量（按武器类型配置）")]
    public int rifleReserveAmmo = 60;    // 步枪补充量
    public int fireKirinReserveAmmo = 50;// 火麒麟补充量
    public int awmReserveAmmo = 15;      // AWM补充量
    [Tooltip("所有武器通用补充量（未匹配时使用）")]
    public int defaultReserveAmmo = 30;

    [Header("交互设置")]
    [Tooltip("交互距离")]
    public float interactDistance = 2f;
    [Tooltip("交互按键")]
    public KeyCode interactKey = KeyCode.E;
    [Tooltip("提示文本（如：按E补充弹药）")]
    public string promptText = "按E补充弹药";

    [Header("视觉/音效")]
    public GameObject pickupEffect;      // 拾取特效
    public AudioClip pickupSound;        // 拾取音效
    public Material normalMaterial;      // 正常材质
    public Material cooldownMaterial;    // 冷却材质

    [Header("事件")]
    public UnityEvent OnAmmoCollected;   // 拾取弹药事件

    // 私有变量
    private Renderer ammoBoxRenderer;
    private Collider ammoBoxCollider;
    private Transform player;
    private bool isInCooldown = false;
    private float cooldownTimer = 0f;
    private WeaponManager playerWeaponManager;
    private WeaponAmmo playerWeaponAmmo;
    // 【新增】标记是否需要显示交互提示
    private bool showPrompt = false;
    // 【新增】存储提示文本的屏幕位置
    private Vector3 promptScreenPos;
    public bool IsInCooldown => isInCooldown;
    public float InteractDistance => interactDistance;
    // 弹药箱类型枚举
    public enum AmmoBoxType
    {
        CurrentWeapon,   // 只补充当前手持武器
        AllWeapons       // 补充所有武器
    }

    void Start()
    {
        // 获取组件
        ammoBoxRenderer = GetComponent<Renderer>();
        ammoBoxCollider = GetComponent<Collider>();

        // 确保碰撞体为触发器
        ammoBoxCollider.isTrigger = true;

        // 初始化材质
        if (ammoBoxRenderer != null && normalMaterial != null)
        {
            ammoBoxRenderer.material = normalMaterial;
        }

        // 查找玩家（可根据你的项目调整，比如Tag为Player）
        player = GameObject.FindGameObjectWithTag("Gun")?.transform;
    }

    void Update()
    {
        // 冷却计时
        if (isInCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isInCooldown = false;
                if (ammoBoxRenderer != null && normalMaterial != null)
                {
                    ammoBoxRenderer.material = normalMaterial;
                }
            }
            if (!isInCooldown)
            {
                transform.Rotate(0, 90 * Time.deltaTime, 0); // 缓慢旋转
            }
            // 冷却时隐藏提示
            showPrompt = false;
            return;
        }

        // 检测玩家距离并处理交互
        if (player != null && IsPlayerInRange())
        {
            // 获取玩家的武器组件（延迟获取，避免初始为空）
            if (playerWeaponManager == null)
            {
                playerWeaponManager = player.GetComponent<WeaponManager>();
                if (playerWeaponManager != null)
                {
                    playerWeaponAmmo = playerWeaponManager.GetComponent<WeaponAmmo>();
                }
            }

            // 【修改】只更新提示状态和位置，不绘制GUI
            showPrompt = true;
            promptScreenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up);

            // 玩家按键交互
            if (Input.GetKeyDown(interactKey) && playerWeaponAmmo != null)
            {
                CollectAmmo();
            }
        }
        else
        {
            // 玩家不在范围内，隐藏提示
            showPrompt = false;
        }
    }

    void OnGUI()
    {
#if UNITY_EDITOR
        // 仅在编辑器中显示调试提示
        if (showPrompt)
        {
            GUI.contentColor = Color.white;
            // 绘制提示文本（位置微调，避免超出屏幕）
            GUI.Label(new Rect(promptScreenPos.x - 50, Screen.height - promptScreenPos.y - 50, 100, 30), promptText);
        }
#endif
    }

    // 检测玩家是否在交互范围内
    private bool IsPlayerInRange()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        return distance <= interactDistance;
    }

    // 显示交互提示（这里用OnGUI示例，建议替换为UGUI）
//    private void ShowInteractPrompt()
//    {
//#if UNITY_EDITOR
//        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up);
//        GUI.contentColor = Color.white;
//        GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y - 50, 100, 30), promptText);
//#endif
//    }

    // 收集弹药核心逻辑
    private void CollectAmmo()
    {
        if (playerWeaponAmmo == null || isInCooldown) return;

        // 播放拾取特效和音效
        PlayPickupFeedback();

        // 根据类型补充弹药
        if (ammoBoxType == AmmoBoxType.CurrentWeapon)
        {
            RefillCurrentWeaponAmmo();
        }
        else
        {
            RefillAllWeaponsAmmo();
        }

        // 触发事件
        OnAmmoCollected?.Invoke();

        // 处理使用后逻辑
        HandlePostCollection();

        Debug.Log("弹药箱：已补充弹药！");
    }

    // 补充当前武器弹药
    private void RefillCurrentWeaponAmmo()
    {
        if (playerWeaponManager.CurrentWeapon == null) return;

        string currentWeaponName = playerWeaponManager.CurrentWeapon.name;

        // 1. 填满当前弹匣
        if (fillCurrentMag)
        {
            var ammoInfo = playerWeaponManager.GetCurrentWeaponAmmo();
            int maxAmmo = playerWeaponAmmo.GetCurrentWeaponMaxAmmo();
            int ammoToAdd = maxAmmo - ammoInfo.current;

            if (ammoToAdd > 0)
            {
                playerWeaponAmmo.AddAmmo(currentWeaponName, ammoToAdd, false);
            }
        }

        // 2. 补充后备弹药
        int reserveToAdd = GetReserveAmmoByWeaponName(currentWeaponName);
        playerWeaponAmmo.AddAmmo(currentWeaponName, reserveToAdd, true);
    }

    // 补充所有武器弹药
    private void RefillAllWeaponsAmmo()
    {
        // 获取所有武器弹药信息
        var allAmmoInfo = playerWeaponAmmo.GetAllAmmoInfo();

        foreach (var weaponAmmo in allAmmoInfo)
        {
            string weaponName = weaponAmmo.Key;
            int reserveToAdd = GetReserveAmmoByWeaponName(weaponName);

            // 只补充后备弹药（弹匣保持当前状态）
            playerWeaponAmmo.AddAmmo(weaponName, reserveToAdd, true);
        }
    }

    // 根据武器名称获取对应的后备弹药补充量
    private int GetReserveAmmoByWeaponName(string weaponName)
    {
        if (weaponName.Contains("Rifle"))
        {
            return rifleReserveAmmo;
        }
        else if (weaponName.Contains("FireKirin"))
        {
            return fireKirinReserveAmmo;
        }
        else if (weaponName.Contains("AWM"))
        {
            return awmReserveAmmo;
        }
        else
        {
            return defaultReserveAmmo;
        }
    }

    // 播放拾取反馈（特效+音效）
    private void PlayPickupFeedback()
    {
        // 播放特效
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        }

        // 播放音效
        if (pickupSound != null && player != null)
        {
            AudioSource playerAudio = player.GetComponent<AudioSource>();
            if (playerAudio != null)
            {
                playerAudio.PlayOneShot(pickupSound);
            }
        }
    }

    // 处理拾取后的逻辑（一次性/冷却）
    private void HandlePostCollection()
    {
        if (isOneTimeUse)
        {
            // 一次性使用：销毁弹药箱
            Destroy(gameObject);
        }
        else
        {
            // 冷却模式：进入冷却状态
            isInCooldown = true;
            cooldownTimer = cooldownTime;

            if (ammoBoxRenderer != null && cooldownMaterial != null)
            {
                ammoBoxRenderer.material = cooldownMaterial;
            }
        }
    }

    // Gizmos绘制交互范围
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}