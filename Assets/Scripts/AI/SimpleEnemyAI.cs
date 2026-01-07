using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleEnemyAI : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 10f; // 【修改】提高旋转速度，确保快速朝向玩家
    public float stoppingDistance = 2f;

    [Header("重力设置")]
    public float gravity = 9.81f;
    public float groundedGravity = -2f;

    [Header("检测设置")]
    public float detectionRange = 10f;
    public float checkInterval = 0.1f; // 【修改】缩短检测间隔，状态切换更灵敏

    [Header("攻击设置")]
    public float attackDamage = 10f;
    public float attackCooldown = 1f;
    public float maxAttackHeight = 4f;

    [Header("调试")]
    public bool showGizmos = true;
    public bool debugLog = true; // 新增调试日志开关

    [Header("动画配置")]
    [SerializeField] private Animator enemyAnimator;
    public string paramIsRunning = "IsRunning";
    public string paramAttackTrigger = "AttackTrigger";
    //public string paramIsAttacking = "IsAttacking";

    [Header("组件引用")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform enemyModel;

    // 私有变量
    private Transform player;
    private float nextCheckTime;
    private float attackTimer;
    private Vector3 targetPosition;
    private bool isChasing = false;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isAttacking = false;
    private bool hasAttackTriggered = false; // 【新增】标记攻击触发状态

    void Start()
    {
        FindPlayer();
        GetComponentReferences();
        GetAnimatorReference();
        velocity = Vector3.zero;
        ValidateComponents();

        if (debugLog) Debug.Log($"{name} AI初始化完成");
    }

    void GetAnimatorReference()
    {
        if (enemyAnimator == null)
        {
            if (enemyModel != null)
            {
                enemyAnimator = enemyModel.GetComponent<Animator>();
            }
            if (enemyAnimator == null)
            {
                enemyAnimator = GetComponent<Animator>();
            }
        }

        if (enemyAnimator == null)
        {
            Debug.LogWarning($"{name}: 未找到Animator组件！");
        }
        else
        {
            // 【新增】校验Animator参数是否存在
            ValidateAnimatorParams();
        }
    }

    // 【新增】校验Animator参数，避免参数名错误
    void ValidateAnimatorParams()
    {
        foreach (AnimatorControllerParameter param in enemyAnimator.parameters)
        {
            if (param.name == paramIsRunning && param.type != AnimatorControllerParameterType.Bool)
            {
                Debug.LogError($"{name}: {paramIsRunning} 参数类型不是布尔型！");
            }
            if (param.name == paramAttackTrigger && param.type != AnimatorControllerParameterType.Trigger)
            {
                Debug.LogError($"{name}: {paramAttackTrigger} 参数类型不是触发型！");
            }
        }
    }

    void GetComponentReferences()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (enemyModel == null)
        {
            FindEnemyModel();
        }

        if (characterController == null)
        {
            Debug.LogError($"{name}: 未找到CharacterController组件！");
        }
    }

    void FindEnemyModel()
    {
        foreach (Transform child in transform)
        {
            if (child.GetComponent<Renderer>() != null)
            {
                enemyModel = child;
                if (debugLog) Debug.Log($"找到模型子物体: {enemyModel.name}");
                break;
            }
        }
    }

    void ValidateComponents()
    {
        if (characterController == null)
        {
            Debug.LogWarning($"{name}: 缺少CharacterController，移动功能不可用！");
        }

        if (enemyAnimator == null)
        {
            Debug.LogWarning($"{name}: 缺少Animator，动画功能不可用！");
        }

        if (player == null)
        {
            Debug.LogError($"{name}: 未找到玩家！");
            enabled = false;
        }
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null) playerObj = GameObject.Find("Player");
        if (playerObj == null) playerObj = FindPlayerByComponents();
        if (playerObj == null) playerObj = FindPlayerByName();

        if (playerObj != null)
        {
            player = playerObj.transform;
            if (debugLog) Debug.Log($"找到玩家: {player.name}");
        }
        else
        {
            Debug.LogError("找不到玩家！请确保玩家有'Player'标签");
        }
    }

    GameObject FindPlayerByComponents()
    {
        GameObject[] allGameObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allGameObjects)
        {
            if (obj.GetComponent<CharacterController>() != null && obj.GetComponent<Camera>() == null)
            {
                return obj;
            }
            if (obj.GetComponent<Rigidbody>() != null && !obj.GetComponent<Rigidbody>().isKinematic && obj.name.ToLower().Contains("player"))
            {
                return obj;
            }
        }
        return null;
    }

    GameObject FindPlayerByName()
    {
        GameObject[] allGameObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allGameObjects)
        {
            string objName = obj.name.ToLower();
            if ((objName.Contains("player") || objName.Contains("主角") || objName.Contains("角色")) && obj.GetComponent<Canvas>() == null && obj.GetComponent<Camera>() == null)
            {
                return obj;
            }
        }
        return null;
    }

    void Update()
    {
        if (player == null || characterController == null) return;

        CheckGrounded();
        ApplyGravity();

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

        // 【修改】攻击中仅阻塞移动/检测，不阻塞动画更新
        if (isAttacking)
        {
            // 攻击中强制停止移动，确保动画能触发
            characterController.Move(Vector3.zero);
            SetAnimatorBool(paramIsRunning, false);
            return;
        }

        // AI行为逻辑
        if (isChasing)
        {
            ChasePlayer();
            SetAnimatorBool(paramIsRunning, true);
        }
        else
        {
            // 【修复】玩家超出范围时，强制切换为闲置
            SetAnimatorBool(paramIsRunning, false);
            hasAttackTriggered = false; // 重置攻击标记
        }
    }

    void CheckGrounded()
    {
        isGrounded = characterController != null && characterController.isGrounded;
    }

    void ApplyGravity()
    {
        if (characterController == null) return;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = groundedGravity;
        }
        else
        {
            velocity.y -= gravity * Time.deltaTime;
        }
    }

    void CheckForPlayer()
    {
        if (player == null) return;

        // 重新计算玩家水平距离
        Vector3 playerPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        float horizontalDistance = Vector3.Distance(transform.position, playerPos);

        if (horizontalDistance <= detectionRange)
        {
            if (!isChasing && debugLog)
            {
                Debug.Log($"{name} 开始追踪玩家，距离: {horizontalDistance}");
            }
            isChasing = true;
            targetPosition = player.position;
        }
        else
        {
            if (isChasing && debugLog)
            {
                Debug.Log($"{name} 失去玩家视野，距离: {horizontalDistance}");
            }
            isChasing = false;
        }
    }

    void ChasePlayer()
    {
        if (player == null) return;

        // 【修复】每帧更新目标位置，确保追踪最新位置
        targetPosition = new Vector3(player.position.x, transform.position.y, player.position.z);
        Vector3 direction = (targetPosition - transform.position).normalized;
        float horizontalDistance = Vector3.Distance(transform.position, targetPosition);

        // 距离大于停止距离 → 移动+旋转
        if (horizontalDistance > stoppingDistance)
        {
            // 强制朝向玩家（修复追踪失效）
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // 移动逻辑
            Vector3 moveVector = direction * moveSpeed * Time.deltaTime;
            moveVector.y = velocity.y * Time.deltaTime;
            characterController.Move(moveVector);

            hasAttackTriggered = false; // 离开攻击范围，重置攻击标记
        }
        else
        {
            // 进入攻击范围
            float verticalDistance = Mathf.Abs(player.position.y - transform.position.y);
            if (verticalDistance <= maxAttackHeight && !hasAttackTriggered)
            {
                TryAttack();
            }
            else
            {
                // 高度不符，继续移动
                SetAnimatorBool(paramIsRunning, true);
                hasAttackTriggered = false;
            }
        }
    }

    // 【重构】攻击逻辑：先触发动画，再标记攻击状态
    void TryAttack()
    {
        if (attackTimer > 0 || isAttacking) return;

        if (debugLog) Debug.Log($"{name} 触发攻击动画");

        // 1. 先触发攻击动画（关键：先设置参数，再标记攻击状态）
        SetAnimatorTrigger(paramAttackTrigger);
        SetAnimatorBool(paramIsRunning, false);

        // 2. 标记攻击状态，防止重复触发
        isAttacking = true;
        hasAttackTriggered = true;

        // 3. 重置冷却
        attackTimer = attackCooldown;
    }

    // 动画事件回调：攻击动画结束时调用
    public void OnAttackAnimationEnd()
    {
        if (player == null) return;
        // 【新增】二次校验：玩家是否仍在攻击范围内
        bool isPlayerInRange = CheckIfPlayerInAttackRange();
        if (!isPlayerInRange)
        {
            if (debugLog) Debug.Log($"{name} 攻击动画结束，但玩家已离开攻击范围，不扣血");
            isAttacking = false; // 重置攻击状态
            enemyAnimator.ResetTrigger(paramAttackTrigger);
            return; // 不在范围，直接退出，不扣血
        }

        // 结算伤害
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
            if (debugLog) Debug.Log($"{name} 攻击动画结束，造成 {attackDamage} 点伤害");
        }

        // 【修复】正确重置攻击状态，恢复移动能力
        isAttacking = false;
        enemyAnimator.ResetTrigger(paramAttackTrigger);

        // 攻击后重新检测玩家位置，确保继续追踪
        CheckForPlayer();
    }
    // 【新增】封装“玩家是否在攻击范围”的校验方法
    private bool CheckIfPlayerInAttackRange()
    {
        if (player == null) return false;

        // 1. 计算水平距离（忽略Y轴）
        Vector3 playerHorizontalPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        float horizontalDistance = Vector3.Distance(transform.position, playerHorizontalPos);

        // 2. 计算垂直高度差
        float verticalDistance = Mathf.Abs(player.position.y - transform.position.y);

        // 3. 同时满足水平范围（stoppingDistance）和高度范围（maxAttackHeight）
        return horizontalDistance <= stoppingDistance && verticalDistance <= maxAttackHeight;
    }

    // 安全设置Animator参数
    void SetAnimatorBool(string paramName, bool value)
    {
        if (enemyAnimator != null && enemyAnimator.isActiveAndEnabled)
        {
            enemyAnimator.SetBool(paramName, value);
            if (debugLog && paramName == paramIsRunning)
            {
                Debug.Log($"{name} 设置{paramName} = {value}");
            }
        }
    }

    void SetAnimatorTrigger(string paramName)
    {
        if (enemyAnimator != null && enemyAnimator.isActiveAndEnabled)
        {
            enemyAnimator.SetTrigger(paramName);
            if (debugLog) Debug.Log($"{name} 触发{paramName}");
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isAttacking) return;

        if (hit.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = hit.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null && attackTimer <= 0)
            {
                TryAttack();
            }
        }
    }

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
        if (isChasing && !isAttacking)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
        }

        // 接地状态
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f, 0.1f);

        // 攻击状态
        Gizmos.color = isAttacking ? Color.magenta : Color.clear;
        Gizmos.DrawSphere(transform.position + Vector3.up * 1f, 0.2f);
    }

    public void SetPlayer(GameObject playerObject)
    {
        if (playerObject != null)
        {
            player = playerObject.transform;
            if (debugLog) Debug.Log($"手动设置玩家: {player.name}");
        }
    }

    public void SetPlayer(Transform playerTransform)
    {
        if (playerTransform != null)
        {
            player = playerTransform;
            if (debugLog) Debug.Log($"手动设置玩家: {player.name}");
        }
    }

#if UNITY_EDITOR
    void Reset()
    {
        Debug.Log($"正在为 {name} 设置SimpleEnemyAI组件...");
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogWarning($"请手动为 {name} 添加CharacterController组件");
        }
        FindEnemyModel();
        if (enemyModel != null)
        {
            enemyAnimator = enemyModel.GetComponent<Animator>();
        }
        if (enemyAnimator == null)
        {
            enemyAnimator = GetComponent<Animator>();
        }
    }
#endif
}