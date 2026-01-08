using UnityEngine;

/// <summary>
/// 敌人AI追逐状态（状态模式具体实现类）
/// 负责处理敌人进入/持续/退出「追击玩家」状态的所有逻辑
/// 继承自AI状态基类，遵循开闭原则，仅聚焦追击状态的专属逻辑
/// </summary>
public class ChasingState : BaseEnemyAIState
{
    /// <summary>
    /// 追击状态构造函数
    /// </summary>
    /// <param name="ai">所属的敌人AI核心脚本实例（依赖注入，用于调用AI的公共方法）</param>
    /// <param name="stateType">绑定当前状态对应的枚举类型（此处固定为EnemyAIState.Chasing）</param>
    public ChasingState(SimpleEnemyAI ai, EnemyAIState stateType) : base(ai, stateType)
    {
        // 调用父类构造函数完成核心依赖（AI脚本和状态类型）的初始化
    }

    /// <summary>
    /// 进入追击状态时执行的逻辑（仅执行一次）
    /// 作用：初始化追击状态的基础参数、播放追击动画、开启追击音效等
    /// </summary>
    public override void EnterState()
    {
        // 触发AI的动画控制器，将"是否奔跑"布尔参数设为true，播放敌人奔跑动画
        ai.SetAnimatorBool(ai.paramIsRunning, true);

        // 【扩展点】若有追击音效，可在此处调用：ai.enemySound?.PlayChaseSound();
    }

    /// <summary>
    /// 追击状态持续期间每帧执行的逻辑
    /// 核心：驱动敌人向玩家移动，同时检测是否进入攻击范围/丢失目标
    /// </summary>
    public override void UpdateState()
    {
        // 调用AI核心脚本的「追逐玩家」方法，处理具体的移动、旋转、距离检测逻辑
        // 该方法内部会判断与玩家的距离，若进入攻击范围则自动切换到攻击状态
        ai.ChasePlayer();
    }

    /// <summary>
    /// 退出追击状态时执行的清理逻辑（仅执行一次）
    /// 作用：重置追击状态的临时参数、停止奔跑动画、清理状态相关资源
    /// </summary>
    public override void ExitState()
    {
        // 退出追击状态时，停止奔跑动画（避免切换到闲置/攻击状态后仍播放奔跑）
        ai.SetAnimatorBool(ai.paramIsRunning, false);

        // 【扩展点】若有追击音效，可在此处停止：ai.enemySound?.StopChaseSound();

        // 若有其他临时变量（如追击速度缓存、目标标记等），可在此处重置
    }
}