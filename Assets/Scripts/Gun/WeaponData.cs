using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "武器/武器数据")]
public class WeaponData : ScriptableObject
{
    [Header("基础信息")]
    public string weaponName; // 武器名称
    public WeaponType weaponType; // 武器类型
    public Sprite weaponIcon; // 武器图标（UI显示）

    [Header("战斗属性")]
    public float baseDamage; // 基础伤害
    public int clipSize; // 弹夹容量
    public int maxTotalAmmo; // 最大总弹药
    public float fireRate; // 射击间隔（秒/发）
    public float reloadTime; // 换弹时间（秒）
    public float accuracy; // 射击精度（0=无散射，1=最大散射）

    [Header("资源引用")]
    public GameObject weaponPrefab; // 武器模型预制体
    public AudioClip fireSound; // 射击音效
    public AudioClip reloadSound; // 换弹音效
    public ParticleSystem muzzleFlash; // 枪口特效
    public GameObject crosshairPrefab; // 准星预制体

    // 武器类型枚举
    public enum WeaponType
    {
        步枪,
        火麒麟,
        狙击枪
    }
}