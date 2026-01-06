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
    public PlayerHealth playerHealth;  // 拖入玩家的生命值脚本（可选，空则自动找）
    public List<EnemyHealth> allEnemies = new List<EnemyHealth>(); // 拖入所有野怪的EnemyHealth脚本（可选，空则自动找）

    [Header("音效资源")]
    public AudioClip gameOverSound; // 拖入游戏失败音效文件
    public AudioClip gameWinSound;  // 拖入游戏通关音效文件
    private AudioSource audioSource; // 音频播放组件

    private bool isGameEnded = false;

    void Start()
    {
        // ========== 1. 初始化音频组件（自动添加，避免手动遗漏） ==========
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false; // 禁止场景启动时自动播放音效

        // ========== 2. 动态查找依赖（适配预制体，无需手动拖入） ==========
        FindAllDependencies();

        // ========== 3. 初始化鼠标（新场景启动时锁定鼠标） ==========
        LockCursor();

        // ========== 4. 防护：清空旧监听，避免重复绑定 ==========
        UnsubscribeAllEvents();

        // ========== 5. 监听玩家死亡事件（增加日志，方便排查） ==========
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDead += ShowGameOver;
            Debug.Log($"[{gameObject.name}] 已绑定玩家死亡事件");
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] PlayerHealth未找到！请检查玩家标签是否为Player");
        }

        // ========== 6. 监听所有野怪死亡事件（增加日志） ==========
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
                    Debug.LogError($"[{gameObject.name}] AllEnemies列表中有空引用！");
                }
            }
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] AllEnemies列表为空！已自动查找场景内Enemy标签的敌人");
        }

        // ========== 7. 初始化面板（确保面板默认隐藏） ==========
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (gameWinPanel != null) gameWinPanel.SetActive(false);
    }

    // 核心：动态查找所有依赖对象（适配预制体，无需手动拖入）
    void FindAllDependencies()
    {
        // 查找玩家（按Player标签）
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<PlayerHealth>();
            }
        }

        // 查找所有敌人（按Enemy标签）
        if (allEnemies.Count == 0)
        {
            GameObject[] enemyObjs = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (var obj in enemyObjs)
            {
                EnemyHealth eh = obj.GetComponent<EnemyHealth>();
                if (eh != null) allEnemies.Add(eh);
            }
        }

        // 查找UI面板（按名称，确保场景内面板名称为GameOverPanel/GameWinPanel）
        if (gameOverPanel == null)
        {
            gameOverPanel = GameObject.Find("GameOverPanel");
        }
        if (gameWinPanel == null)
        {
            gameWinPanel = GameObject.Find("GameWinPanel");
        }
    }

    // ========== 关键修改：改为public，让PortalTeleport能调用 ==========
    // 玩家死亡：显示失败界面 + 播放失败音效
    public void ShowGameOver()
    {
        if (isGameEnded) return;
        isGameEnded = true;

        // 防护：面板为空时提示
        if (gameOverPanel == null)
        {
            Debug.LogError($"[{gameObject.name}] GameOverPanel未找到！请检查面板名称");
            return;
        }

        gameOverPanel.SetActive(true);
        Time.timeScale = 0;
        UnlockCursor();
        Debug.Log($"[{gameObject.name}] 显示失败界面，鼠标已解锁");

        // 播放游戏失败音效（带空引用防护）
        if (audioSource != null && gameOverSound != null)
        {
            audioSource.PlayOneShot(gameOverSound); // 单次播放，不打断其他音频
            Debug.Log($"[{gameObject.name}] 播放游戏失败音效");
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] 未设置游戏失败音效！");
        }
    }

    // 检查是否所有野怪都死亡
    void CheckAllEnemiesDead()
    {
        if (isGameEnded) return;

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

    // ========== 关键修改：改为public，让PortalTeleport能调用 ==========
    // 通关：显示胜利界面 + 播放通关音效
    public void ShowGameWin()
    {
        if (isGameEnded) return;
        isGameEnded = true;

        // 防护：面板为空时提示
        if (gameWinPanel == null)
        {
            Debug.LogError($"[{gameObject.name}] GameWinPanel未找到！请检查面板名称");
            return;
        }

        gameWinPanel.SetActive(true);
        Time.timeScale = 0;
        UnlockCursor();
        Debug.Log($"[{gameObject.name}] 显示通关界面，鼠标已解锁");

        // 播放游戏通关音效（带空引用防护）
        if (audioSource != null && gameWinSound != null)
        {
            audioSource.PlayOneShot(gameWinSound);
            Debug.Log($"[{gameObject.name}] 播放游戏通关音效");
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] 未设置游戏通关音效！");
        }
    }

    // 按钮逻辑：重新开始
    public void RestartGame()
    {
        Time.timeScale = 1;
        LockCursor();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Debug.Log($"[{gameObject.name}] 重新开始当前场景");
    }

    // 按钮逻辑：下一关
    public void NextLevel()
    {
        Time.timeScale = 1;
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

    // 按钮逻辑：结束游戏
    public void QuitGame()
    {
        Time.timeScale = 1;
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