using UnityEngine;

/// <summary>
/// 攻击行为接口（支持多攻击方式扩展）
/// </summary>
public interface IAttackBehaviour
{
    int AttackType { get; }          // 攻击类型标识
    float Cooldown { get; }          // 冷却时间
    float AttackRange { get; }       // 攻击范围
    float MaxAttackHeight { get; }   // 最大攻击高度
    bool CanAttack(Transform attacker, Transform target); // 是否可攻击
    void ExecuteAttack(Transform attacker, IHealth target); // 执行攻击
    void OnAttackAnimationEnd();     // 攻击动画结束回调
}