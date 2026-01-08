using UnityEngine;

/// <summary>
/// 近战攻击行为实现
/// </summary>
public class MeleeAttackBehaviour1 : MonoBehaviour, IAttackBehaviour
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
        if (Time.time - lastAttackTime < Cooldown) return false;

        // 检查距离/高度
        float horizontalDist = Vector3.Distance(
            new Vector3(target.position.x, attacker.position.y, target.position.z),
            attacker.position
        );
        
        // 检查攻击范围
        if (horizontalDist > AttackRange) return false;
        
        // 检查高度差
        float heightDiff = Mathf.Abs(target.position.y - attacker.position.y);
        if (heightDiff > MaxAttackHeight) return false;
        
        return true;
    }

    public void ExecuteAttack(Transform attacker, IHealth target)
    {
        if (target != null && !target.IsDead)
        {
            target.TakeDamage(attackDamage);
            lastAttackTime = Time.time;
        }
    }

    public void OnAttackAnimationEnd()
    {
        // 近战攻击动画结束时的处理
    }
}