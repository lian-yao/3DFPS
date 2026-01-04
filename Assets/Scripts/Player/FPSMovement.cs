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

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float currentSpeed;

    // 下蹲相关变量
    private bool isCrouching = false;
    private bool wantsToStand = false;
    private float currentControllerHeight;
    private Vector3 cameraOriginalPosition;

    // 射击状态变量（新增）
    private bool isShooting = false;

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
    }

    void Update()
    {
        // ====== 更新射击状态（新增）======
        isShooting = Input.GetMouseButton(0);

        // ====== 视角控制 ======
        HandleMouseLook();

        // ====== 下蹲处理 ======
        HandleCrouching();

        // ====== 移动控制 ======
        HandleMovement();

        // ====== 跳跃和重力 ======
        HandleJumpAndGravity();
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

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // === 修改点：按下鼠标左键时强制使用走路速度 ===
        if (isShooting) // 使用射击状态变量
        {
            currentSpeed = walkSpeed;
        }
        else
        {
            // 正常的移动速度判断
            if (isCrouching)
            {
                currentSpeed = crouchSpeed;
            }
            else if (Input.GetKey(KeyCode.LeftShift))
            {
                currentSpeed = runSpeed;
            }
            else
            {
                currentSpeed = walkSpeed;
            }
        }

        Vector3 move = transform.right * x + transform.forward * z;

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

    // === 新增：公开方法获取射击状态 ===
    public bool IsShooting()
    {
        return isShooting;
    }

    // === 新增：获取当前移动模式 ===
    public MovementState GetMovementState()
    {
        if (isCrouching)
        {
            return MovementState.Crouching;
        }
        else if (isShooting)
        {
            return MovementState.Walking; // 射击时强制为走路
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            return MovementState.Running;
        }
        else
        {
            return MovementState.Walking;
        }
    }

    // === 新增：判断是否正在跑步 ===
    public bool IsRunning()
    {
        return !isShooting && Input.GetKey(KeyCode.LeftShift) && !isCrouching;
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
        return Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;
    }
}

// === 新增：移动状态枚举 ===
public enum MovementState
{
    Idle,
    Walking,
    Running,
    Crouching
}