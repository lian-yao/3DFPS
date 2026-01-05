using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("UI面板")]
    public GameObject gameOverPanel;   // 拖入失败面板
    public GameObject gameWinPanel;    // 拖入通关面板

    [Header("游戏对象")]
    public PlayerHealth playerHealth;  // 拖入玩家的生命值脚本
    public List<EnemyHealth> allEnemies = new List<EnemyHealth>(); // 拖入所有野怪的EnemyHealth脚本

    private bool isGameEnded = false;

    void Start()
    {
        // 监听玩家死亡事件
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDead += ShowGameOver;
        }
        // 监听所有野怪死亡事件
        foreach (var enemy in allEnemies)
        {
            enemy.OnEnemyDead += CheckAllEnemiesDead;
        }
    }

    // 玩家死亡：显示失败界面（新增解锁鼠标）
    void ShowGameOver()
    {
        if (isGameEnded) return;
        isGameEnded = true;
        gameOverPanel.SetActive(true);
        Time.timeScale = 0; // 暂停游戏

        // 关键修改1：失败界面解锁鼠标，确保按钮可点击
        UnlockCursor();
    }

    // 检查是否所有野怪都死亡
    void CheckAllEnemiesDead()
    {
        if (isGameEnded) return;
        bool allDead = true;
        foreach (var enemy in allEnemies)
        {
            if (!enemy.isDead)
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

    // 通关：显示胜利界面（核心修复：解锁鼠标）
    void ShowGameWin()
    {
        isGameEnded = true;
        gameWinPanel.SetActive(true);
        Time.timeScale = 0; // 暂停游戏

        // 关键修改2：通关界面必须解锁鼠标，否则按钮点不了
        UnlockCursor();
    }

    // 封装解锁鼠标的方法（复用逻辑）
    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None; // 解锁鼠标锁定
        Cursor.visible = true; // 显示鼠标指针
    }

    // 按钮逻辑：重新开始（加载当前场景）
    public void RestartGame()
    {
        Time.timeScale = 1; // 恢复游戏时间
        LockCursor(); // 重新锁定鼠标（可选，回到游戏状态）
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    // 按钮逻辑：下一关（加载下一个场景）
    public void NextLevel()
    {
        Time.timeScale = 1; // 恢复游戏时间
        LockCursor(); // 重新锁定鼠标
        int nextSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneIndex);
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
        Application.Quit();
        // 编辑器中退出Play模式
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // 封装锁定鼠标的方法（游戏运行时用）
    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked; // 锁定鼠标到屏幕中心
        Cursor.visible = false; // 隐藏鼠标指针
    }
}