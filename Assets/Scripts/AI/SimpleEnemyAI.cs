using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人AI核心逻辑：基于状态模式实现，支持多攻击方式扩展、音效扩展，低耦合且符合开闭原则
/// </summary>
public class SimpleEnemyAI : MonoBehaviour
{
    [Header("移动配置")]
    public float moveSpeed = 3f; // 移动速度
    public float rotationSpeed = 10f; // 旋转速度
    public float stoppingDistance = 2f; // 追击停止距离（进入攻击范围）

    [Header("重力配置")]
    public float gravity = 9.81f; // 重力值
    public float groundedGravity = -2f; // 地面重力（防止下陷）

    [Header("感知配置")]
    public float detectionRange = 10f; // 玩家检测范围
    public float checkInterval = 0.1f; // 检测间隔（优化性能）

    [Header("扩展配置")]
    [SerializeField] private List<IAttackBehaviour> attackBehaviours; // 多攻击方式集合
    [SerializeField] private int defaultAttackType = 0; // 默认攻击类型索引
    [SerializeField] private IEnemySound enemySound;    // 音效组件（依赖注入）
    public bool showGizmos = true; // 是否显示Gizmos辅助线
    public bool debugLog = true; // 是否输出调试日志

    [Header("动画参数")]
    [SerializeField] private Animator enemyAnimator; // 敌人动画器
    public string paramIsRunning = "IsRunning"; // 奔跑动画布尔参数名
    [SerializeField] private List<AttackTriggerParam> attackTriggerParamsList; // 编辑器可配置的攻击动画触发参数列表
    private Dictionary<int, string> attackTriggerParams; // 运行时使用的多攻击动画触发参数（类型-参数名）
    public string paramIsAttacking = "IsAttacking"; // 攻击状态布尔参数名

    // 可序列化的攻击动画触发参数类，用于在Unity编辑器中配置
    [System.Serializable]
    public class AttackTriggerParam
    {
        public int attackType; // 攻击类型
        public string triggerName; // 动画触发参数名
    }

    [Header("组件引用")]
    [SerializeField] private CharacterController characterController; // 角色控制器（移动）
    [SerializeField] private Transform enemyModel; // 敌人模型（用于动画/渲染）

    // 状态机核心变量
    private BaseEnemyAIState currentState; // 当前AI状态
    private Dictionary<EnemyAIState, BaseEnemyAIState> stateDictionary = new(); // 状态字典（管理所有状态）

    // 核心运行变量
    private Transform player; // 玩家Transform引用
    private float nextCheckTime; // 下次检测玩家的时间
    private Vector3 velocity; // 移动速度向量（含重力）
    private bool isGrounded; // 是否在地面
    private IHealth playerHealth; // 玩家健康系统
    private IAttackBehaviour currentAttackBehaviour; // 当前使用的攻击方式
    private float lastAttackEndTime; // 上次攻击结束的时间
    private float attackEndDelay = 0.5f; // 攻击结束后，延迟多久再尝试新攻击

    void Awake()
    {
        // 自动注入音效组件（未手动赋值时）
        enemySound ??= GetComponent<IEnemySound>();

        // 初始化攻击行为（加载多攻击方式）
        InitAttackBehaviours();

        // 初始化状态机（注册所有AI状态）
        InitStateMachine();
    }

    void Start()
    {
        // 初始化流程：找玩家、获取组件、验证组件、设置初始状态
        FindPlayer();
        GetComponentReferences();
        GetAnimatorReference();
        ValidateComponents();

        velocity = Vector3.zero;
        SwitchState(EnemyAIState.Idle); // 默认初始状态为闲置

        if (debugLog) Debug.Log($"{name} AI初始化完成");
    }

    void Update()
    {
        // 核心组件为空时直接返回，避免空引用
        if (player == null || characterController == null || currentState == null) return;

        // 每帧检测地面、应用重力、执行当前状态逻辑
        CheckGrounded();
        ApplyGravity();
        currentState.UpdateState(); // 状态驱动的Update逻辑

        // 定时检测玩家（优化性能，避免每帧检测）
        if (Time.time >= nextCheckTime)
        {
            CheckForPlayer();
            nextCheckTime = Time.time + checkInterval;
        }
    }

