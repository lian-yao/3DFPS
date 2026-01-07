using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class AmmoEvent : UnityEvent<int, int> { }

public class WeaponAmmo : MonoBehaviour
{
    [System.Serializable]
    public class AmmoInfo
    {
        public string weaponName;
        public int currentAmmo;
        public int maxAmmo;
        public int reserveAmmo;
        public int maxReserveAmmo;
        public float reloadTime = 2.0f;
        public AudioClip reloadSound;
        public bool needReload => currentAmmo <= 0;

        public AmmoInfo(string name, int maxClip, int maxReserve)
        {
            weaponName = name;
            maxAmmo = maxClip;
            maxReserveAmmo = maxReserve;
            currentAmmo = maxClip;
            reserveAmmo = maxReserve;
        }
    }

    [Header("武器弹药设置")]
    [SerializeField] private List<AmmoInfo> weaponAmmoList = new List<AmmoInfo>();

    [Header("默认弹药配置")]
    [SerializeField] private int defaultMaxAmmo = 5;
    [SerializeField] private int defaultMaxReserve = 10;
    [SerializeField] private float defaultReloadTime = 2.0f;

    [Header("事件")]
    public AmmoEvent OnAmmoChanged;
    public AmmoEvent OnReloadStarted;
    public AmmoEvent OnReloadCompleted;

