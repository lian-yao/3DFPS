
using UnityEngine;

public class FPSAnimationController : MonoBehaviour
{
    public Animator animator;

    // 动画参数 - 需要与 Animator 窗口中的参数名完全一致！
    private const string SPEED_PARAM = "Speed";               // Float 类型
    private const string IS_SHOOTING_PARAM = "IsShooting";    // Bool 类型
    private const string IS_CROUCHING_PARAM = "IsCrouching";  // Bool 类型（新增）

    // 输入状态缓存
    private float horizontalInput;
    private float verticalInput;
    private bool isSprinting;
    private bool isShooting;
    private bool isCrouching;  // 新增：判断是否按ctrl

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        // 确保 Animator 中有这些参数
        CheckAnimatorParameters();
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

        // 新增：判断是否按左Ctrl键
        isCrouching = Input.GetKey(KeyCode.LeftControl);

        // 设置下蹲参数到 Animator
        animator.SetBool(IS_CROUCHING_PARAM, isCrouching);

        // 更新速度值
        UpdateSpeedValue();
    }

    // 检查 Animator 参数是否齐全
    void CheckAnimatorParameters()
    {
        if (animator == null) return;

        bool hasSpeed = false;
        bool hasShooting = false;
        bool hasCrouching = false;

        // 检查所有参数
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == SPEED_PARAM && param.type == AnimatorControllerParameterType.Float)
                hasSpeed = true;
            else if (param.name == IS_SHOOTING_PARAM && param.type == AnimatorControllerParameterType.Bool)
                hasShooting = true;
            else if (param.name == IS_CROUCHING_PARAM && param.type == AnimatorControllerParameterType.Bool)
                hasCrouching = true;
        }

        // 输出警告
        if (!hasSpeed)
            Debug.LogWarning($"Animator 缺少 Float 参数: {SPEED_PARAM}");
        if (!hasShooting)
            Debug.LogWarning($"Animator 缺少 Bool 参数: {IS_SHOOTING_PARAM}");
        if (!hasCrouching)
            Debug.LogWarning($"Animator 缺少 Bool 参数: {IS_CROUCHING_PARAM}");
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
            else if (isSprinting && verticalInput > 0 && !isCrouching)  // 新增：按ctrl时不能奔跑
            {
                // 向前奔跑：不在射击状态且没按ctrl
                speedValue = 1.0f;
            }
            else
            {
                // 正常行走或按ctrl移动
                speedValue = 0.5f;
            }

            // 新增：按ctrl时速度减半
            if (isCrouching)
            {
                speedValue *= 0.5f;
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

    // 新增：获取是否按ctrl
    public bool IsCrouching()
    {
        return isCrouching;
    }

    // 调试信息
    void OnGUI()
    {
        if (animator == null) return;

        float speed = animator.GetFloat(SPEED_PARAM);
        string state;

        if (speed < 0.1f)
            state = isShooting ? "站立射击" : (isCrouching ? "蹲伏静止" : "待机");
        else if (speed < 0.3f)
            state = isCrouching ? "蹲伏移动" : (isShooting ? "行走射击" : "行走");
        else if (speed < 0.7f)
            state = isShooting ? "行走射击" : "行走";
        else
            state = "奔跑";

        GUI.Label(new Rect(10, 10, 300, 20), $"速度值: {speed:F2}");
        GUI.Label(new Rect(10, 30, 300, 20), $"射击中: {isShooting}");
        GUI.Label(new Rect(10, 50, 300, 20), $"下蹲中: {isCrouching}");
        GUI.Label(new Rect(10, 70, 300, 20), $"状态: {state}");
    }

    // 在 Scene 视图中显示状态
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2.5f,
                $"Speed: {animator?.GetFloat(SPEED_PARAM):F2}\n" +
                $"射击: {isShooting}\n" +
                $"下蹲: {isCrouching}"
            );
        }
    }
#endif
}