using UnityEngine;

// 注意：脚本名必须和类名一致（PortalTeleport），否则会编译报错
public class PortalTeleport : MonoBehaviour
{
    private GameManager gameManager;

    void Start()
    {
        // 找到场景中的GameManager（添加空引用日志，方便排查）
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("场景中未找到GameManager对象！请检查是否添加了GameManager");
        }
    }

    // 玩家触碰传送门时触发（触发碰撞必须保证Collider勾选Is Trigger）
    void OnTriggerEnter(Collider other)
    {
        // 双重防护：确认碰撞对象是玩家，且GameManager存在
        if (other != null && other.CompareTag("Player") && gameManager != null)
        {
            Debug.Log("玩家触碰传送门，准备显示通关界面");
            // 调用GameManager的ShowGameWin方法（必须保证该方法是public）
            gameManager.ShowGameWin();
        }
        else if (gameManager == null)
        {
            Debug.LogError("GameManager为空，无法显示通关界面！");
        }
        else if (!other.CompareTag("Player"))
        {
            Debug.Log($"碰撞对象不是玩家，标签是：{other.tag}");
        }
    }
}