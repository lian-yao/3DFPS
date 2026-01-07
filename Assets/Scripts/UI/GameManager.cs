using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("UI面板")]
    public GameObject gameOverPanel;   // 拖入失败面板
    public GameObject gameWinPanel;    // 拖入通关面板

    [Header("游戏对象")]
    public PlayerHealth playerHealth;  // 拖入玩家的生命值脚本
    public List<EnemyHealth> allEnemies = new List<EnemyHealth>(); // 拖入所有野怪的EnemyHealth脚本

    [Header("音效设置")]
    public AudioClip gameOverSound; // 拖入游戏失败音效文件
    public AudioClip gameWinSound;  // 拖入通关音效文件
    [Range(0f, 1f)] // 限制音量范围在0-1之间，方便在Inspector面板调节
    public float gameOverVolume = 0.8f; // 失败音效音量（默认0.8）
    [Range(0f, 1f)]
    public float gameWinVolume = 0.8f;   // 通关音效音量（默认0.8）
    private AudioSource audioSource; // 音频播放组件

    private bool isGameEnded = false;

    void Start()
    {
        // 强制恢复游戏时间（核心：避免初始就暂停）
        Time.timeScale = 1;

        // 初始化音频组件（自动添加，避免手动遗漏）
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false; // 禁止场景启动时自动播放音效

        // 初始化鼠标（新场景启动时锁定鼠标）
        LockCursor();

        // 防护：清空旧监听，避免重复绑定
        UnsubscribeAllEvents();

        // 监听玩家死亡事件（增加日志，方便排查）
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDead += ShowGameOver;
            Debug.Log($"[{gameObject.name}] 已绑定玩家死亡事件");
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] PlayerHealth未赋值！请检查新场景的GameManager配置");
        }

        // ========== 核心修改1：AllEnemies列表为空时不输出Error ==========
        if (allEnemies.Count > 0)
        {
            foreach (var enemy in allEnemies)
            {
                if (enemy != null)
                {
                    enemy.OnEnemyDead += CheckAllEnemiesDead;
                    Debug.Log($"[{gameObject.name}] 已绑定敌人{enemy.name}的死亡事件");
                }
                else
                {
                    // 保留空引用的Error（因为是空值错误，需要提示）
                    Debug.LogError($"[{gameObject.name}] AllEnemies列表中有空引用！");
                }
            }
        }
        // 列表为空时：完全不输出任何日志（去掉原来的Error）

        // 初始化面板（确保面板默认隐藏）
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (gameWinPanel != null) gameWinPanel.SetActive(false);
    }

    // 玩家死亡：显示失败界面 + 播放失败音效（新增音量控制）
    public void ShowGameOver()
    {
        if (isGameEnded) return;
        isGameEnded = true;

        // 防护：面板为空时提示
        if (gameOverPanel == null)
        {
            Debug.LogError($"[{gameObject.name}] GameOverPanel未赋值！");
            return;
        }

        gameOverPanel.SetActive(true);
        // 仅在玩家主动死亡时暂停（其他情况不暂停）
        Time.timeScale = 0;
        UnlockCursor();
        Debug.Log($"[{gameObject.name}] 显示失败界面，鼠标已解锁");

        // 播放游戏失败音效（带音量控制 + 空引用防护）
        if (audioSource != null && gameOverSound != null)
        {
            audioSource.PlayOneShot(gameOverSound, gameOverVolume); // 第二个参数是音量
            Debug.Log($"[{gameObject.name}] 播放游戏失败音效，音量：{gameOverVolume}");
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] 未设置游戏失败音效！");
        }
    }

    // ========== 核心修改2：检查敌人死亡时增加容错，避免空列表触发异常 ==========
    void CheckAllEnemiesDead()
    {
        if (isGameEnded || allEnemies.Count == 0) return; // 列表为空时直接返回，不执行后续逻辑

        bool allDead = true;
        foreach (var enemy in allEnemies)
        {
            if (enemy != null && !enemy.isDead)
            {
                allDead = false;
                break;
            }
        }

        if (allDead)
        {
            ShowGameWin();
        }
    }

    // 通关：显示胜利界面 + 播放通关音效（新增音量控制）
    public void ShowGameWin()
    {
        if (isGameEnded) return;
        isGameEnded = true;

        // 防护：面板为空时提示
        if (gameWinPanel == null)
        {
            Debug.LogError($"[{gameObject.name}] GameWinPanel未赋值！");
            return;
        }

        gameWinPanel.SetActive(true);
        // 仅在通关时暂停（其他情况不暂停）
        Time.timeScale = 0;
        UnlockCursor();
        Debug.Log($"[{gameObject.name}] 显示通关界面，鼠标已解锁");

        // 播放游戏通关音效（带音量控制 + 空引用防护）
        if (audioSource != null && gameWinSound != null)
        {
            audioSource.PlayOneShot(gameWinSound, gameWinVolume); // 第二个参数是音量
            Debug.Log($"[{gameObject.name}] 播放游戏通关音效，音量：{gameWinVolume}");
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] 未设置游戏通关音效！");
        }
    }

    // 按钮逻辑：重新开始（强制恢复时间）
    public void RestartGame()
    {
        Time.timeScale = 1; // 强制恢复游戏时间
        LockCursor();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Debug.Log($"[{gameObject.name}] 重新开始当前场景");
    }

    // 按钮逻辑：下一关（强制恢复时间）
    public void NextLevel()
    {
        Time.timeScale = 1; // 强制恢复游戏时间
        LockCursor();

        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
            Debug.Log($"[{gameObject.name}] 加载下一关：{nextSceneIndex}");
        }
        else
        {
            Debug.Log("已到最后一关！");
            QuitGame();
        }
    }

    // 按钮逻辑：结束游戏（强制恢复时间）
    public void QuitGame()
    {
        Time.timeScale = 1; // 强制恢复游戏时间
        Debug.Log($"[{gameObject.name}] 退出游戏");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // 封装解锁鼠标
    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 封装锁定鼠标
    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // 防护：取消所有事件监听（避免内存泄漏）
    void UnsubscribeAllEvents()
    {
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDead -= ShowGameOver;
        }
        foreach (var enemy in allEnemies)
        {
            if (enemy != null)
            {
                enemy.OnEnemyDead -= CheckAllEnemiesDead;
            }
        }
    }

    // 场景销毁时取消监听
    void OnDestroy()
    {
        UnsubscribeAllEvents();
    }
}