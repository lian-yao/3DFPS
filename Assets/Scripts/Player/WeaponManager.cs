using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // 添加UI命名空间

public class WeaponManager : MonoBehaviour
{
    [Header("武器设置")]
    [SerializeField] private Transform handPoint;  // 手部挂载点
    [SerializeField] private List<GameObject> weapons = new List<GameObject>(); // 所有武器
    [SerializeField] private int defaultWeaponIndex = 0; // 默认武器索引

    [Header("UI设置")]
    [SerializeField] private Text weaponNameText; // 武器名称显示
    [SerializeField] private Text ammoText;      // 弹药显示
    [SerializeField] private Image weaponIcon;   // 武器图标（可选）

    [Header("动画设置")]
    [SerializeField] private Animator weaponAnimator;  // 手部/武器模型的Animator
    [SerializeField] private string fireSpeedParam = "FireSpeed"; // Animator中控制动画速度的参数名
    [SerializeField] private float animationSpeedMultiplier = 1.0f; // 动画速度乘数，用于微调

    [Header("切换设置")]
    [SerializeField] private float switchSpeed = 0.3f; // 切换速度
    [SerializeField] private AudioClip switchSound;   // 切换音效
    [SerializeField] private AudioClip switchRifleSound;   // 步枪切换音效
    [SerializeField] private AudioClip switchFireKirinSound; // 火麒麟切换音效
    [SerializeField] private AudioClip switchAWMSound; // AWM切换音效

    [Header("换弹音效")]
    public AudioClip rifleReloadSound;     // 步枪换弹音效
    public AudioClip fireKirinReloadSound; // 火麒麟换弹音效  
    public AudioClip awmReloadSound;       // AWM换弹音效

    [Header("弹药系统")]
    [SerializeField] private WeaponAmmo weaponAmmo;
    [SerializeField] private KeyCode reloadKey = KeyCode.R; // 装填键

    private int currentWeaponIndex = -1; // 当前武器索引
    private bool isSwitching = false;    // 是否正在切换
    private Animator animator;
    private AudioSource audioSource;

    // 保存武器的原始位置和旋转（防止位置丢失）
    private Dictionary<GameObject, Vector3> weaponOriginalPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Quaternion> weaponOriginalRotations = new Dictionary<GameObject, Quaternion>();

    // 武器名称映射
    private Dictionary<string, string> weaponDisplayNames = new Dictionary<string, string>()
    {
        { "Rifle", "步枪" },
        { "FireKirin", "火麒麟" },
        { "AWM", "AWM" }
    };

    // 属性
    public GameObject CurrentWeapon => currentWeaponIndex >= 0 ? weapons[currentWeaponIndex] : null;
    public int CurrentWeaponIndex => currentWeaponIndex;
    public int TotalWeapons => weapons.Count;

    void Start()
    {
        // 获取组件
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // 限制武器数量为3把
        if (weapons.Count > 3)
        {
            Debug.LogWarning($"武器数量超过3把，只保留前3把");
            weapons = weapons.GetRange(0, 3);
        }

        // 如果武器动画器未指定，尝试从子对象获取
        if (weaponAnimator == null)
        {
            weaponAnimator = GetComponentInChildren<Animator>();
            if (weaponAnimator == null && handPoint != null)
            {
                weaponAnimator = handPoint.GetComponent<Animator>();
            }
        }

        // 保存武器的原始位置和旋转
        SaveWeaponOriginalTransforms();

        // 初始化武器
        InitializeWeapons();

        // 初始化弹药系统
        InitializeAmmoSystem();

        // 切换到默认武器
        if (defaultWeaponIndex >= 0 && defaultWeaponIndex < weapons.Count)
        {
            SwitchWeapon(defaultWeaponIndex);
        }
    }

    // 初始化弹药系统
    void InitializeAmmoSystem()
    {
        if (weaponAmmo == null)
        {
            weaponAmmo = GetComponent<WeaponAmmo>();
            if (weaponAmmo == null)
            {
                weaponAmmo = gameObject.AddComponent<WeaponAmmo>();
            }
        }

        // 为三把武器注册弹药信息
        if (weapons.Count >= 1 && weapons[0] != null)
        {
            // 步枪：35/120
            weaponAmmo.RegisterWeapon(weapons[0].name, 35, 70);
            // 设置步枪换弹音效
            weaponAmmo.SetWeaponReloadSound(weapons[0].name, rifleReloadSound);
        }
        if (weapons.Count >= 2 && weapons[1] != null)
        {
            // 火麒麟：30/100
            weaponAmmo.RegisterWeapon(weapons[1].name, 30, 60);
            // 设置火麒麟换弹音效
            weaponAmmo.SetWeaponReloadSound(weapons[1].name, fireKirinReloadSound);
        }
        if (weapons.Count >= 3 && weapons[2] != null)
        {
            // AWM：5/30
            weaponAmmo.RegisterWeapon(weapons[2].name, 5, 10);
            // 设置AWM换弹音效
            weaponAmmo.SetWeaponReloadSound(weapons[2].name, awmReloadSound);
        }
    }

