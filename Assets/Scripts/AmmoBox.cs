using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class AmmoBox : MonoBehaviour
{
    [Header("��������")]
    [Tooltip("��ҩ�����ͣ����䵱ǰ����/��������")]
    public AmmoBoxType ammoBoxType = AmmoBoxType.CurrentWeapon;
    [Tooltip("�Ƿ�Ϊһ���Ե���")]
    public bool isOneTimeUse = true;
    [Tooltip("ʹ����ȴʱ�䣨��һ����ʱ��")]
    public float cooldownTime = 10f;

    [Header("��������")]
    [Tooltip("��ǰ������ϻֱ������")]
    public bool fillCurrentMag = true;
    [Tooltip("����ĺ󱸵�ҩ�����������������ã�")]
    public int rifleReserveAmmo = 60;    // ��ǹ������
    public int fireKirinReserveAmmo = 50;// �����벹����
    public int awmReserveAmmo = 15;      // AWM������
    [Tooltip("��������ͨ�ò�������δƥ��ʱʹ�ã�")]
    public int defaultReserveAmmo = 30;

    [Header("��������")]
    [Tooltip("��������")]
    public float interactDistance = 2f;
    [Tooltip("��������")]
    public KeyCode interactKey = KeyCode.E;
    [Tooltip("��ʾ�ı����磺��E���䵯ҩ��")]
    public string promptText = "��E���䵯ҩ";

    [Header("�Ӿ�/��Ч")]
    public GameObject pickupEffect;      // ʰȡ��Ч
    public AudioClip pickupSound;        // ʰȡ��Ч
    public Material normalMaterial;      // ��������
    public Material cooldownMaterial;    // ��ȴ����

    [Header("�¼�")]
    public UnityEvent OnAmmoCollected;   // ʰȡ��ҩ�¼�

    // ˽�б���
    private Renderer ammoBoxRenderer;
    private Collider ammoBoxCollider;
    private Transform player;
    private bool isInCooldown = false;
    private float cooldownTimer = 0f;
    private WeaponManager playerWeaponManager;
    private WeaponAmmo playerWeaponAmmo;
    // ������������Ƿ���Ҫ��ʾ������ʾ
    private bool showPrompt = false;
    // ���������洢��ʾ�ı�����Ļλ��
    private Vector3 promptScreenPos;
    public bool IsInCooldown => isInCooldown;
    public float InteractDistance => interactDistance;
    // ��ҩ������ö��
    public enum AmmoBoxType
    {
        CurrentWeapon,   // ֻ���䵱ǰ�ֳ�����
        AllWeapons       // ������������
    }

    void Start()
    {
        // ��ȡ���
        ammoBoxRenderer = GetComponent<Renderer>();
        ammoBoxCollider = GetComponent<Collider>();

        // ȷ����ײ��Ϊ������
        ammoBoxCollider.isTrigger = true;

        // ��ʼ������
        if (ammoBoxRenderer != null && normalMaterial != null)
        {
            ammoBoxRenderer.material = normalMaterial;
        }

        // ������ң��ɸ��������Ŀ����������TagΪPlayer��
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        Debug.Log("弹药箱: 查找Player对象 - " + (player != null ? "成功" : "失败"));
    }

    void Update()
    {
        // ��ȴ��ʱ
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
                transform.Rotate(0, 90 * Time.deltaTime, 0); // ������ת
            }
            // ��ȴʱ������ʾ
            showPrompt = false;
            return;
        }

        // �������Ҿ��벢��������
        if (player == null)
        {
            // �������未找到玩家对象，重新查找
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            Debug.Log("弹药箱: 重新查找Player对象 - " + (player != null ? "成功" : "失败"));
        }

        if (player != null && IsPlayerInRange())
        {
            // ��ȡ��ҵ�����������ӳٻ�ȡ�������ʼΪ�գ�
            if (playerWeaponManager == null)
            {
                playerWeaponManager = player.GetComponent<WeaponManager>();
                if (playerWeaponManager == null)
                {
                    // 如果直接在Player对象上找不到，尝试在子对象中查找
                    playerWeaponManager = player.GetComponentInChildren<WeaponManager>();
                }
                Debug.Log("弹药箱: 获取WeaponManager - " + (playerWeaponManager != null ? "成功" : "失败"));
            }

            if (playerWeaponManager != null && playerWeaponAmmo == null)
            {
                playerWeaponAmmo = playerWeaponManager.WeaponAmmoSystem;
                Debug.Log("弹药箱: 获取WeaponAmmo - " + (playerWeaponAmmo != null ? "成功" : "失败"));
            }

            // ���޸ġ�ֻ������ʾ״̬��λ�ã�������GUI
            showPrompt = true;
            promptScreenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up);

            // ��Ұ�������
            if (Input.GetKeyDown(interactKey))
            {
                if (playerWeaponAmmo != null)
                {
                    CollectAmmo();
                }
                else
                {
                    Debug.Log("弹药箱: 无法收集弹药 - playerWeaponAmmo为空");
                }
            }
        }
        else
        {
            // ��Ҳ��ڷ�Χ�ڣ�������ʾ
            showPrompt = false;
        }
    }

    void OnGUI()
    {
#if UNITY_EDITOR
        // ���ڱ༭������ʾ������ʾ
        if (showPrompt)
        {
            GUI.contentColor = Color.white;
            // ������ʾ�ı���λ��΢�������ⳬ����Ļ��
            GUI.Label(new Rect(promptScreenPos.x - 50, Screen.height - promptScreenPos.y - 50, 100, 30), promptText);
        }
#endif
    }

    // �������Ƿ��ڽ�����Χ��
    private bool IsPlayerInRange()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        bool inRange = distance <= interactDistance;
        Debug.Log("弹药箱: 玩家距离 - " + distance + 
                  " 交互距离:" + interactDistance + 
                  " 是否在范围内:" + inRange);
        return inRange;
    }

    // ��ʾ������ʾ��������OnGUIʾ���������滻ΪUGUI��
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
        Debug.Log("弹药箱: 开始收集弹药 - " + 
                  "isInCooldown:" + isInCooldown + 
                  " playerWeaponAmmo:" + (playerWeaponAmmo != null ? "存在" : "不存在") + 
                  " playerWeaponManager:" + (playerWeaponManager != null ? "存在" : "不存在"));

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

        Debug.Log("弹药箱: 已补充弹药");
    }

    // ���䵱ǰ������ҩ
    private void RefillCurrentWeaponAmmo()
    {
        Debug.Log("弹药箱: 开始补充当前武器弹药");
        if (playerWeaponManager.CurrentWeapon == null)
        {
            Debug.Log("弹药箱: 当前武器为空");
            return;
        }

        string currentWeaponName = playerWeaponManager.CurrentWeapon.name;
        Debug.Log("弹药箱: 当前武器名称 - " + currentWeaponName);

        // 1. ������ǰ��ϻ
        if (fillCurrentMag)
        {
            Debug.Log("弹药箱: 开始补充当前弹匣");
            var ammoInfo = playerWeaponManager.GetCurrentWeaponAmmo();
            Debug.Log("弹药箱: 当前弹匣状态 - 当前:" + ammoInfo.current + " 后备:" + ammoInfo.reserve);
            int maxAmmo = playerWeaponAmmo.GetCurrentWeaponMaxAmmo();
            int ammoToAdd = maxAmmo - ammoInfo.current;
            Debug.Log("弹药箱: 弹匣补充量 - 需要:" + ammoToAdd + " 最大:" + maxAmmo);

            if (ammoToAdd > 0)
            {
                playerWeaponAmmo.AddAmmo(currentWeaponName, ammoToAdd, false);
                Debug.Log("弹药箱: 补充当前弹匣 " + ammoToAdd + " 发");
            }
        }

        // 2. ����󱸵�ҩ
        int reserveToAdd = GetReserveAmmoByWeaponName(currentWeaponName);
        Debug.Log("弹药箱: 补充后备弹药 " + reserveToAdd + " 发");
        playerWeaponAmmo.AddAmmo(currentWeaponName, reserveToAdd, true);
        Debug.Log("弹药箱: 当前武器弹药补充完成");
    }

    // ��������������ҩ
    private void RefillAllWeaponsAmmo()
    {
        Debug.Log("弹药箱: 开始补充所有武器弹药");
        // ��ȡ����������ҩ��Ϣ
        var allAmmoInfo = playerWeaponAmmo.GetAllAmmoInfo();
        Debug.Log("弹药箱: 武器数量 - " + allAmmoInfo.Count);

        foreach (var weaponAmmo in allAmmoInfo)
        {
            string weaponName = weaponAmmo.Key;
            Debug.Log("弹药箱: 武器 - " + weaponName + 
                      " 当前:" + weaponAmmo.Value.current + 
                      " 后备:" + weaponAmmo.Value.reserve);
            int reserveToAdd = GetReserveAmmoByWeaponName(weaponName);
            Debug.Log("弹药箱: 为" + weaponName + "补充后备弹药 " + reserveToAdd + " 发");

            // ֻ����󱸵�ҩ����ϻ���ֵ�ǰ״̬��
            playerWeaponAmmo.AddAmmo(weaponName, reserveToAdd, true);
        }
        Debug.Log("弹药箱: 所有武器弹药补充完成");
    }

    // �����������ƻ�ȡ��Ӧ�ĺ󱸵�ҩ������
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

    // ����ʰȡ��������Ч+��Ч��
    private void PlayPickupFeedback()
    {
        // ������Ч
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        }

        // ������Ч
        if (pickupSound != null && player != null)
        {
            AudioSource playerAudio = player.GetComponent<AudioSource>();
            if (playerAudio != null)
            {
                playerAudio.PlayOneShot(pickupSound);
            }
        }
    }

    // ����ʰȡ����߼���һ����/��ȴ��
    private void HandlePostCollection()
    {
        if (isOneTimeUse)
        {
            // һ����ʹ�ã����ٵ�ҩ��
            Destroy(gameObject);
        }
        else
        {
            // ��ȴģʽ��������ȴ״̬
            isInCooldown = true;
            cooldownTimer = cooldownTime;

            if (ammoBoxRenderer != null && cooldownMaterial != null)
            {
                ammoBoxRenderer.material = cooldownMaterial;
            }
        }
    }

    // Gizmos���ƽ�����Χ
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}