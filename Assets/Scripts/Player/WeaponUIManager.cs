using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponUIManager : MonoBehaviour
{
    [Header("武器信息显示")]
    public Text weaponNameText;
    public Text ammoCountText;
    public Text reserveAmmoText;
    public Image weaponIcon;
    public Image reloadProgressBar;
    public GameObject reloadIndicator;

    [Header("武器图标")]
    public Sprite rifleIcon;
    public Sprite fireKirinIcon;
    public Sprite awmIcon;

    private WeaponManager weaponManager;
    private WeaponAmmo weaponAmmo;

    void Start()
    {
        weaponManager = FindObjectOfType<WeaponManager>();
        if (weaponManager != null)
        {
            weaponAmmo = weaponManager.GetComponent<WeaponAmmo>();
        }

        if (reloadIndicator != null)
            reloadIndicator.SetActive(false);
    }

    void Update()
    {
        if (weaponManager == null) return;

        UpdateWeaponInfo();
        UpdateReloadProgress();
    }

    void UpdateWeaponInfo()
    {
        // 更新武器名称
        if (weaponNameText != null)
        {
            weaponNameText.text = weaponManager.GetWeaponDisplayName(weaponManager.CurrentWeaponIndex);
        }

        // 更新弹药信息
        if (weaponAmmo != null)
        {
            var ammoInfo = weaponManager.GetCurrentWeaponAmmo();

            if (ammoCountText != null)
            {
                ammoCountText.text = ammoInfo.current.ToString();

                // 改变颜色
                if (ammoInfo.current <= 0)
                    ammoCountText.color = Color.red;
                else if (ammoInfo.current <= 5)
                    ammoCountText.color = Color.yellow;
                else
                    ammoCountText.color = Color.white;
            }

            if (reserveAmmoText != null)
            {
                reserveAmmoText.text = ammoInfo.reserve.ToString();
            }
        }

        // 更新武器图标
        if (weaponIcon != null)
        {
            string weaponName = weaponManager.GetWeaponName(weaponManager.CurrentWeaponIndex);

            if (weaponName.Contains("Rifle") && rifleIcon != null)
                weaponIcon.sprite = rifleIcon;
            else if (weaponName.Contains("FireKirin") && fireKirinIcon != null)
                weaponIcon.sprite = fireKirinIcon;
            else if (weaponName.Contains("AWM") && awmIcon != null)
                weaponIcon.sprite = awmIcon;
        }
    }

    void UpdateReloadProgress()
    {
        if (weaponAmmo == null || reloadIndicator == null || reloadProgressBar == null)
            return;

        bool isReloading = weaponAmmo.IsReloading();
        reloadIndicator.SetActive(isReloading);

        // 这里可以添加装填进度条的更新逻辑
        // 需要修改WeaponAmmo以提供装填进度信息
    }
}