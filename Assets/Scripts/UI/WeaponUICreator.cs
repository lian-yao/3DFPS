
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponUICreator : MonoBehaviour
{
    [Header("UI组件引用（手动拖拽）")]
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI ammoText;

    [Header("游戏组件引用（手动拖拽）")]
    public WeaponManager weaponManager;
    public WeaponAmmo weaponAmmo;  // 需要这个

    void Start()
    {
        // 检查必要的引用
        if (weaponNameText == null)
            Debug.LogError("请将WeaponNameText拖拽到脚本中！");
        if (ammoText == null)
            Debug.LogError("请将AmmoText拖拽到脚本中！");
        if (weaponManager == null)
            Debug.LogError("请将WeaponManager拖拽到脚本中！");
        if (weaponAmmo == null)
            Debug.LogError("请将WeaponAmmo拖拽到脚本中！");

        // 初始更新
        UpdateUI();
    }

    void Update()
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        if (weaponManager == null || weaponAmmo == null) return;

        // 更新武器名称
        if (weaponNameText != null)
        {
            string displayName = weaponManager.GetWeaponDisplayName(weaponManager.CurrentWeaponIndex);
            weaponNameText.text = displayName;
        }

        // 更新弹药显示
        if (ammoText != null)
        {
            var ammoInfo = weaponManager.GetCurrentWeaponAmmo();

            // 检查是否正在装填
            if (weaponAmmo.IsReloading())
            {
                ammoText.text = "装填中...";
                ammoText.color = Color.yellow;
            }
            else
            {
                ammoText.text = $"{ammoInfo.current} / {ammoInfo.reserve}";

                // 根据弹药量改变颜色
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
        }
    }

    // 公开方法：手动刷新UI
    public void RefreshUI()
    {
        UpdateUI();
    }
}