    #region 初始化/状态机管理
    private void InitAttackBehaviours()
    {
        // 自动获取挂载的所有攻击行为组件
        attackBehaviours ??= new List<IAttackBehaviour>(GetComponents<IAttackBehaviour>());

        if (debugLog) Debug.Log($"{name} 找到 {attackBehaviours.Count} 个攻击行为组件");

        // 设置默认攻击方式（索引合法时）
        if (attackBehaviours.Count > 0 && defaultAttackType >= 0 && defaultAttackType < attackBehaviours.Count)
        {
            currentAttackBehaviour = attackBehaviours[defaultAttackType];
            if (debugLog) Debug.Log($"{name} 设置默认攻击行为: 类型 {currentAttackBehaviour.AttackType}, 范围 {currentAttackBehaviour.AttackRange}");
        }
        else
        {
            Debug.LogWarning($"{name} 未配置有效攻击行为");
        }

        // 初始化攻击动画参数字典
        attackTriggerParams = new Dictionary<int, string>();
        
        // 1. 首先加载编辑器中配置的参数
        if (attackTriggerParamsList != null)
        {
            foreach (var param in attackTriggerParamsList)
            {
                if (!attackTriggerParams.ContainsKey(param.attackType))
                {
                    attackTriggerParams.Add(param.attackType, param.triggerName);
                    if (debugLog) Debug.Log($"{name} 加载编辑器配置的攻击动画参数: 类型 {param.attackType}, 参数名 {param.triggerName}");
                }
            }
        }
        
        // 2. 自动补全未配置的攻击触发参数
        foreach (var attack in attackBehaviours)
        {
            if (!attackTriggerParams.ContainsKey(attack.AttackType))
            {
                // 使用攻击类型作为键，默认触发参数名为AttackTrigger
                attackTriggerParams.Add(attack.AttackType, "AttackTrigger");
                if (debugLog) Debug.Log($"{name} 自动补全攻击动画参数: 类型 {attack.AttackType}, 默认参数名 AttackTrigger");
            }
        }
    }

    private void InitStateMachine()
    {
        // 注册AI状态（开闭原则：新增状态只需在此添加）
        stateDictionary.Add(EnemyAIState.Idle, new IdleState(this, EnemyAIState.Idle));
        stateDictionary.Add(EnemyAIState.Chasing, new ChasingState(this, EnemyAIState.Chasing));
        stateDictionary.Add(EnemyAIState.Attacking, new AttackingState(this, EnemyAIState.Attacking));
    }

    /// <summary>切换AI状态（执行状态退出/进入逻辑）</summary>
    /// <param name="newStateType">目标状态类型</param>
    public void SwitchState(EnemyAIState newStateType)
    {
        if (stateDictionary.TryGetValue(newStateType, out var newState))
        {
            // 先退出当前状态，再进入新状态
            if (currentState != null) currentState.ExitState();
            currentState = newState;
            currentState.EnterState();

            if (debugLog) Debug.Log($"{name} 切换状态: {newStateType}");
        }
    }
    #endregion

    #region 核心逻辑（感知/移动/攻击）
    /// <summary>检测玩家是否在感知范围内，切换对应状态</summary>
    private void CheckForPlayer()
    {
        // 只检测水平距离（忽略Y轴高度差）
        Vector3 playerHorizontalPos = new(player.position.x, transform.position.y, player.position.z);
        float distance = Vector3.Distance(transform.position, playerHorizontalPos);

        // 玩家在范围内：切换到追击/保持攻击状态
        if (distance <= detectionRange)
        {
            if (currentState.StateType != EnemyAIState.Chasing && currentState.StateType != EnemyAIState.Attacking)
            {
                SwitchState(EnemyAIState.Chasing);
                if (enemySound != null) enemySound.PlayChaseSound(); // 播放追击音效
            }
        }
        else
        {
            // 玩家超出范围：切换回闲置状态
            if (currentState.StateType != EnemyAIState.Idle)
            {
                SwitchState(EnemyAIState.Idle);
                if (enemySound != null) enemySound.StopAllSounds();
            }
        }
    }

