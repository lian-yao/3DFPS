using UnityEngine;

/// <summary>
/// 敌人攻击状态实现
/// </summary>
public class AttackingState : BaseEnemyAIState
{
    public AttackingState(SimpleEnemyAI ai, EnemyAIState stateType) : base(ai, stateType)
    {}

    public override void EnterState()
    {
        ai.SetAnimatorBool(ai.paramIsAttacking, true);
    }

    public override void UpdateState()
    {
        // 在攻击状态下，禁用移动
        ai.GetComponent<CharacterController>().Move(Vector3.zero);
    }

    public override void ExitState()
    {
        ai.SetAnimatorBool(ai.paramIsAttacking, false);
    }
}