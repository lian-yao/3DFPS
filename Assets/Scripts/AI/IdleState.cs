using UnityEngine;

/// <summary>
/// 敌人空闲状态实现
/// </summary>
public class IdleState : BaseEnemyAIState
{
    public IdleState(SimpleEnemyAI ai, EnemyAIState stateType) : base(ai, stateType)
    {}

    public override void EnterState()
    {
        ai.SetAnimatorBool(ai.paramIsRunning, false);
    }

    public override void UpdateState()
    {
        // 空闲状态下可以添加一些随机巡逻或警戒行为
    }

    public override void ExitState()
    {
        // 空闲状态退出时的清理工作
    }
}