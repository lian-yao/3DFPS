using UnityEngine;

public class FPSAnimationController : MonoBehaviour
{
    public Animator animator;

    // 核心：只需要这两个参数！
    private const string SPEED_PARAM = "Speed";
    private const string IS_SHOOTING_PARAM = "IsShooting";

    // 输入状态缓存
    private float horizontalInput;
    private float verticalInput;
    private bool isSprinting;
    private bool isShooting;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (animator == null) return;

        // 获取射击输入
        bool shootingInput = Input.GetMouseButton(0);
        if (shootingInput != isShooting)
        {
            isShooting = shootingInput;
            animator.SetBool(IS_SHOOTING_PARAM, isShooting);
        }

        // 更新速度值
        UpdateSpeedValue();
    }

    // 外部调用：设置移动输入
    public void SetMovementInput(float horizontal, float vertical, bool sprinting)
    {
        horizontalInput = horizontal;
        verticalInput = vertical;
        isSprinting = sprinting;
    }

    void UpdateSpeedValue()
    {
        // 计算是否在移动
        bool isMoving = (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f);

        // === 核心逻辑：计算速度值 ===
        // 0 = 静止，0.5 = 行走，1 = 奔跑
        float speedValue = 0f;

        if (isMoving)
        {
            if (isShooting)
            {
                // 射击时：强制为行走速度（0.5）
                speedValue = 0.5f;
            }
            else if (isSprinting && verticalInput > 0)
            {
                // 向前奔跑：不在射击状态
                speedValue = 1.0f;
            }
            else
            {
                // 正常行走
                speedValue = 0.5f;
            }
        }
        else
        {
            // 静止
            speedValue = 0f;
        }

        // 设置到Animator
        animator.SetFloat(SPEED_PARAM, speedValue);
    }

    // 公共方法：获取射击状态
    public bool IsShooting()
    {
        return isShooting;
    }

    // 调试信息
    void OnGUI()
    {
        if (animator == null) return;

        float speed = animator.GetFloat(SPEED_PARAM);
        string state;

        if (speed < 0.1f) state = isShooting ? "站立射击" : "待机";
        else if (speed < 0.7f) state = isShooting ? "行走射击" : "行走";
        else state = "奔跑";

        GUI.Label(new Rect(10, 10, 300, 20), $"速度值: {speed:F2}");
        GUI.Label(new Rect(10, 30, 300, 20), $"射击中: {isShooting}");
        GUI.Label(new Rect(10, 50, 300, 20), $"状态: {state}");
    }
}