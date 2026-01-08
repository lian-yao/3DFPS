using UnityEngine;

/// <summary>
/// 近战攻击行为实现
/// </summary>
public class MeleeAttackBehaviour : MonoBehaviour, IAttackBehaviour
{
    [Header("近战攻击设置")]
    public int attackType = 0; // 攻击类型标识（区分不同攻击方式）
    public float attackDamage = 10f; // 近战攻击伤害值
    public float attackCooldown = 1f; // 攻击冷却时间（两次攻击的最小间隔）
    public float attackRange = 2f; // 近战攻击有效距离（水平范围）
    public float maxAttackHeight = 4f; // 攻击允许的最大高度差（忽略过高/过低的目标）
    private float lastAttackTime; // 上一次攻击的时间戳（用于判断冷却是否结束）

    public int AttackType => attackType;
    public float Cooldown => attackCooldown;
    public float AttackRange => attackRange;
    public float MaxAttackHeight => maxAttackHeight;

    public bool CanAttack(Transform attacker, Transform target)
    {
        // 检查冷却时间
        float timeSinceLastAttack = Time.time - lastAttackTime;
        if (timeSinceLastAttack < Cooldown)
        {
            Debug.LogWarning($"{attacker.name} 不满足攻击条件: 冷却时间未到 (已过 {timeSinceLastAttack:F2}s, 冷却时间 {Cooldown:F2}s)");
            return false;
        }

        // 检查距离/高度
        float horizontalDist = Vector3.Distance(
            new Vector3(target.position.x, attacker.position.y, target.position.z),
            attacker.position
        );
        
        // 检查攻击范围
        if (horizontalDist > AttackRange)
        {
            Debug.LogWarning($"{attacker.name} 不满足攻击条件: 距离超出范围 (距离 {horizontalDist:F2}, 攻击范围 {AttackRange:F2})");
            return false;
        }
        
        // 检查高度差
        float heightDiff = Mathf.Abs(target.position.y - attacker.position.y);
        if (heightDiff > MaxAttackHeight)
        {
            Debug.LogWarning($"{attacker.name} 不满足攻击条件: 高度差超出范围 (高度差 {heightDiff:F2}, 最大允许 {MaxAttackHeight:F2})");
            return false;
        }
        
        return true;
    }

    public void ExecuteAttack(Transform attacker, IHealth target)
    {
        if (target != null && !target.IsDead)
        {
            target.TakeDamage(attackDamage);
            Debug.Log($"{attacker.name} 执行伤害: {attackDamage}，攻击类型: {attackType}");
        }
    }

    public void OnAttackAnimationEnd()
    {
        // 攻击动画结束时更新冷却时间
        lastAttackTime = Time.time;
        Debug.Log($"{gameObject.name} 攻击动画结束，更新冷却时间");
    }
}