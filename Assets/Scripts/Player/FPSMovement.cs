using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSMovement : MonoBehaviour
{
    [Header("移动设置")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float jumpForce = 5f;
    public float gravity = -9.81f;

    [Header("下蹲设置")]
    public float crouchSpeed = 2.5f;           // 下蹲移动速度
    public float crouchHeight = 1f;           // 下蹲时高度
    public float standingHeight = 2f;         // 站立时高度
    public float crouchTransitionSpeed = 10f; // 下蹲过渡速度
    public KeyCode crouchKey = KeyCode.LeftControl;     // 下蹲按键
    [Tooltip("启用后按一次切换，禁用需要按住")]
    public bool toggleCrouch = false;         // 切换模式

    [Header("视角设置")]
    public float mouseSensitivity = 100f;
    public Transform playerCamera;
    public float xRotation = 0f;
    [Header("摄像机下蹲偏移")]
    public float cameraCrouchOffset = -0.5f;  // 下蹲时摄像机下降高度

    [Header("动画控制")]
    public FPSAnimationController animationController; // 新增：引用动画控制器

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float currentSpeed;

    // 下蹲相关变量
    private bool isCrouching = false;
    private bool wantsToStand = false;
    private float currentControllerHeight;
    private Vector3 cameraOriginalPosition;

    // 移动输入
    private float horizontalInput;
    private float verticalInput;
    private bool isSprinting;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        // 初始化高度
        currentControllerHeight = standingHeight;
        controller.height = currentControllerHeight;
        controller.center = new Vector3(0, currentControllerHeight * 0.5f, 0);

        // 记录摄像机原始位置
        if (playerCamera != null)
        {
            cameraOriginalPosition = playerCamera.localPosition;
        }

        // 如果没有指定动画控制器，尝试自动获取
        if (animationController == null)
        {
            animationController = GetComponent<FPSAnimationController>();
        }
    }

    void Update()
    {
        // ====== 获取输入 ======
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        isSprinting = Input.GetKey(KeyCode.LeftShift);

        // ====== 视角控制 ======
        HandleMouseLook();

        // ====== 下蹲处理 ======
        HandleCrouching();

        // ====== 移动控制 ======
        HandleMovement();

        // ====== 跳跃和重力 ======
        HandleJumpAndGravity();

        // ====== 更新动画（简化！）======
        if (animationController != null)
        {
            // 这里调用的是正确的SetMovementInput方法
            animationController.SetMovementInput(horizontalInput, verticalInput, isSprinting);
        }
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleCrouching()
    {
        // 处理下蹲输入
        if (toggleCrouch)
        {
            // 切换模式
            if (Input.GetKeyDown(crouchKey))
            {
                isCrouching = !isCrouching;
                if (!isCrouching)
                {
                    wantsToStand = true;
                }
            }
        }
        else
        {
            // 按住模式
            bool wasCrouching = isCrouching;
            isCrouching = Input.GetKey(crouchKey);

            // 如果松开下蹲键，尝试站起
            if (wasCrouching && !isCrouching)
            {
                wantsToStand = true;
            }
        }

        // 检测头顶是否有障碍物（防止站起时卡住）
        if (wantsToStand && !isCrouching)
        {
            float checkDistance = standingHeight - crouchHeight;
            Vector3 rayStart = transform.position + Vector3.up * crouchHeight;

            // 向上发射射线检测障碍物
            if (Physics.Raycast(rayStart, Vector3.up, checkDistance + 0.1f))
            {
                // 头顶有障碍物，保持蹲下
                isCrouching = true;
                wantsToStand = false;
            }
        }

        // 平滑调整控制器高度
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        currentControllerHeight = Mathf.Lerp(
            currentControllerHeight,
            targetHeight,
            Time.deltaTime * crouchTransitionSpeed
        );

        // 更新CharacterController的高度和中心点
        float heightDifference = controller.height - currentControllerHeight;
        controller.height = currentControllerHeight;
        controller.center = new Vector3(0, currentControllerHeight * 0.5f, 0);

        // 平滑调整摄像机高度（模拟头部下降）
        if (playerCamera != null)
        {
            float targetCameraY = cameraOriginalPosition.y;
            if (isCrouching)
            {
                targetCameraY = cameraOriginalPosition.y + cameraCrouchOffset;
            }

            Vector3 cameraPos = playerCamera.localPosition;
            cameraPos.y = Mathf.Lerp(cameraPos.y, targetCameraY, Time.deltaTime * crouchTransitionSpeed);
            playerCamera.localPosition = cameraPos;
        }

        // 重置站起标志
        if (!isCrouching && Mathf.Abs(currentControllerHeight - standingHeight) < 0.01f)
        {
            wantsToStand = false;
        }
    }

    void HandleMovement()
    {
        isGrounded = controller.isGrounded;

        // 获取射击状态
        bool isShooting = false;
        if (animationController != null)
        {
            isShooting = animationController.IsShooting();
        }

        // === 简化速度计算 ===
        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else if (isShooting || !isSprinting) // 射击时不能奔跑
        {
            currentSpeed = walkSpeed;
        }
        else
        {
            currentSpeed = runSpeed;
        }

        Vector3 move = transform.right * horizontalInput + transform.forward * verticalInput;

        // 限制下蹲时的移动方向
        if (isCrouching)
        {
            move = Vector3.ClampMagnitude(move, 1f);
        }

        controller.Move(move * currentSpeed * Time.deltaTime);
    }

    void HandleJumpAndGravity()
    {
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // 只有在站立且不处于站起检测状态时才能跳跃
        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching && !wantsToStand)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);

            // 跳跃时自动取消下蹲
            if (isCrouching)
            {
                isCrouching = false;
                wantsToStand = true;
            }
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // 供其他脚本查询状态的方法
    public bool IsCrouching()
    {
        return isCrouching;
    }

    public bool IsTryingToStand()
    {
        return wantsToStand;
    }

    public float GetCurrentHeight()
    {
        return currentControllerHeight;
    }

    public bool IsMoving()
    {
        return horizontalInput != 0 || verticalInput != 0;
    }

    // 新增：获取移动状态
    public MovementState GetMovementState()
    {
        if (isCrouching)
        {
            return MovementState.Crouching;
        }

        bool isShooting = animationController != null && animationController.IsShooting();
        if (isShooting)
        {
            return MovementState.Walking; // 射击时强制为走路
        }
        else if (isSprinting && verticalInput > 0)
        {
            return MovementState.Running;
        }
        else if (IsMoving())
        {
            return MovementState.Walking;
        }
        else
        {
            return MovementState.Idle;
        }
    }
}

// 移动状态枚举
public enum MovementState
{
    Idle,
    Walking,
    Running,
    Crouching
}