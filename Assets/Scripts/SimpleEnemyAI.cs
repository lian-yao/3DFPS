using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleEnemyAI : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;
    public float stoppingDistance = 2f;

    [Header("检测设置")]
    public float detectionRange = 10f;
    public float checkInterval = 0.3f;

    [Header("攻击设置")]
    public float attackDamage = 10f;
    public float attackCooldown = 1f;

    [Header("调试")]
    public bool showGizmos = true;

    // 私有变量
    private Transform player;
    private CharacterController characterController;
    private float nextCheckTime;
    private float attackTimer;
    private Vector3 targetPosition;
    private bool isChasing = false;

    void Start()
    {
        // 1. 查找玩家（多种方式）
        FindPlayer();

        // 2. 获取或添加CharacterController（比Rigidbody更稳定）
        InitializeCharacterController();

        Debug.Log($"{name} AI初始化完成");
    }

    void FindPlayer()
    {
        // 方法1：通过标签
        GameObject playerObj = GameObject.FindWithTag("Player");

        // 方法2：通过名字
        if (playerObj == null)
        {
            playerObj = GameObject.Find("Player");
        }

        // 方法3：查找包含Player的对象
        if (playerObj == null)
        {
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.ToLower().Contains("player"))
                {
                    playerObj = obj;
                    break;
                }
            }
        }

        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log($"找到玩家: {player.name}");
        }
        else
        {
            Debug.LogError("找不到玩家！请确保玩家物体存在");
            enabled = false; // 禁用脚本
        }
    }

    void InitializeCharacterController()
    {
        characterController = GetComponent<CharacterController>();

        if (characterController == null)
        {
            // 添加CharacterController
            characterController = gameObject.AddComponent<CharacterController>();

            // 设置合适的大小
            characterController.height = 2f;
            characterController.radius = 0.5f;
            characterController.center = new Vector3(0, 1f, 0);
            characterController.stepOffset = 0.3f;

            Debug.Log("添加CharacterController组件");
        }
    }

    void Update()
    {
        if (player == null) return;

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

    void CheckForPlayer()
    {
        if (player == null) return;

        // 计算到玩家的距离
        float distance = Vector3.Distance(transform.position, player.position);

        // 简单距离检测（先不用视线检查）
        if (distance <= detectionRange)
        {
            isChasing = true;
            targetPosition = player.position;
        }
        else
        {
            isChasing = false;
        }
    }

    void ChasePlayer()
    {
        if (player == null) return;

        // 更新目标位置
        targetPosition = player.position;

        // 计算到目标的距离
        float distance = Vector3.Distance(transform.position, targetPosition);

        // 如果距离大于停止距离，继续移动
        if (distance > stoppingDistance)
        {
            // 计算移动方向
            Vector3 direction = (targetPosition - transform.position).normalized;

            // 忽略Y轴移动
            direction.y = 0;

            if (direction.magnitude > 0.1f)
            {
                // 移动
                Vector3 moveVector = direction * moveSpeed * Time.deltaTime;

                // 使用CharacterController移动
                if (characterController != null)
                {
                    characterController.Move(moveVector);
                }
                else
                {
                    // 备用：直接移动Transform
                    transform.position += moveVector;
                }

                // 旋转面向移动方向
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                                                     rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            // 在攻击范围内，尝试攻击
            TryAttack();
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
            Debug.Log($"{name} 攻击玩家，造成 {attackDamage} 点伤害");
        }

        // 重置攻击冷却
        attackTimer = attackCooldown;
    }

    // 碰撞攻击（备用）
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

    // 可视化调试
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
    }
}