    /// <summary>追击玩家核心逻辑（移动+旋转）</summary>
    public void ChasePlayer()
    {
        // 目标位置（仅水平方向）
        Vector3 targetPos = new(player.position.x, transform.position.y, player.position.z);
        Vector3 direction = (targetPos - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPos);

        // 距离大于停止距离：继续移动/旋转
        if (distance > stoppingDistance)
        {
            // 平滑旋转朝向玩家
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);

            // 计算移动向量（含重力）并移动
            Vector3 moveVector = direction * moveSpeed * Time.deltaTime;
            moveVector.y = velocity.y * Time.deltaTime;
            characterController.Move(moveVector);

            // 播放奔跑动画
            SetAnimatorBool(paramIsRunning, true);
        }
        else
        {
            // 进入攻击范围：尝试攻击
            TryAttack();
        }
    }
    public void RandomizeAttackType()//随机切换攻击类型的方法
{
    if (attackBehaviours.Count == 0) return;
    
    int randomIndex = Random.Range(0, attackBehaviours.Count);
    currentAttackBehaviour = attackBehaviours[randomIndex];
    
    if (debugLog) Debug.Log($"{name} 随机切换攻击类型: {currentAttackBehaviour.AttackType}");
}

    /// <summary>尝试执行攻击（检测攻击条件+切换攻击状态）</summary>
    public void TryAttack()
    {
        // 攻击组件/玩家健康系统为空，或玩家已死亡时直接返回
        if (currentAttackBehaviour == null || playerHealth == null || playerHealth.IsDead)
        {
            if (debugLog)
            {
                if (currentAttackBehaviour == null) Debug.Log($"{name} 攻击行为为空");
                if (playerHealth == null) Debug.Log($"{name} 玩家健康系统为空");
                if (playerHealth != null && playerHealth.IsDead) Debug.Log($"{name} 玩家已死亡");
            }
            return;
        }
        
        // 如果已经在攻击状态，直接返回，防止重复攻击
        if (currentState != null && currentState.StateType == EnemyAIState.Attacking)
        {
            if (debugLog) Debug.Log($"{name} 已在攻击状态，跳过攻击尝试");
            return;
        }
        
        // 检查攻击结束后延迟，防止攻击结束后立即触发新攻击
        if (Time.time - lastAttackEndTime < attackEndDelay)
        {
            if (debugLog) Debug.Log($"{name} 攻击结束后延迟，跳过攻击尝试");
            return;
        }

        // 满足攻击条件时切换到攻击状态
        if (currentAttackBehaviour.CanAttack(transform, player))
        {
            if (debugLog) Debug.Log($"{name} 满足攻击条件，切换到攻击状态");
            
            // 先设置攻击动画参数，再切换状态
            SetAnimatorBool(paramIsRunning, false); // 停止奔跑动画
            
            // 触发对应攻击类型的动画
            if (attackTriggerParams.TryGetValue(currentAttackBehaviour.AttackType, out string trigger))
            {
                SetAnimatorTrigger(trigger);
                if (debugLog) Debug.Log($"{name} 触发攻击动画: {trigger}");
            }
            
            // 切换到攻击状态
            SwitchState(EnemyAIState.Attacking);
            
            // 播放攻击音效
            if (enemySound != null) enemySound.PlayAttackSound(currentAttackBehaviour.AttackType);
        }
        else
        {
            if (debugLog)
            {
                Vector3 targetPos = new(player.position.x, transform.position.y, player.position.z);
                float distance = Vector3.Distance(transform.position, targetPos);
                float heightDiff = Mathf.Abs(player.position.y - transform.position.y);
                float cooldown = currentAttackBehaviour.Cooldown;
                Debug.Log($"{name} 不满足攻击条件: 距离={distance:F2}, 攻击范围={currentAttackBehaviour.AttackRange}, 高度差={heightDiff:F2}, 最大允许高度={currentAttackBehaviour.MaxAttackHeight}, 冷却时间={cooldown:F2}");
            }
        }
    }

    /// <summary>攻击命中回调（由Animator事件调用，用于伤害结算）</summary>
    public void OnAttackHit()
    {
        Debug.Log($"{name} OnAttackHit 被调用，当前状态: {currentState?.StateType}");
        
        // 玩家存活时执行伤害结算
        if (playerHealth != null && !playerHealth.IsDead)
        {
            if (currentAttackBehaviour != null)
            {
                // 再次检查攻击条件，确保伤害结算时攻击条件仍然满足
                if (currentAttackBehaviour.CanAttack(transform, player))
                {
                    Debug.Log($"{name} 执行攻击伤害，攻击类型: {currentAttackBehaviour.AttackType}");
                    currentAttackBehaviour.ExecuteAttack(transform, playerHealth);
                    
                    // 播放攻击音效
                    enemySound?.PlayAttackSound();
                }
                else
                {
                    Debug.Log($"{name} 伤害结算时攻击条件不满足，跳过伤害");
                }
            }
        }
        else
        {
            Debug.Log($"{name} 玩家已死亡或无玩家健康系统，跳过伤害");
        }
    }
    
    /// <summary>攻击动画结束回调（由Animator事件调用，用于动画结束处理）</summary>
    public void OnAttackAnimationEnd()
    {
        Debug.Log($"{name} OnAttackAnimationEnd 被调用，当前状态: {currentState?.StateType}");
        
        // 执行攻击行为的动画结束逻辑
        if (currentAttackBehaviour != null) currentAttackBehaviour.OnAttackAnimationEnd();
        
        // 随机切换攻击类型
        RandomizeAttackType();
        
        // 记录攻击结束时间
        lastAttackEndTime = Time.time;
        
        // 攻击结束后，切换到追击状态
        Debug.Log($"{name} 准备切换到追击状态");
        SwitchState(EnemyAIState.Chasing);
        Debug.Log($"{name} 切换后状态: {currentState?.StateType}");
    }
    #endregion

    #region 辅助方法（重力/动画/组件）
    /// <summary>检测是否在地面（基于CharacterController）</summary>
    private void CheckGrounded()
    {
        isGrounded = characterController != null ? characterController.isGrounded : false;
    }

    /// <summary>应用重力（区分地面/空中重力）</summary>
    private void ApplyGravity()
    {
        if (characterController == null) return;

        // 地面时用低重力防止下陷，空中时正常重力
        velocity.y = isGrounded && velocity.y < 0 ? groundedGravity : velocity.y - gravity * Time.deltaTime;
    }

    /// <summary>设置动画器布尔参数</summary>
    /// <param name="paramName">参数名</param>
    /// <param name="value">参数值</param>
    public void SetAnimatorBool(string paramName, bool value)
    {
        if (enemyAnimator != null && enemyAnimator.isActiveAndEnabled)
        {
            enemyAnimator.SetBool(paramName, value);
        }
    }

    /// <summary>触发动画器触发参数</summary>
    /// <param name="paramName">参数名</param>
    public void SetAnimatorTrigger(string paramName)
    {
        if (enemyAnimator != null && enemyAnimator.isActiveAndEnabled)
        {
            enemyAnimator.SetTrigger(paramName);
        }
    }

    /// <summary>查找玩家（优先Tag，其次名称）</summary>
    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<IHealth>(); // 解耦玩家健康系统
        }
        else
        {
            Debug.LogError("未找到玩家（需给玩家添加Player标签）");
            enabled = false; // 禁用AI避免报错
        }
    }

    /// <summary>获取组件引用（自动补全未手动赋值的组件）</summary>
    private void GetComponentReferences()
    {
        characterController ??= GetComponent<CharacterController>();
        enemyModel ??= FindEnemyModel();
    }

    /// <summary>查找敌人模型（找带Renderer的子物体）</summary>
    /// <returns>敌人模型Transform</returns>
    private Transform FindEnemyModel()
    {
        foreach (Transform child in transform)
        {
            if (child.GetComponent<Renderer>() != null) return child;
        }
        return null;
    }

    /// <summary>获取动画器引用（优先模型，其次自身）</summary>
    private void GetAnimatorReference()
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
    }

    /// <summary>验证核心组件是否存在，输出警告</summary>
    private void ValidateComponents()
    {
        if (characterController == null) Debug.LogWarning($"{name} 缺少CharacterController");
        if (enemyAnimator == null) Debug.LogWarning($"{name} 缺少Animator");
        if (attackBehaviours.Count == 0) Debug.LogWarning($"{name} 未配置攻击行为");
    }
    #endregion

    #region Gizmos/公共扩展方法
    /// <summary>编辑器Gizmos：绘制感知范围、停止距离、攻击范围</summary>
    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // 感知范围（追击时红色，否则黄色）
        Gizmos.color = (currentState != null && currentState.StateType == EnemyAIState.Chasing) ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 停止距离（绿色）
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);

        // 攻击范围（青色）
        if (currentAttackBehaviour != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, currentAttackBehaviour.AttackRange);
        }
    }

    /// <summary>切换攻击方式（外部可调用）</summary>
    /// <param name="attackType">攻击类型标识</param>
    public void SwitchAttackType(int attackType)
    {
        foreach (var attack in attackBehaviours)
        {
            if (attack.AttackType == attackType)
            {
                currentAttackBehaviour = attack;
                return;
            }
        }
        Debug.LogWarning($"无效攻击类型:{attackType}");
    }
    #endregion

