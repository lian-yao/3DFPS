using UnityEngine;

/// <summary>
/// 健康系统通用接口（玩家/敌人通用）
/// </summary>
public interface IHealth
{
    void TakeDamage(float damage);
    bool IsDead { get; }
}