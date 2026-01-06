using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleEnemyAI : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;
    public float stoppingDistance = 2f;

    [Header("重力设置")]
    public float gravity = 9.81f;
    public float groundedGravity = -2f; // 小的向下力确保站在地面上

    [Header("检测设置")]
    public float detectionRange = 10f;
    public float checkInterval = 0.3f;

    [Header("攻击设置")]
    public float attackDamage = 10f;
    public float attackCooldown = 1f;
    public float maxAttackHeight = 4f;

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

        UnityEngine.Debug.Log($"{name} AI初始化完成");
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

        // 简单距离检测
        if (horizontalDistance <= detectionRange)
        {
            if (!isChasing)
            {
                // 开始追踪
                targetPosition = player.position;
                UnityEngine.Debug.Log($"{name} 开始追踪玩家");
            }
            isChasing = true;
        }
        else
        {
            if (isChasing)
            {
                UnityEngine.Debug.Log($"{name} 失去玩家视野");
            }
            isChasing = false;
        }
    }

    void ChasePlayer()
    {
        if (player == null) return;
        if (characterController == null) return;

        // 更新目标位置
        targetPosition = player.position;

        // 计算水平距离（忽略Y轴）
        Vector3 horizontalDirection = targetPosition - transform.position;
        horizontalDirection.y = 0;
        float horizontalDistance = horizontalDirection.magnitude;

        // 如果水平距离大于停止距离，继续移动
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

                // 旋转面向移动方向
                if (direction.magnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);

                    // 如果模型需要旋转修正，在这里处理
                    if (enemyModel != null)
                    {
                        // 旋转父物体（AI控制器）
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                                                             rotationSpeed * Time.deltaTime);
                        // 模型子物体保持自己的localRotation（在编辑器中设置）
                    }
                    else
                    {
                        // 没有单独模型子物体，直接旋转自己
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                                                             rotationSpeed * Time.deltaTime);
                    }
                }
            }
        }
        else
        {
            // 在攻击范围内
            float verticalDistance = Mathf.Abs(targetPosition.y - transform.position.y);
            //float maxAttackHeight = 4f;

            if (verticalDistance <= maxAttackHeight)
            {
                TryAttack();
            }
        }
    }

    void TryAttack()
    {
        // 检查攻击冷却
        if (attackTimer > 0) return;

        // 攻击玩家
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
            UnityEngine.Debug.Log($"{name} 攻击玩家，造成 {attackDamage} 点伤害");
        }

        // 重置攻击冷却
        attackTimer = attackCooldown;
    }

    // 碰撞攻击
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = hit.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null && attackTimer <= 0)
            {
                playerHealth.TakeDamage(attackDamage);
                attackTimer = attackCooldown;
            }
        }
    }

    // 在编辑器中绘制调试信息
    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // 检测范围
        Gizmos.color = isChasing ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 停止距离
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);

        // 到玩家的线
        if (player != null)
        {
            Gizmos.color = isChasing ? Color.red : Color.gray;
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
        UnityEngine.Debug.Log($"正在为 {name} 设置SimpleEnemyAI组件...");

        // 尝试自动获取CharacterController
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            UnityEngine.Debug.LogWarning($"请手动为 {name} 添加CharacterController组件");
        }

        // 查找模型子物体
        FindEnemyModel();
    }
#endif
}