    [Header("音效")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip emptySound;
    [SerializeField] private AudioClip magazineInsertSound;

    // 私有变量
    private Dictionary<string, AmmoInfo> ammoDictionary = new Dictionary<string, AmmoInfo>();
    private bool isReloading = false;
    private string currentWeaponName = "";

    void Awake()
    {
        // 初始化音频源
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // 将列表转换为字典以便快速查找
        InitializeAmmoDictionary();
    }

    void InitializeAmmoDictionary()
    {
        foreach (var ammoInfo in weaponAmmoList)
        {
            if (!ammoDictionary.ContainsKey(ammoInfo.weaponName))
            {
                ammoDictionary.Add(ammoInfo.weaponName, ammoInfo);
            }
        }
    }

    // 注册武器弹药信息
    public void RegisterWeapon(string weaponName)
    {
        RegisterWeapon(weaponName, defaultMaxAmmo, defaultMaxReserve);
    }

    public void RegisterWeapon(string weaponName, int maxAmmo, int maxReserve)
    {
        if (!ammoDictionary.ContainsKey(weaponName))
        {
            AmmoInfo newAmmo = new AmmoInfo(weaponName, maxAmmo, maxReserve);
            newAmmo.reloadTime = defaultReloadTime;
            ammoDictionary.Add(weaponName, newAmmo);
            weaponAmmoList.Add(newAmmo);

            Debug.Log($"注册武器弹药: {weaponName} - 弹匣:{maxAmmo}/后备:{maxReserve}");
        }
    }

    // 设置当前武器
    public void SetCurrentWeapon(string weaponName)
    {
        currentWeaponName = weaponName;

        if (isReloading)
        {
            StopAllCoroutines();
            isReloading = false;
        }
    }

    // 消耗弹药
    public bool ConsumeAmmo(string weaponName, int amount = 1)
    {
        if (ammoDictionary.TryGetValue(weaponName, out AmmoInfo ammo))
        {
            if (ammo.currentAmmo >= amount)
            {
                ammo.currentAmmo -= amount;
                OnAmmoChanged?.Invoke(ammo.currentAmmo, ammo.reserveAmmo);
                return true;
            }
            else
            {
                // 弹药不足，播放空枪声
                if (emptySound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(emptySound);
                }
                return false;
            }
        }
        return false;
    }

    // 检查是否需要装填
    public bool NeedReload(string weaponName)
    {
        if (ammoDictionary.TryGetValue(weaponName, out AmmoInfo ammo))
        {
            // 子弹为0时总是需要装填
            if (ammo.currentAmmo <= 0)
                return true;

            // 只有当弹匣不满且有后备弹药时才返回true
            if (ammo.currentAmmo < ammo.maxAmmo && ammo.reserveAmmo > 0)
            {
                // 可以添加额外条件，比如子弹少于30%时
                // return ammo.currentAmmo < ammo.maxAmmo * 0.3f;
                return true; // 或者直接返回true允许任何不满弹匣的装填
            }

            return false;
        }
        return false;
    }

    // 开始装填
    public void StartReload(string weaponName)
    {
        if (isReloading) return;

        if (ammoDictionary.TryGetValue(weaponName, out AmmoInfo ammo))
        {
            // 新增：检查弹匣是否已满
            if (ammo.currentAmmo >= ammo.maxAmmo)
            {
                Debug.Log($"{weaponName} 弹匣已满，无需装填");
                return;
            }

            // 检查是否有后备弹药
            if (ammo.reserveAmmo <= 0)
            {
                Debug.Log($"{weaponName} 无后备弹药");
                if (emptySound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(emptySound);
                }
                return;
            }

            // 检查是否需要装填（新增逻辑）
            // 只有当需要装填时才能开始装填
            if (!NeedReload(weaponName))
            {
                Debug.Log($"{weaponName} 不需要装填（弹药充足）");
                return;
            }

            // 如果是子弹为0的情况，立即开始装填
            if (ammo.currentAmmo <= 0)
            {
                StartCoroutine(ReloadCoroutine(ammo));
            }
            else
            {
                // 其他情况正常处理
                StartCoroutine(ReloadCoroutine(ammo));
            }
        }
    }

    IEnumerator ReloadCoroutine(AmmoInfo ammo)
    {
        isReloading = true;

        Debug.Log($"{ammo.weaponName} 开始装填...");
        OnReloadStarted?.Invoke(ammo.currentAmmo, ammo.reserveAmmo);

        // 播放装填音效
        if (ammo.reloadSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(ammo.reloadSound);
        }
        else if (audioSource != null && audioSource.clip != null)
        {
            audioSource.PlayOneShot(audioSource.clip);
        }

        // 等待装填时间
        yield return new WaitForSeconds(ammo.reloadTime);

        // 计算需要装填的弹药量
        int ammoNeeded = ammo.maxAmmo - ammo.currentAmmo;
        int ammoToLoad = Mathf.Min(ammoNeeded, ammo.reserveAmmo);

        // 更新弹药
        ammo.currentAmmo += ammoToLoad;
        ammo.reserveAmmo -= ammoToLoad;

        // 播放弹匣插入音效
        if (magazineInsertSound != null && audioSource != null && ammoToLoad > 0)
        {
            audioSource.PlayOneShot(magazineInsertSound);
        }

        Debug.Log($"{ammo.weaponName} 装填完成! +{ammoToLoad} 发 (剩余:{ammo.reserveAmmo})");
        OnReloadCompleted?.Invoke(ammo.currentAmmo, ammo.reserveAmmo);
        OnAmmoChanged?.Invoke(ammo.currentAmmo, ammo.reserveAmmo);

        isReloading = false;
    }

    // 取消装填
    public void CancelReload()
    {
        if (isReloading)
        {
            StopAllCoroutines();
            isReloading = false;
            Debug.Log("装填取消");
        }
    }

    // 添加弹药
    public void AddAmmo(string weaponName, int amount, bool toReserve = true)
    {
        if (ammoDictionary.TryGetValue(weaponName, out AmmoInfo ammo))
        {
            if (toReserve)
            {
                ammo.reserveAmmo = Mathf.Min(ammo.reserveAmmo + amount, ammo.maxReserveAmmo);
                Debug.Log($"{weaponName} 获得 {amount} 发后备弹药 (当前:{ammo.reserveAmmo})");
            }
            else
            {
                ammo.currentAmmo = Mathf.Min(ammo.currentAmmo + amount, ammo.maxAmmo);
                Debug.Log($"{weaponName} 弹匣增加 {amount} 发 (当前:{ammo.currentAmmo})");
            }

            OnAmmoChanged?.Invoke(ammo.currentAmmo, ammo.reserveAmmo);
        }
    }

    // 获取弹药信息
    public (int current, int reserve) GetAmmoInfo(string weaponName)
    {
        if (ammoDictionary.TryGetValue(weaponName, out AmmoInfo ammo))
        {
            return (ammo.currentAmmo, ammo.reserveAmmo);
        }
        return (0, 0);
    }

    // 获取当前武器的最大弹匣容量
    public int GetCurrentWeaponMaxAmmo()
    {
        if (ammoDictionary.TryGetValue(currentWeaponName, out AmmoInfo ammo))
        {
            return ammo.maxAmmo;
        }
        return defaultMaxAmmo;
    }

    // 获取是否正在装填
    public bool IsReloading() => isReloading;

    // 自动装填（如果弹药不足）
    public bool CheckAndAutoReload(string weaponName)
    {
        if (NeedReload(weaponName) && !isReloading)
        {
            StartReload(weaponName);
            return true;
        }
        return false;
    }

    // 获取所有武器弹药信息（用于UI显示）
    public Dictionary<string, (int current, int reserve)> GetAllAmmoInfo()
    {
        Dictionary<string, (int current, int reserve)> allAmmo = new Dictionary<string, (int current, int reserve)>();

        foreach (var kvp in ammoDictionary)
        {
            allAmmo.Add(kvp.Key, (kvp.Value.currentAmmo, kvp.Value.reserveAmmo));
        }

        return allAmmo;
    }

    // 调试GUI
    void OnGUI()
    {
        if (string.IsNullOrEmpty(currentWeaponName) || !ammoDictionary.ContainsKey(currentWeaponName))
            return;

        var ammo = ammoDictionary[currentWeaponName];

        GUI.color = isReloading ? Color.yellow : Color.white;
        string reloadStatus = isReloading ? " [装填中]" : "";

        GUI.Label(new Rect(10, 100, 300, 20),
                 $"弹药: {ammo.currentAmmo}/{ammo.maxAmmo} | 后备: {ammo.reserveAmmo}{reloadStatus}");

        if (ammo.currentAmmo <= ammo.maxAmmo * 0.3f && !isReloading)
        {
            GUI.color = Color.yellow;
            GUI.Label(new Rect(10, 120, 300, 20), "弹药不足，按R装填");
        }
    }
}