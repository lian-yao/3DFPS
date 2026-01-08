/// <summary>
/// 敌人AI状态抽象基类（遵循开闭原则：新增AI状态时只需继承此类实现，无需修改原有代码）
/// </summary>
public abstract class BaseEnemyAIState
{
    /// <summary>
    /// 所属的敌人AI核心逻辑脚本引用（用于状态中调用AI的公共方法/属性）
    /// </summary>
    protected SimpleEnemyAI ai;

    /// <summary>
    /// 当前状态对应的枚举类型（标记该状态实例属于哪种AI状态）
    /// </summary>
    protected EnemyAIState stateType;

    /// <summary>
    /// 【只读属性】获取当前状态对应的枚举类型
    /// </summary>
    public EnemyAIState StateType => stateType;

    /// <summary>
    /// 初始化敌人AI状态基类的构造函数
    /// </summary>
    /// <param name="ai">敌人AI核心逻辑脚本的实例（注入依赖，让状态能访问AI的核心逻辑）</param>
    /// <param name="stateType">当前状态对应的枚举类型（如Idle/Chasing/Attacking）</param>
    public BaseEnemyAIState(SimpleEnemyAI ai, EnemyAIState stateType)
    {
        this.ai = ai;
        this.stateType = stateType;
    }

    /// <summary>
    /// 抽象方法：进入该状态时执行的逻辑（子类必须实现）
    /// 示例：初始化状态参数、播放状态动画、开启音效等
    /// </summary>
    public abstract void EnterState();

    /// <summary>
    /// 抽象方法：状态持续期间每帧执行的逻辑（子类必须实现）
    /// 示例：追击玩家、检测攻击范围、闲置时的随机移动等
    /// </summary>
    public abstract void UpdateState();

    /// <summary>
    /// 抽象方法：退出该状态时执行的逻辑（子类必须实现）
    /// 示例：重置状态参数、停止状态动画、清理临时变量等
    /// </summary>
    public abstract void ExitState();
}