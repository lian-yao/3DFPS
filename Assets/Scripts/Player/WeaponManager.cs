using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("武器设置")]
    [SerializeField] private Transform handPoint;  // 手部挂载点
    [SerializeField] private List<GameObject> weapons = new List<GameObject>(); // 所有武器
    [SerializeField] private int defaultWeaponIndex = 0; // 默认武器索引

    [Header("动画设置")]
    [SerializeField] private Animator weaponAnimator;  // 手部/武器模型的Animator
    [SerializeField] private string fireSpeedParam = "FireSpeed"; // Animator中控制动画速度的参数名
    [SerializeField] private float animationSpeedMultiplier = 1.0f; // 动画速度乘数，用于微调

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

        // 更新射击动画速度（新增的关键功能）
        UpdateFireAnimationSpeed();

        // 触发武器切换事件
        OnWeaponSwitched(previousWeaponIndex, newIndex);

        isSwitching = false;
    }

    // 新增：更新射击动画速度
    void UpdateFireAnimationSpeed()
    {
        if (weaponAnimator == null || currentWeaponIndex < 0)
        {
            Debug.LogWarning("无法更新动画速度：Animator未设置或当前武器索引无效");
            return;
        }

        GameObject currentWeapon = weapons[currentWeaponIndex];
        if (currentWeapon == null) return;

        // 尝试获取当前武器的AutoPlayerShoot组件
        AutoPlayerShoot shootScript = currentWeapon.GetComponent<AutoPlayerShoot>();
        if (shootScript == null)
        {
            Debug.LogWarning($"武器 {currentWeapon.name} 上没有找到AutoPlayerShoot组件");
            return;
        }

        // 获取射速（fireRate是射击间隔，单位：秒）
        float fireRate = shootScript.fireRate;

        // 防止除零错误
        if (fireRate <= 0.01f) fireRate = 0.01f;

        // 计算动画速度：射速越快（fireRate越小），动画速度应该越快
        // 基础公式：动画速度 = 基础速度 / 射击间隔
        float baseSpeed = 1.0f;
        float calculatedSpeed = (baseSpeed / fireRate) * animationSpeedMultiplier;

        // 限制动画速度在合理范围内
        calculatedSpeed = Mathf.Clamp(calculatedSpeed, 0.3f, 3f);

        // 设置到Animator
        weaponAnimator.SetFloat(fireSpeedParam, calculatedSpeed);

        // 调试信息
        float rpm = 60f / fireRate; // 转换为每分钟射速
        Debug.Log($"武器切换: {currentWeapon.name} | " +
                 $"射速: {fireRate:F2}s/发 ({rpm:F0} RPM) | " +
                 $"动画速度: {calculatedSpeed:F2}x");
    }

    // 新增：手动更新动画速度（可以在外部调用）
    public void RefreshAnimationSpeed()
    {
        UpdateFireAnimationSpeed();
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
        string oldName = GetWeaponName(oldIndex);
        string newName = GetWeaponName(newIndex);
        Debug.Log($"武器切换: {oldName} → {newName}");

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

    // 获取当前武器的射速（供外部使用）
    public float GetCurrentWeaponFireRate()
    {
        if (currentWeaponIndex < 0 || currentWeaponIndex >= weapons.Count) return 0.5f;

        GameObject weapon = weapons[currentWeaponIndex];
        if (weapon == null) return 0.5f;

        AutoPlayerShoot shootScript = weapon.GetComponent<AutoPlayerShoot>();
        return shootScript != null ? shootScript.fireRate : 0.5f;
    }

    // 获取当前武器的动画速度（供外部使用）
    public float GetCurrentAnimationSpeed()
    {
        if (weaponAnimator != null)
        {
            return weaponAnimator.GetFloat(fireSpeedParam);
        }
        return 1.0f;
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

        // 显示射速和动画速度信息
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
        {
            float fireRate = GetCurrentWeaponFireRate();
            float animationSpeed = GetCurrentAnimationSpeed();
            float rpm = 60f / fireRate;

            GUI.Label(new Rect(10, 170, 300, 20), $"射速: {fireRate:F2}s/发 ({rpm:F0} RPM)");
            GUI.Label(new Rect(10, 190, 300, 20), $"动画速度: {animationSpeed:F2}x");
        }

        GUI.Label(new Rect(10, 210, 300, 20), $"武器列表:");

        // 显示所有武器状态
        for (int i = 0; i < weapons.Count; i++)
        {
            string weaponName = GetWeaponName(i);
            bool isActive = weapons[i] != null && weapons[i].activeSelf;
            string prefix = (i == currentWeaponIndex) ? "► " : "  ";
            string status = isActive ? " [激活]" : " [隐藏]";

            GUI.Label(new Rect(10, 230 + i * 20, 300, 20), $"{prefix}[{i + 1}] {weaponName}{status}");
        }

        // 显示控制提示
        GUI.Label(new Rect(10, 230 + weapons.Count * 20 + 10, 300, 80),
            "控制方式:\n" +
            "1/2/3: 直接切换武器\n" +
            "鼠标滚轮: 上下切换武器\n" +
            "Q键: 切换到上一把武器\n" +
            "鼠标左键: 射击 (AutoPlayerShoot)\n" +
            "动画速度自动根据武器射速调整");

        // 显示武器位置信息（调试用）
        if (CurrentWeapon != null)
        {
            Vector3 pos = CurrentWeapon.transform.localPosition;
            GUI.Label(new Rect(10, 230 + weapons.Count * 20 + 90, 300, 20),
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