#if UNITY_EDITOR
    /// <summary>编辑器重置方法（自动赋值组件）</summary>
    void Reset()
    {
        characterController = GetComponent<CharacterController>();
        enemyModel = FindEnemyModel();
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

    // 基础状态抽象类（所有AI状态的基类）
    public abstract class BaseEnemyAIState
    {
        protected SimpleEnemyAI ai; // 所属AI核心脚本
        protected EnemyAIState stateType; // 状态类型

        /// <summary>获取当前状态类型</summary>
        public EnemyAIState StateType => stateType;

        /// <summary>状态构造函数</summary>
        /// <param name="ai">所属AI脚本</param>
        /// <param name="stateType">状态类型</param>
        public BaseEnemyAIState(SimpleEnemyAI ai, EnemyAIState stateType)
        {
            this.ai = ai;
            this.stateType = stateType;
        }

        /// <summary>进入状态时执行（仅一次）</summary>
        public abstract void EnterState();
        /// <summary>状态持续期间每帧执行</summary>
        public abstract void UpdateState();
        /// <summary>退出状态时执行（仅一次）</summary>
        public abstract void ExitState();
    }

    // 空闲状态实现
    public class IdleState : BaseEnemyAIState
    {
        /// <summary>空闲状态构造函数</summary>
        public IdleState(SimpleEnemyAI ai, EnemyAIState stateType) : base(ai, stateType)
        { }

        /// <summary>进入空闲状态：停止奔跑动画</summary>
        public override void EnterState()
        {
            ai.SetAnimatorBool(ai.paramIsRunning, false);
        }

        /// <summary>空闲状态每帧逻辑：可扩展随机巡逻/警戒</summary>
        public override void UpdateState()
        {
            // 空闲状态下可以添加一些随机巡逻或警戒行为
        }

        /// <summary>退出空闲状态：清理临时变量</summary>
        public override void ExitState()
        {
            // 空闲状态退出时的清理工作
        }
    }

    // 追逐状态实现
    public class ChasingState : BaseEnemyAIState
    {
        /// <summary>追逐状态构造函数</summary>
        public ChasingState(SimpleEnemyAI ai, EnemyAIState stateType) : base(ai, stateType)
        { }

        /// <summary>进入追逐状态：播放奔跑动画</summary>
        public override void EnterState()
        {
            ai.SetAnimatorBool(ai.paramIsRunning, true);
        }

        /// <summary>追逐状态每帧逻辑：执行追击玩家</summary>
        public override void UpdateState()
        {
            ai.ChasePlayer();
        }

        /// <summary>退出追逐状态：清理追击相关逻辑</summary>
        public override void ExitState()
        {
            // 追逐状态退出时的清理工作
        }
    }

    // 攻击状态实现
    public class AttackingState : BaseEnemyAIState
    {
        /// <summary>攻击状态构造函数</summary>
        public AttackingState(SimpleEnemyAI ai, EnemyAIState stateType) : base(ai, stateType)
        { }

        /// <summary>进入攻击状态：标记攻击中</summary>
        public override void EnterState()
        {
            if (ai.debugLog) Debug.Log($"{ai.name} 进入攻击状态");
            ai.SetAnimatorBool(ai.paramIsAttacking, true);
            ai.SetAnimatorBool(ai.paramIsRunning, false); // 确保停止奔跑动画
        }

        /// <summary>攻击状态每帧逻辑：禁用移动，保持攻击姿态</summary>
        public override void UpdateState()
        {
            // 在攻击状态下，禁用移动
            ai.GetComponent<CharacterController>().Move(Vector3.zero);
        }

        /// <summary>退出攻击状态：取消攻击标记</summary>
        public override void ExitState()
        {
            ai.SetAnimatorBool(ai.paramIsAttacking, false);
        }
    }
}