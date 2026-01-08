/// <summary>
/// 敌人音效接口（预留声音扩展）
/// </summary>
public interface IEnemySound
{
    void PlayHurtSound();       // 受伤音效
    void PlayDeathSound();      // 死亡音效
    void PlayAttackSound(int attackType = 0); // 攻击音效（支持多攻击类型）
    void PlayChaseSound();      // 追击音效
    void StopAllSounds();       // 停止所有音效
}