    void SaveWeaponOriginalTransforms()
    {
        foreach (GameObject weapon in weapons)
        {
            if (weapon != null)
            {
                weaponOriginalPositions[weapon] = weapon.transform.localPosition;
                weaponOriginalRotations[weapon] = weapon.transform.localRotation;
            }
        }
    }

    void InitializeWeapons()
    {
        if (handPoint == null)
        {
            Debug.LogError("未设置手部挂载点！");
            return;
        }

        // 如果weapons列表为空，自动查找子对象
        if (weapons.Count == 0)
        {
            FindWeaponsInChildren();
        }

        // 确保所有武器都在手部挂载点下
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] != null)
            {
                // 设置父对象
                weapons[i].transform.SetParent(handPoint, false);

                // 使用保存的原始位置和旋转
                if (weaponOriginalPositions.ContainsKey(weapons[i]))
                {
                    weapons[i].transform.localPosition = weaponOriginalPositions[weapons[i]];
                    weapons[i].transform.localRotation = weaponOriginalRotations[weapons[i]];
                }

                // 隐藏所有武器（等待切换时显示）
                weapons[i].SetActive(false);
            }
        }

        Debug.Log($"武器管理器初始化完成，找到 {weapons.Count} 把武器");
    }

    void FindWeaponsInChildren()
    {
        // 查找手部挂载点的所有子对象作为武器
        if (handPoint != null)
        {
            for (int i = 0; i < handPoint.childCount; i++)
            {
                Transform child = handPoint.GetChild(i);
                if (!weapons.Contains(child.gameObject))
                {
                    weapons.Add(child.gameObject);
                }
            }
        }
    }

    void Update()
    {
        if (isSwitching) return;

        // 检测武器切换输入
        HandleWeaponSwitchInput();

        // 检测装填输入
        HandleReloadInput();

        // 更新UI
        UpdateWeaponUI();
    }

    void HandleWeaponSwitchInput()
    {
        // 按数字键切换武器
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwitchWeapon(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwitchWeapon(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SwitchWeapon(2);
        }

        // 使用滚轮切换
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) // 向上滚动，切换到下一把武器
        {
            SwitchToNextWeapon();
        }
        else if (scroll < 0f) // 向下滚动，切换到上一把武器
        {
            SwitchToPreviousWeapon();
        }

        // Q键切换上一个武器
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SwitchToPreviousWeapon();
        }
    }

    void HandleReloadInput()
    {
        if (Input.GetKeyDown(reloadKey) && !isSwitching)
        {
            ReloadCurrentWeapon();
        }
    }

    public void SwitchWeapon(int newIndex)
    {
        // 检查索引是否有效
        if (newIndex < 0 || newIndex >= weapons.Count)
        {
            Debug.LogWarning($"武器索引 {newIndex} 无效，可用范围: 0-{weapons.Count - 1}");
            return;
        }

        // 检查是否已经是当前武器
        if (newIndex == currentWeaponIndex)
        {
            return;
        }

        // 检查武器对象是否存在
        if (weapons[newIndex] == null)
        {
            Debug.LogError($"武器 {newIndex} 对象为空！");
            return;
        }

        StartCoroutine(SwitchWeaponCoroutine(newIndex));
    }

    IEnumerator SwitchWeaponCoroutine(int newIndex)
    {
        isSwitching = true;

        // 播放对应的切换音效
        PlaySwitchSound(newIndex);

        // 如果有当前武器，收起它
        if (currentWeaponIndex >= 0 && weapons[currentWeaponIndex] != null)
        {
            // 取消当前武器的装填
            if (weaponAmmo != null)
            {
                weaponAmmo.CancelReload();
            }

            yield return StartCoroutine(HideWeaponCoroutine(currentWeaponIndex));
        }

        // 显示新武器
        yield return StartCoroutine(ShowWeaponCoroutine(newIndex));

        // 更新当前武器索引
        int previousWeaponIndex = currentWeaponIndex;
        currentWeaponIndex = newIndex;

        // 设置当前武器到弹药系统
        if (weaponAmmo != null && weapons[newIndex] != null)
        {
            weaponAmmo.SetCurrentWeapon(weapons[newIndex].name);
        }

        // 更新射击动画速度
        UpdateFireAnimationSpeed();

        // 触发武器切换事件
        OnWeaponSwitched(previousWeaponIndex, newIndex);

        // 检查是否需要自动装填
        //CheckAutoReload();

        isSwitching = false;
    }

    void CheckAutoReload()
    {
        if (CurrentWeapon != null && weaponAmmo != null)
        {
            string weaponName = CurrentWeapon.name;
            if (weaponAmmo.NeedReload(weaponName) && !weaponAmmo.IsReloading())
            {
                StartCoroutine(AutoReloadAfterSwitch());
            }
        }
    }

    IEnumerator AutoReloadAfterSwitch()
    {
        yield return new WaitForSeconds(0.5f);
        ReloadCurrentWeapon();
    }

    void UpdateFireAnimationSpeed()
    {
        if (weaponAnimator == null || currentWeaponIndex < 0)
        {
            return;
        }

        GameObject currentWeapon = weapons[currentWeaponIndex];
        if (currentWeapon == null) return;

        AutoPlayerShoot shootScript = currentWeapon.GetComponent<AutoPlayerShoot>();
        if (shootScript == null)
        {
            return;
        }

        // 获取射速
        float fireRate = shootScript.fireRate;
        if (fireRate <= 0.01f) fireRate = 0.01f;

        // 计算动画速度
        float baseSpeed = 1.0f;
        float calculatedSpeed = (baseSpeed / fireRate) * animationSpeedMultiplier;

        // 限制动画速度在合理范围内
        calculatedSpeed = Mathf.Clamp(calculatedSpeed, 0.3f, 3f);

        // 设置到Animator
        weaponAnimator.SetFloat(fireSpeedParam, calculatedSpeed);
    }

    void PlaySwitchSound(int weaponIndex)
    {
        AudioClip soundToPlay = switchSound;

        // 根据武器索引选择不同的音效
        if (weaponIndex == 0 && switchRifleSound != null)
        {
            soundToPlay = switchRifleSound;
        }
        else if (weaponIndex == 1 && switchFireKirinSound != null)
        {
            soundToPlay = switchFireKirinSound;
        }
        else if (weaponIndex == 2 && switchAWMSound != null)
        {
            soundToPlay = switchAWMSound;
        }

        if (soundToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(soundToPlay);
        }
    }

    public void ReloadCurrentWeapon()
    {
        if (currentWeaponIndex < 0 || weapons[currentWeaponIndex] == null) return;

        string weaponName = weapons[currentWeaponIndex].name;

        if (weaponAmmo != null && !weaponAmmo.IsReloading())
        {
            weaponAmmo.StartReload(weaponName);

            // 播放装填动画
            if (animator != null)
            {
                animator.SetTrigger("Reload");
            }

            Debug.Log($"开始装填: {weaponName}");
        }
    }

    public bool TryShootCurrentWeapon()
    {
        if (currentWeaponIndex < 0 || weapons[currentWeaponIndex] == null)
            return false;

        string weaponName = weapons[currentWeaponIndex].name;

        if (weaponAmmo != null)
        {
            // 消耗弹药
            bool canShoot = weaponAmmo.ConsumeAmmo(weaponName, 1);

            // 如果射击后子弹为0，自动装填
            if (canShoot)
            {
                var ammoInfo = weaponAmmo.GetAmmoInfo(weaponName);
                if (ammoInfo.current <= 0 && !weaponAmmo.IsReloading())
                {
                    StartCoroutine(AutoReloadAfterShooting());
                }
            }

            return canShoot;
        }

        return true;
    }

    IEnumerator AutoReloadAfterShooting()
    {
        yield return new WaitForSeconds(0.5f);
        ReloadCurrentWeapon();
    }

    public (int current, int reserve) GetCurrentWeaponAmmo()
    {
        if (currentWeaponIndex < 0 || weapons[currentWeaponIndex] == null || weaponAmmo == null)
            return (0, 0);

        return weaponAmmo.GetAmmoInfo(weapons[currentWeaponIndex].name);
    }

    IEnumerator HideWeaponCoroutine(int index)
    {
        GameObject weapon = weapons[index];

        // 如果有动画，可以在这里播放收起动画
        if (animator != null)
        {
            animator.SetTrigger("HideWeapon");
        }

        yield return new WaitForSeconds(switchSpeed * 0.5f);
        weapon.SetActive(false);
    }

    IEnumerator ShowWeaponCoroutine(int index)
    {
        GameObject weapon = weapons[index];

        // 确保武器位置正确
        if (weaponOriginalPositions.ContainsKey(weapon))
        {
            weapon.transform.localPosition = weaponOriginalPositions[weapon];
            weapon.transform.localRotation = weaponOriginalRotations[weapon];
        }

        // 激活武器
        weapon.SetActive(true);

        // 如果有动画，可以在这里播放拔出动画
        if (animator != null)
        {
            animator.SetTrigger("DrawWeapon");
        }

        yield return new WaitForSeconds(switchSpeed * 0.5f);
    }

    void OnWeaponSwitched(int oldIndex, int newIndex)
    {
        string oldName = GetWeaponName(oldIndex);
        string newName = GetWeaponName(newIndex);
        Debug.Log($"武器切换: {oldName} → {newName}");

        // 更新UI
        UpdateWeaponUI();
    }

    public void SwitchToNextWeapon()
    {
        if (weapons.Count <= 1) return;

        int newIndex = (currentWeaponIndex + 1) % weapons.Count;
        SwitchWeapon(newIndex);
    }

    public void SwitchToPreviousWeapon()
    {
        if (weapons.Count <= 1) return;

        int newIndex = currentWeaponIndex - 1;
        if (newIndex < 0) newIndex = weapons.Count - 1;
        SwitchWeapon(newIndex);
    }

    public string GetWeaponDisplayName(int index)
    {
        if (index < 0 || index >= weapons.Count || weapons[index] == null)
            return "无";

        string weaponName = weapons[index].name;
        if (weaponDisplayNames.ContainsKey(weaponName))
        {
            return weaponDisplayNames[weaponName];
        }
        return weaponName;
    }

    public string GetWeaponName(int index)
    {
        if (index < 0 || index >= weapons.Count || weapons[index] == null)
            return "无";

        return weapons[index].name;
    }

    void UpdateWeaponUI()
    {
        if (currentWeaponIndex < 0) return;

        // 更新武器名称
        if (weaponNameText != null)
        {
            weaponNameText.text = GetWeaponDisplayName(currentWeaponIndex);
        }

        // 更新弹药显示
        if (ammoText != null && weaponAmmo != null)
        {
            var ammoInfo = GetCurrentWeaponAmmo();
            string reloadStatus = weaponAmmo.IsReloading() ? " [装填中...]" : "";
            ammoText.text = $"{ammoInfo.current} / {ammoInfo.reserve}{reloadStatus}";

            // 根据弹药数量改变颜色
            if (ammoInfo.current <= 0)
            {
                ammoText.color = Color.red;
            }
            else if (ammoInfo.current <= 5)
            {
                ammoText.color = Color.yellow;
            }
            else
            {
                ammoText.color = Color.white;
            }
        }

        // 更新武器图标（如果有）
        if (weaponIcon != null)
        {
            // 这里可以根据武器类型设置不同的图标
        }
    }

    public void RefreshUI()
    {
        UpdateWeaponUI();
    }

    public bool IsReloading()
    {
        return weaponAmmo != null && weaponAmmo.IsReloading();
    }

    // 获取弹药系统引用（供外部使用）
    public WeaponAmmo WeaponAmmoSystem
    {
        get { return weaponAmmo; }
    }

    public float GetCurrentWeaponFireRate()
    {
        if (currentWeaponIndex < 0 || currentWeaponIndex >= weapons.Count) return 0.5f;

        GameObject weapon = weapons[currentWeaponIndex];
        if (weapon == null) return 0.5f;

        AutoPlayerShoot shootScript = weapon.GetComponent<AutoPlayerShoot>();
        return shootScript != null ? shootScript.fireRate : 0.5f;
    }

    void OnGUI()
    {
        if (weapons.Count == 0) return;

        GUI.Label(new Rect(10, 150, 300, 20), $"当前武器: {GetWeaponDisplayName(currentWeaponIndex)}");

        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
        {
            float fireRate = GetCurrentWeaponFireRate();
            float rpm = 60f / fireRate;

            GUI.Label(new Rect(10, 170, 300, 20), $"射速: {fireRate:F2}s/发 ({rpm:F0} RPM)");

            if (weaponAmmo != null)
            {
                var ammoInfo = GetCurrentWeaponAmmo();
                GUI.color = weaponAmmo.IsReloading() ? Color.yellow : Color.white;
                string reloadStatus = weaponAmmo.IsReloading() ? " [装填中]" : "";
                GUI.Label(new Rect(10, 190, 300, 20), $"弹药: {ammoInfo.current}/{ammoInfo.reserve}{reloadStatus}");
            }
        }
    }
}