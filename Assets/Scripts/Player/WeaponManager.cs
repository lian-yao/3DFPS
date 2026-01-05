using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("武器设置")]
    [SerializeField] private Transform handPoint;  // 手部挂载点
    [SerializeField] private List<GameObject> weapons = new List<GameObject>(); // 所有武器
    [SerializeField] private int defaultWeaponIndex = 0; // 默认武器索引

    [Header("切换设置")]
    [SerializeField] private float switchSpeed = 0.3f; // 切换速度
    [SerializeField] private AudioClip switchSound;   // 切换音效
    [SerializeField] private bool enableAutoReload = true; // 切换时自动装填

    private int currentWeaponIndex = -1; // 当前武器索引
    private bool isSwitching = false;    // 是否正在切换
    private Animator animator;
    private AudioSource audioSource;

    // 保存武器的原始位置和旋转（防止位置丢失）
    private Dictionary<GameObject, Vector3> weaponOriginalPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Quaternion> weaponOriginalRotations = new Dictionary<GameObject, Quaternion>();

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

        // 保存武器的原始位置和旋转
        SaveWeaponOriginalTransforms();

        // 初始化武器
        InitializeWeapons();

        // 切换到默认武器
        if (defaultWeaponIndex >= 0 && defaultWeaponIndex < weapons.Count)
        {
            SwitchWeapon(defaultWeaponIndex);
        }
    }

    void Update()
    {
        if (isSwitching) return;

        // 检测武器切换输入
        HandleWeaponSwitchInput();
    }

    // 保存武器的原始Transform
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
        else if (Input.GetKeyDown(KeyCode.Alpha4) && weapons.Count > 3)
        {
            SwitchWeapon(3);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5) && weapons.Count > 4)
        {
            SwitchWeapon(4);
        }

        // 使用滚轮切换
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0.1f) // 向上滚动
        {
            SwitchToNextWeapon();
        }
        else if (scroll < -0.1f) // 向下滚动
        {
            SwitchToPreviousWeapon();
        }

        // Q键切换上一个武器
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SwitchToPreviousWeapon();
        }
    }

    // 切换到指定索引的武器
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
            // Debug.Log($"已经是武器 {newIndex}");
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

        // 播放切换音效
        PlaySwitchSound();

        // 如果有当前武器，收起它
        if (currentWeaponIndex >= 0 && weapons[currentWeaponIndex] != null)
        {
            yield return StartCoroutine(HideWeaponCoroutine(currentWeaponIndex));
        }

        // 显示新武器
        yield return StartCoroutine(ShowWeaponCoroutine(newIndex));

        // 更新当前武器索引
        int previousWeaponIndex = currentWeaponIndex;
        currentWeaponIndex = newIndex;

        // 触发武器切换事件
        OnWeaponSwitched(previousWeaponIndex, newIndex);

        isSwitching = false;
    }

    IEnumerator HideWeaponCoroutine(int index)
    {
        GameObject weapon = weapons[index];

        // 如果有动画，可以在这里播放收起动画
        if (animator != null)
        {
            animator.SetTrigger("HideWeapon");
        }

        // 简单延迟后隐藏（模拟收起动作）
        yield return new WaitForSeconds(switchSpeed * 0.5f);

        // 重要：只隐藏，不重置位置！
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

        // 动画时间
        yield return new WaitForSeconds(switchSpeed * 0.5f);

        // 如果启用自动装填
        if (enableAutoReload)
        {
            // 可以在这里触发装填逻辑
            // WeaponBase weaponComponent = weapon.GetComponent<WeaponBase>();
            // if (weaponComponent != null) weaponComponent.CheckAmmoAndReload();
        }
    }

    void PlaySwitchSound()
    {
        if (switchSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(switchSound);
        }
    }

    void OnWeaponSwitched(int oldIndex, int newIndex)
    {
        Debug.Log($"武器切换: {GetWeaponName(oldIndex)} → {GetWeaponName(newIndex)}");

        // 可以在这里触发UI更新
        UpdateWeaponUI();
    }

    // 切换到下一把武器
    public void SwitchToNextWeapon()
    {
        if (weapons.Count <= 1) return;

        int newIndex = (currentWeaponIndex + 1) % weapons.Count;
        SwitchWeapon(newIndex);
    }

    // 切换到上一把武器
    public void SwitchToPreviousWeapon()
    {
        if (weapons.Count <= 1) return;

        int newIndex = currentWeaponIndex - 1;
        if (newIndex < 0) newIndex = weapons.Count - 1;
        SwitchWeapon(newIndex);
    }

    // 获取武器名称
    public string GetWeaponName(int index)
    {
        if (index < 0 || index >= weapons.Count || weapons[index] == null)
            return "无";

        return weapons[index].name;
    }

    // 更新武器UI（供外部调用）
    void UpdateWeaponUI()
    {
        // 这里可以添加UI更新逻辑
        // 例如：UIManager.Instance.UpdateWeaponIcon(CurrentWeapon);
    }

    // 添加新武器
    public void AddWeapon(GameObject newWeapon)
    {
        if (newWeapon == null) return;

        // 保存原始位置
        weaponOriginalPositions[newWeapon] = newWeapon.transform.localPosition;
        weaponOriginalRotations[newWeapon] = newWeapon.transform.localRotation;

        // 设置武器父对象
        newWeapon.transform.SetParent(handPoint, false);

        // 隐藏武器
        newWeapon.SetActive(false);

        // 添加到列表
        weapons.Add(newWeapon);

        Debug.Log($"添加武器: {newWeapon.name}");
    }

    // 移除武器
    public void RemoveWeapon(int index)
    {
        if (index < 0 || index >= weapons.Count) return;

        GameObject weaponToRemove = weapons[index];

        // 从字典中移除
        if (weaponOriginalPositions.ContainsKey(weaponToRemove))
        {
            weaponOriginalPositions.Remove(weaponToRemove);
            weaponOriginalRotations.Remove(weaponToRemove);
        }

        weapons.RemoveAt(index);

        if (weaponToRemove != null)
        {
            Destroy(weaponToRemove);
        }

        // 如果移除的是当前武器，切换到其他武器
        if (index == currentWeaponIndex)
        {
            if (weapons.Count > 0)
            {
                SwitchWeapon(0);
            }
            else
            {
                currentWeaponIndex = -1;
            }
        }
    }

    // 获取当前武器的Transform（供其他脚本使用）
    public Transform GetCurrentWeaponTransform()
    {
        if (CurrentWeapon != null)
            return CurrentWeapon.transform;
        return null;
    }

    // 获取当前武器的射击点（如果有）
    public Transform GetCurrentWeaponFirePoint()
    {
        if (CurrentWeapon == null) return null;

        // 假设射击点名为 "FirePoint" 或 "Muzzle"
        Transform firePoint = CurrentWeapon.transform.Find("FirePoint");
        if (firePoint == null) firePoint = CurrentWeapon.transform.Find("Muzzle");

        return firePoint;
    }

    // 调试信息
    void OnGUI()
    {
        if (weapons.Count == 0) return;

        // 显示当前武器信息
        GUI.Label(new Rect(10, 150, 300, 20), $"当前武器: {GetWeaponName(currentWeaponIndex)}");
        GUI.Label(new Rect(10, 170, 300, 20), $"武器列表:");

        // 显示所有武器状态
        for (int i = 0; i < weapons.Count; i++)
        {
            string weaponName = GetWeaponName(i);
            bool isActive = weapons[i] != null && weapons[i].activeSelf;
            string prefix = (i == currentWeaponIndex) ? "► " : "  ";
            string status = isActive ? " [激活]" : " [隐藏]";

            GUI.Label(new Rect(10, 190 + i * 20, 300, 20), $"{prefix}[{i + 1}] {weaponName}{status}");
        }

        // 显示控制提示
        GUI.Label(new Rect(10, 190 + weapons.Count * 20 + 10, 300, 60),
            "控制方式:\n" +
            "1/2/3: 直接切换武器\n" +
            "鼠标滚轮: 上下切换武器\n" +
            "Q键: 切换到上一把武器");

        // 显示武器位置信息（调试用）
        if (CurrentWeapon != null)
        {
            Vector3 pos = CurrentWeapon.transform.localPosition;
            GUI.Label(new Rect(10, 190 + weapons.Count * 20 + 80, 300, 20),
                $"武器位置: ({pos.x:F2}, {pos.y:F2}, {pos.z:F2})");
        }
    }

    // 编辑器辅助：在编辑器中预览武器位置
    void OnDrawGizmosSelected()
    {
        if (handPoint == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(handPoint.position, 0.05f);

        // 绘制武器位置预览
        foreach (GameObject weapon in weapons)
        {
            if (weapon != null)
            {
                Gizmos.color = weapon.activeSelf ? Color.red : Color.gray;
                Gizmos.DrawWireCube(weapon.transform.position, Vector3.one * 0.03f);
            }
        }
    }
}

// 如果需要武器数据，使用这个类
[System.Serializable]
public class WeaponInfo
{
    public string weaponName;
    public WeaponType weaponType;
    public int maxAmmo;
    public int damage;
    public float fireRate;
    public Sprite icon;
}

public enum WeaponType
{
    Pistol,
    Rifle,
    Shotgun,
    Sniper,
    Melee,
    Grenade
}