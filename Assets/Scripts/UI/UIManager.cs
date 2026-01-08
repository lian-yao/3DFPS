using UnityEngine;

public class UIManager : MonoBehaviour
{
    // 拖入你的“游戏开始界面”Panel（比如包含开始、退出、介绍按钮的那个界面）
    public GameObject gameStartPanel;

    // 自定义触发按键（默认设为ESC键，你可以在Inspector里改成任意键）
    public KeyCode toggleKey = KeyCode.Escape;

    // 游戏启动时初始化（可选：比如默认显示开始界面）
    void Start()
    {
        // 如果想默认显示，设为true；想默认隐藏，设为false
        gameStartPanel.SetActive(true);
    }

    // 每一帧检测按键
    void Update()
    {
        // 检测按键按下（只触发一次，避免长按一直切换）
        if (Input.GetKeyDown(toggleKey))
        {
            // 核心逻辑：切换界面状态（显示变隐藏，隐藏变显示）
            gameStartPanel.SetActive(!gameStartPanel.activeSelf);
        }
    }

    // 保留按钮触发的方法（如果需要按钮配合）
    // 比如点击“开始游戏”隐藏界面
    public void HideStartPanel()
    {
        gameStartPanel.SetActive(false);
    }

    // 比如想通过按钮重新显示开始界面
    public void ShowStartPanel()
    {
        gameStartPanel.SetActive(true);
    }
}