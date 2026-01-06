using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SimpleMovementSound : MonoBehaviour
{
    [Header("移动声音")]
    public AudioClip walkSound;
    public AudioClip runSound;
    public AudioClip crouchSound;

    [Header("设置")]
    [Range(0, 1)] public float walkVolume = 0.3f;
    [Range(0, 1)] public float runVolume = 0.5f;
    [Range(0, 1)] public float crouchVolume = 0.2f;
    public float minMoveSpeed = 0.1f; // 最小移动速度阈值

    private AudioSource audioSource;
    private FPSMovement movementController;
    private MovementState currentState;
    private MovementState lastState;

    void Start()
    {
        movementController = GetComponent<FPSMovement>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        ConfigureAudioSource();

        // === 添加这行调试代码 ===
        Debug.Log($"音频文件检查 - 走路:{(walkSound != null ? "有" : "无")}, 跑步:{(runSound != null ? "有" : "无")}");
    }

    void ConfigureAudioSource()
    {
        audioSource.spatialBlend = 1f; // 3D音效
        audioSource.playOnAwake = false;
        audioSource.loop = true; // 设置为循环播放
    }

    void Update()
    {
        if (movementController == null) return;

        // 获取当前移动状态
        currentState = movementController.GetMovementState();
        bool isMoving = movementController.IsMoving();

        // 如果状态变化，切换声音
        if (currentState != lastState ||
            (isMoving && !audioSource.isPlaying) ||
            (!isMoving && audioSource.isPlaying))
        {
            UpdateMovementSound();
        }

        lastState = currentState;
    }

    void UpdateMovementSound()
    {
        bool isMoving = movementController.IsMoving();

        if (!isMoving || movementController.GetCurrentHeight() < 0.1f)
        {
            // 停止所有声音
            audioSource.Stop();
            return;
        }

        // 根据状态设置音频
        switch (currentState)
        {
            case MovementState.Walking:
                SetAudioClip(walkSound, walkVolume);
                break;

            case MovementState.Running:
                SetAudioClip(runSound, runVolume);
                break;

            case MovementState.Crouching:
                SetAudioClip(crouchSound, crouchVolume);
                break;

            case MovementState.Idle:
                audioSource.Stop();
                break;
        }
    }

    void SetAudioClip(AudioClip clip, float volume)
    {
        if (clip == null) return;

        if (audioSource.clip != clip)
        {
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.Play();
        }
        else if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }
}
