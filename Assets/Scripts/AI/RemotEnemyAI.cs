using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemotEnemyAI : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;
    public float stoppingDistance = 8f; // 远程：停在8米外攻击（原近战2米）

    [Header("重力设置")]
    public float gravity = 9.81f;
    public float groundedGravity = -2f; // 小的向下力确保站在地面上

    [Header("检测设置")]
    public float detectionRange = 15f; // 远程：检测范围更远（原10米）
    public float checkInterval = 0.3f;
    public bool checkLineOfSight = true; // 远程：是否检测视野（穿墙不攻击）

    [Header("远程攻击设置")]
    public float attackDamage = 10f;
    public float attackCooldown = 2f; // 远程：攻击冷却更长（原1秒）
    public float maxAttackHeight = 4f;
    public GameObject projectilePrefab; // 子弹/技能预制体（需手动拖入）
    public Transform firePoint; // 攻击发射点（怪物枪口/技能释放点）
    public float projectileSpeed = 15f; // 子弹速度
    public float attackWindup = 0.5f; // 攻击前摇（抬手时间）
    public float projectileLifetime = 3f; // 子弹生命周期（避免内存泄漏）

    [Header("调试")]
    public bool showGizmos = true;

    // 引用组件 - 在Unity编辑器中手动拖拽
    [Header("组件引用")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform enemyModel; // 可选：模型子物体引用

    // 私有变量
    private Transform player;
    private float nextCheckTime;
    private float attackTimer;
    private Vector3 targetPosition;
    private bool isChasing = false;
    private Vector3 velocity; // 用于重力计算
    private bool isGrounded;
    private bool isAttacking = false; // 标记是否在攻击前摇中

    void Start()
    {
        // 1. 查找玩家
        FindPlayer();

        // 2. 获取组件引用（如果未手动设置）
        GetComponentReferences();

        // 3. 初始化velocity
        velocity = Vector3.zero;

        // 4. 验证必要组件
        ValidateComponents();

        // 5. 远程攻击参数验证
        ValidateRangedAttackSettings();

        UnityEngine.Debug.Log($"{name} 远程AI初始化完成");
    }

    void GetComponentReferences()
    {
        // 如果未手动设置CharacterController，尝试自动获取
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        // 如果未手动设置模型，尝试查找子物体模型
        if (enemyModel == null)
        {
            FindEnemyModel();
        }

        // 如果还是没有CharacterController，记录错误但继续运行
        if (characterController == null)
        {
            UnityEngine.Debug.LogError($"{name}: 未找到CharacterController组件！请在Inspector中手动拖拽赋值。");
        }
    }

    void FindEnemyModel()
    {
        // 查找第一个有渲染器的子物体作为模型
        foreach (Transform child in transform)
        {
            if (child.GetComponent<Renderer>() != null)
            {
                enemyModel = child;
                UnityEngine.Debug.Log($"找到模型子物体: {enemyModel.name}");
                break;
            }
        }
    }

    void ValidateComponents()
    {
        // 检查必要组件
        if (characterController == null)
        {
            UnityEngine.Debug.LogWarning($"{name}: 缺少CharacterController，AI移动功能将不可用！");
            UnityEngine.Debug.LogWarning("请在Inspector面板中手动拖拽CharacterController组件到characterController字段。");
        }

        // 检查玩家引用
        if (player == null)
        {
            UnityEngine.Debug.LogError($"{name}: 未找到玩家！");
            enabled = false;
        }
    }

    // 验证远程攻击参数
    void ValidateRangedAttackSettings()
    {
        if (projectilePrefab == null)
        {
            UnityEngine.Debug.LogWarning($"{name}: 未赋值子弹预制体！请拖入projectilePrefab字段");
        }

        if (firePoint == null)
        {
            UnityEngine.Debug.LogWarning($"{name}: 未设置攻击发射点！请创建空物体作为firePoint并拖入");
            // 自动创建默认发射点（备用）
            GameObject defaultFirePoint = new GameObject("DefaultFirePoint");
            defaultFirePoint.transform.parent = transform;
            defaultFirePoint.transform.localPosition = new Vector3(0, 1.5f, 0.5f); // 怪物头部/枪口位置
            firePoint = defaultFirePoint.transform;
            UnityEngine.Debug.Log($"自动创建默认发射点: {firePoint.name}，请调整位置到怪物攻击点");
        }
    }

    void FindPlayer()
    {
        // 方法1：通过标签（最常用）
        GameObject playerObj = GameObject.FindWithTag("Player");

        // 方法2：通过名字
        if (playerObj == null)
        {
            playerObj = GameObject.Find("Player");
        }

        // 方法3：使用更通用的查找方法
        if (playerObj == null)
        {
            playerObj = FindPlayerByComponents();
        }

        // 方法4：手动查找包含"player"名称的对象
        if (playerObj == null)
        {
            playerObj = FindPlayerByName();
        }

        if (playerObj != null)
        {
            player = playerObj.transform;
            UnityEngine.Debug.Log($"找到玩家: {player.name}");
        }
        else
        {
            UnityEngine.Debug.LogError("找不到玩家！请确保玩家物体存在并具有'Player'标签");
            UnityEngine.Debug.LogError("或者创建一个名为'Player'的GameObject，或为其添加'Player'标签");
        }
    }

    // 通用的玩家查找方法 - 不依赖特定组件
    GameObject FindPlayerByComponents()
    {
        // 尝试查找常见的玩家组件
        GameObject[] allGameObjects = GameObject.FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allGameObjects)
        {
            // 检查是否包含常见的玩家组件
            if (obj.GetComponent<CharacterController>() != null &&
                obj.GetComponent<Camera>() == null) // 排除主摄像机
            {
                UnityEngine.Debug.Log($"通过CharacterController找到玩家: {obj.name}");
                return obj;
            }

            // 检查是否包含Rigidbody（可能是玩家）
            if (obj.GetComponent<Rigidbody>() != null &&
                obj.GetComponent<Rigidbody>().isKinematic == false &&
                obj.name.ToLower().Contains("player"))
            {
                UnityEngine.Debug.Log($"通过Rigidbody找到玩家: {obj.name}");
                return obj;
            }
        }

        return null;
    }

    GameObject FindPlayerByName()
    {
        // 查找所有GameObject
        GameObject[] allGameObjects = GameObject.FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allGameObjects)
        {
            string objName = obj.name.ToLower();

            // 检查名称是否包含玩家相关关键词
            if (objName.Contains("player") ||
                objName.Contains("主角") ||
                objName.Contains("角色") ||
                objName.Contains("character"))
            {
                // 进一步验证：不是UI元素等
                if (obj.GetComponent<Canvas>() == null &&
                    obj.GetComponent<Camera>() == null)
                {
                    UnityEngine.Debug.Log($"通过名称找到玩家: {obj.name}");
                    return obj;
                }
            }
        }

        return null;
    }

    void Update()
    {
        if (player == null) return;
        if (characterController == null) return;

        // 检查是否接地
        CheckGrounded();

        // 应用重力
        ApplyGravity();

        // 攻击冷却
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }

        // 定期检测玩家
        if (Time.time >= nextCheckTime)
        {
            CheckForPlayer();
            nextCheckTime = Time.time + checkInterval;
        }

        // AI行为
        if (isChasing)
        {
            ChasePlayer();
        }
        else
        {
            // 可以在这里添加巡逻逻辑
            // Patrol();
        }
    }

    void CheckGrounded()
    {
        // 使用CharacterController的isGrounded属性
        isGrounded = characterController != null && characterController.isGrounded;
    }

    void ApplyGravity()
    {
        if (characterController == null) return;

        if (isGrounded && velocity.y < 0)
        {
            // 在地面上时，应用小的向下力以确保保持在地面上
            velocity.y = groundedGravity;
        }
        else
        {
            // 应用重力
            velocity.y -= gravity * Time.deltaTime;
        }
    }

    void CheckForPlayer()
    {
        if (player == null) return;

        // 计算到玩家的距离（只考虑水平距离）
        Vector3 playerPos = player.position;
        Vector3 enemyPos = transform.position;

        // 忽略Y轴计算水平距离
        playerPos.y = enemyPos.y;
        float horizontalDistance = Vector3.Distance(enemyPos, playerPos);

        // 简单距离检测 + 视野检测（远程专属）
        bool hasLineOfSight = true;
        if (checkLineOfSight)
        {
            hasLineOfSight = CheckLineOfSightToPlayer();
        }

        // 只有在检测范围内且有视野时，才开始追逐
        if (horizontalDistance <= detectionRange && hasLineOfSight)
        {
            if (!isChasing)
            {
                // 开始追踪
                targetPosition = player.position;
                UnityEngine.Debug.Log($"{name} 发现玩家，开始远程追踪");
            }
            isChasing = true;
        }
        else
        {
            if (isChasing)
            {
                UnityEngine.Debug.Log($"{name} 失去玩家视野/超出范围");
            }
            isChasing = false;
        }
    }

    // 射线检测：是否有视野（穿墙不攻击）
    bool CheckLineOfSightToPlayer()
    {
        if (player == null || firePoint == null) return false;

        // 射线起点：发射点，终点：玩家中心（加Y偏移避免地面遮挡）
        Vector3 targetPos = player.position + new Vector3(0, 1f, 0);
        Vector3 direction = targetPos - firePoint.position;

        // 射线检测（忽略自身、忽略触发器）
        RaycastHit hit;
        if (Physics.Raycast(firePoint.position, direction.normalized, out hit, detectionRange))
        {
            // 检测是否命中玩家
            if (hit.collider.CompareTag("Player"))
            {
                return true;
            }
            else
            {
                // 命中障碍物（墙/地形）
                UnityEngine.Debug.DrawLine(firePoint.position, hit.point, Color.yellow);
                return false;
            }
        }

        // 无命中（超出范围）
        return false;
    }

    void ChasePlayer()
    {
        if (player == null) return;
        if (characterController == null) return;
        if (isAttacking) return; // 攻击前摇中不移动

        // 更新目标位置
        targetPosition = player.position;

        // 计算水平距离（忽略Y轴）
        Vector3 horizontalDirection = targetPosition - transform.position;
        horizontalDirection.y = 0;
        float horizontalDistance = horizontalDirection.magnitude;

        // 远程逻辑：距离大于停止距离（8米）则移动，否则停止并攻击
        if (horizontalDistance > stoppingDistance)
        {
            if (horizontalDirection.magnitude > 0.1f)
            {
                // 计算移动方向
                Vector3 direction = horizontalDirection.normalized;

                // 准备移动向量
                Vector3 moveVector = direction * moveSpeed * Time.deltaTime;

                // 添加Y轴移动（重力）
                moveVector.y = velocity.y * Time.deltaTime;

                // 使用CharacterController移动
                if (characterController.enabled)
                {
                    characterController.Move(moveVector);
                }

                // 旋转面向玩家（远程需要始终朝向玩家）
                if (direction.magnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);

                    if (enemyModel != null)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                                                             rotationSpeed * Time.deltaTime);
                    }
                    else
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                                                             rotationSpeed * Time.deltaTime);
                    }
                }
            }
        }
        else
        {
            // 远程攻击范围：检查高度差 + 视野
            float verticalDistance = Mathf.Abs(targetPosition.y - transform.position.y);
            bool hasLineOfSight = checkLineOfSight ? CheckLineOfSightToPlayer() : true;

            if (verticalDistance <= maxAttackHeight && hasLineOfSight)
            {
                TryRangedAttack(); // 远程攻击（替代原近战TryAttack）
            }
        }
    }

    // 远程攻击核心逻辑
    void TryRangedAttack()
    {
        // 检查攻击冷却 + 不在攻击前摇中
        if (attackTimer > 0 || isAttacking) return;

        UnityEngine.Debug.Log($"{name} 准备远程攻击玩家");
        StartCoroutine(RangedAttackCoroutine()); // 协程处理攻击前摇
    }

    // 攻击前摇 + 发射子弹
    IEnumerator RangedAttackCoroutine()
    {
        isAttacking = true;

        // 攻击前摇（抬手动画时间）
        yield return new WaitForSeconds(attackWindup);

        // 发射子弹
        FireProjectile();

        // 重置状态
        attackTimer = attackCooldown;
        isAttacking = false;
    }

    // 发射子弹/技能
    void FireProjectile()
    {
        if (projectilePrefab == null || firePoint == null)
        {
            UnityEngine.Debug.LogError($"{name} 子弹预制体/发射点未设置，无法发射");
            return;
        }

        // 1. 计算子弹朝向（瞄准玩家，加微小随机偏移增加难度）
        Vector3 targetPos = player.position + new Vector3(0, 1f, 0); // 瞄准玩家胸部
        Vector3 fireDirection = (targetPos - firePoint.position).normalized;

        // 可选：添加弹道随机偏移（模拟瞄准误差）
        fireDirection += new Vector3(
            Random.Range(-0.05f, 0.05f),
            Random.Range(-0.03f, 0.03f),
            Random.Range(-0.05f, 0.05f)
        );
        fireDirection.Normalize();

        // 2. 生成子弹预制体
        GameObject projectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.LookRotation(fireDirection)
        );

        // 3. 给子弹添加速度
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = fireDirection * projectileSpeed;
        }
        else
        {
            // 没有Rigidbody则添加（备用方案）
            rb = projectile.AddComponent<Rigidbody>();
            rb.velocity = fireDirection * projectileSpeed;
            rb.useGravity = false; // 远程子弹默认无重力（可根据需求调整）
        }

        // 4. 设置子弹伤害 + 自动销毁
        ProjectileDamage projectileDamage = projectile.GetComponent<ProjectileDamage>();
        if (projectileDamage != null)
        {
            projectileDamage.damage = attackDamage;
        }
        else
        {
            // 自动添加子弹伤害组件（如果没有）
            projectileDamage = projectile.AddComponent<ProjectileDamage>();
            projectileDamage.damage = attackDamage;
        }

        // 5. 子弹超时销毁（避免内存泄漏）
        Destroy(projectile, projectileLifetime);

        UnityEngine.Debug.Log($"{name} 发射子弹，速度: {projectileSpeed}，伤害: {attackDamage}");
    }

    // 移除远程不需要的碰撞攻击（注释/删除均可）
    // void OnControllerColliderHit(ControllerColliderHit hit)
    // {
    //     原近战碰撞攻击逻辑...
    // }

    // 在编辑器中绘制调试信息（新增远程攻击相关Gizmos）
    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // 检测范围
        Gizmos.color = isChasing ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 远程攻击停止距离（绿色）
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);

        // 到玩家的线（红色=有视野，黄色=无视野）
        if (player != null)
        {
            if (checkLineOfSight && Application.isPlaying)
            {
                Gizmos.color = CheckLineOfSightToPlayer() ? Color.red : Color.yellow;
            }
            else
            {
                Gizmos.color = isChasing ? Color.red : Color.gray;
            }
            Gizmos.DrawLine(transform.position, player.position);
        }

        // 移动方向
        if (isChasing)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
        }

        // 接地状态
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f, 0.1f);

        // 远程发射点（紫色）
        if (firePoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(firePoint.position, 0.15f);
            Gizmos.DrawRay(firePoint.position, firePoint.forward * 1f);
        }
    }

    // 玩家手动赋值方法（可以在其他脚本中调用）
    public void SetPlayer(GameObject playerObject)
    {
        if (playerObject != null)
        {
            player = playerObject.transform;
            UnityEngine.Debug.Log($"手动设置玩家: {player.name}");
        }
    }

    public void SetPlayer(Transform playerTransform)
    {
        if (playerTransform != null)
        {
            player = playerTransform;
            UnityEngine.Debug.Log($"手动设置玩家: {player.name}");
        }
    }

    // 编辑器辅助方法
#if UNITY_EDITOR
    void Reset()
    {
        // 当组件第一次添加到GameObject时调用
        UnityEngine.Debug.Log($"正在为 {name} 设置远程AI组件...");

        // 尝试自动获取CharacterController
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            UnityEngine.Debug.LogWarning($"请手动为 {name} 添加CharacterController组件");
        }

        // 查找模型子物体
        FindEnemyModel();

        // 远程默认参数初始化
        stoppingDistance = 8f;
        detectionRange = 15f;
        attackCooldown = 2f;
        projectileSpeed = 15f;
    }
#endif
}

// 子弹伤害组件（需挂载到子弹预制体，或由AI自动添加）
[RequireComponent(typeof(Collider))]
public class ProjectileDamage : MonoBehaviour
{
    public float damage = 10f;

    void OnCollisionEnter(Collision collision)
    {
        // 检测是否命中玩家
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                Debug.Log($"子弹命中玩家，造成 {damage} 点伤害");
            }
        }

        // 命中后销毁子弹（无论是否命中玩家）
        Destroy(gameObject);
    }
}