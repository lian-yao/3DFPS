using UnityEngine;

/// <summary>
/// 敌人音效实现：支持受击、死亡、追击、多攻击类型音效，低耦合可扩展
/// </summary>
public class EnemySound : MonoBehaviour, IEnemySound
{
    [Header("通用音效设置")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool loopChaseSound = true;
    [SerializeField] private float chaseSoundVolume = 0.5f;
    [SerializeField] private float oneShotVolume = 0.7f;

    [Header("受击音效")]
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private float hurtVolume = 0.7f;

    [Header("死亡音效")]
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private float deathVolume = 0.8f;

    [Header("追击音效")]
    [SerializeField] private AudioClip chaseClip;
    [SerializeField] private bool loopChase = true;

    [Header("攻击音效")]
    [SerializeField] private AudioClip attackClip0; // 攻击类型0的音效
    [SerializeField] private AudioClip attackClip1; // 攻击类型1的音效
    [SerializeField] private AudioClip attackClip2; // 攻击类型2的音效
    [SerializeField] private AudioClip attackClip3; // 攻击类型3的音效
    [SerializeField] private float attackVolume = 0.6f;

    private AudioClip currentChaseClip;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D音效
    }

    public void PlayHurtSound()
    {
        if (hurtClip != null)
        {
            audioSource.PlayOneShot(hurtClip, hurtVolume);
            Debug.Log($"{name} 播放受击音效");
        }
        else
        {
            Debug.LogWarning($"{name} 未配置受击音效");
        }
    }

    public void PlayDeathSound()
    {
        if (deathClip != null)
        {
            audioSource.PlayOneShot(deathClip, deathVolume);
            Debug.Log($"{name} 播放死亡音效");
        }
        else
        {
            Debug.LogWarning($"{name} 未配置死亡音效");
        }
    }

    public void PlayAttackSound(int attackType = 0)
    {
        AudioClip clip = GetAttackClip(attackType);
        if (clip != null)
        {
            audioSource.PlayOneShot(clip, attackVolume);
            Debug.Log($"{name} 播放攻击音效，攻击类型: {attackType}");
        }
        else
        {
            Debug.LogWarning($"{name} 未配置攻击类型 {attackType} 的音效");
        }
    }

    private AudioClip GetAttackClip(int attackType)
    {
        return attackType switch
        {
            0 => attackClip0,
            1 => attackClip1,
            2 => attackClip2,
            3 => attackClip3,
            _ => attackClip0
        };
    }

    public void PlayChaseSound()
    {
        if (chaseClip != null)
        {
            if (loopChase)
            {
                if (!audioSource.isPlaying || audioSource.clip != chaseClip)
                {
                    audioSource.clip = chaseClip;
                    audioSource.loop = true;
                    audioSource.volume = chaseSoundVolume;
                    audioSource.Play();
                }
            }
            else
            {
                audioSource.PlayOneShot(chaseClip, chaseSoundVolume);
            }
            Debug.Log($"{name} 播放追击音效");
        }
        else
        {
            Debug.LogWarning($"{name} 未配置追击音效");
        }
    }

    public void StopAllSounds()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        audioSource.clip = null;
        Debug.Log($"{name} 停止所有音效");
    }
}
