using UnityEngine;

/// <summary>
/// 远程攻击行为实现
/// </summary>
public class RangedAttackBehaviour : MonoBehaviour, IAttackBehaviour
{
    [Header("远程攻击设置")]
    public int attackType = 1;
    public float attackDamage = 25f;
    public float attackCooldown = 2f;
    public float attackRange = 8f;
    public float maxAttackHeight = 6f;
    private float lastAttackTime;
    
    [Header("投射物设置")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;
    public float projectileLifetime = 5f;
    public Transform firePoint;
    
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
            Debug.LogWarning($"{attacker.name} 不满足远程攻击条件: 冷却时间未到 (已过 {timeSinceLastAttack:F2}s, 冷却时间 {Cooldown:F2}s)");
            return false;
        }
        
        // 检查投射物预制体
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"{attacker.name} 不满足远程攻击条件: 投射物预制体为空");
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
            Debug.LogWarning($"{attacker.name} 不满足远程攻击条件: 距离超出范围 (距离 {horizontalDist:F2}, 攻击范围 {AttackRange:F2})");
            return false;
        }
        
        // 检查高度差
        float heightDiff = Mathf.Abs(target.position.y - attacker.position.y);
        if (heightDiff > MaxAttackHeight)
        {
            Debug.LogWarning($"{attacker.name} 不满足远程攻击条件: 高度差超出范围 (高度差 {heightDiff:F2}, 最大允许 {MaxAttackHeight:F2})");
            return false;
        }
        
        return true;
    }

    public void ExecuteAttack(Transform attacker, IHealth target)
    {
        if (target != null && !target.IsDead && projectilePrefab != null)
        {
            // 获取目标的Transform
            Transform targetTransform = ((MonoBehaviour)target).transform;
            
            // 创建投射物
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            
            // 设置投射物速度
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 direction = (targetTransform.position - firePoint.position).normalized;
                rb.velocity = direction * projectileSpeed;
            }
            
            // 添加投射物组件
            EnemyProjectile projectileComp = projectile.AddComponent<EnemyProjectile>();
            projectileComp.damage = attackDamage;
            projectileComp.targetHealth = target;
            
            // 设置投射物生命周期
            Destroy(projectile, projectileLifetime);
            
            lastAttackTime = Time.time;
        }
    }

    public void OnAttackAnimationEnd()
    {
        // 远程攻击动画结束时的处理
    }
}

/// <summary>
/// 敌人投射物组件
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    public float damage = 25f;
    public IHealth targetHealth;
    public LayerMask collisionLayers;
    
    private void OnTriggerEnter(Collider other)
    {
        // 检查是否命中有效目标
        if (collisionLayers == (collisionLayers | (1 << other.gameObject.layer)))
        {
            // 对目标造成伤害
            if (targetHealth != null && !targetHealth.IsDead)
            {
                targetHealth.TakeDamage(damage);
            }
            
            // 可以添加击中效果
            Destroy(gameObject);
        }
    }
}