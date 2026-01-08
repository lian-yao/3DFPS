
using UnityEngine;

public class FPSAnimationController : MonoBehaviour
{
    [Header("动画控制器")]
    public Animator animator;

    [Header("射击系统引用")]
    public AutoPlayerShoot shootSystem;

    // 动画参数
    private const string SPEED_PARAM = "Speed";
    private const string IS_SHOOTING_PARAM = "IsShooting";
    private const string IS_CROUCHING_PARAM = "IsCrouching";
    private const string SHOOT_TRIGGER = "Shoot";
    private const string FIRE_SPEED_PARAM = "FireSpeed";

    // 输入状态
    private float horizontalInput;
    private float verticalInput;
    private bool isSprinting;
    private bool isCrouching;

    // 射击动画状态
    private bool isShootingAnimationActive = false;
    private float shootAnimationTimer = 0f;
    private float shootCooldownTimer = 0f;
    private float baseAnimationDuration = 0.25f;
    private bool waitingForNextShot = false;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (shootSystem == null)
        {
            shootSystem = GetComponent<AutoPlayerShoot>();
            if (shootSystem == null)
            {
                shootSystem = GetComponentInParent<AutoPlayerShoot>();
            }
        }

        CheckAnimatorParameters();
        UpdateAnimationParameters();
    }

    void Update()
    {
        if (animator == null) return;

        // 只处理移动和下蹲输入
        isCrouching = Input.GetKey(KeyCode.LeftControl);
        animator.SetBool(IS_CROUCHING_PARAM, isCrouching);

        // 更新射击动画计时器
        if (isShootingAnimationActive)
        {
            shootAnimationTimer -= Time.deltaTime;

            if (shootAnimationTimer <= 0)
            {
                isShootingAnimationActive = false;

                // 检查是否应该进入等待状态
                if (shootCooldownTimer > 0.05f) // 还有冷却时间
                {
                    waitingForNextShot = true;
                    // 保持射击状态但不再播放动画
                    animator.SetBool(IS_SHOOTING_PARAM, false);
                }
                else
                {
                    waitingForNextShot = false;
                    // 冷却也结束，完全重置射击状态
                    animator.SetBool(IS_SHOOTING_PARAM, false);
                }
            }
        }
        else if (waitingForNextShot)
        {
            // 等待下一发射击
            if (shootCooldownTimer <= 0.05f)
            {
                waitingForNextShot = false;
            }
        }

        // 更新射击冷却计时器
        if (shootCooldownTimer > 0)
        {
            shootCooldownTimer -= Time.deltaTime;
        }

        // 更新移动速度
        UpdateSpeedValue();
    }

    void CheckAnimatorParameters()
    {
        if (animator == null) return;

        bool hasFireSpeed = false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == FIRE_SPEED_PARAM && param.type == AnimatorControllerParameterType.Float)
                hasFireSpeed = true;
        }

        if (!hasFireSpeed)
            Debug.LogWarning($"Animator 缺少 Float 参数: {FIRE_SPEED_PARAM} (用于控制射击动画速度)");
    }

    void UpdateAnimationParameters()
    {
        if (shootSystem != null)
        {
            float fireRate = shootSystem.GetFireRate();
            float animDuration = shootSystem.GetShootAnimationDuration();
            baseAnimationDuration = animDuration;

            // 计算动画速度：如果射击间隔小于动画时长，需要加速动画
            if (fireRate > 0)
            {
                float animationSpeed = 1.0f;

                if (fireRate < animDuration)
                {
                    // 快速射击：射击间隔小于动画时长，需要加速动画
                    animationSpeed = animDuration / fireRate;
                    animationSpeed = Mathf.Clamp(animationSpeed, 1.0f, 3.0f);
                    Debug.Log($"快速射击模式: 射击间隔({fireRate:F2}s) < 动画时长({animDuration:F2}s)");
                    Debug.Log($"动画速度设置为: {animationSpeed:F2}x");
                }
                else
                {
                    // 慢速射击：正常播放动画
                    animationSpeed = 1.0f;
                    Debug.Log($"慢速射击模式: 射击间隔({fireRate:F2}s) >= 动画时长({animDuration:F2}s)");
                }

                animator.SetFloat(FIRE_SPEED_PARAM, animationSpeed);
            }
        }
    }

    public void SetShootSystem(AutoPlayerShoot system)
    {
        shootSystem = system;
        UpdateAnimationParameters();
    }

    public void SetShootAnimationSpeed(float speed)
    {
        if (animator != null)
        {
            speed = Mathf.Clamp(speed, 0.5f, 3.0f);
            animator.SetFloat(FIRE_SPEED_PARAM, speed);
        }
    }

    // 由AutoPlayerShoot调用的方法 - 关键！
    public void TriggerShootAnimation()
    {
        if (animator == null) return;

        float fireRate = shootSystem != null ? shootSystem.GetFireRate() : 1.0f;

        // 重置等待状态
        waitingForNextShot = false;

        // 触发单次射击动画
        animator.SetTrigger(SHOOT_TRIGGER);

        // 设置射击动画状态
        isShootingAnimationActive = true;

        // 根据射击间隔确定动画播放时长
        if (fireRate < baseAnimationDuration)
        {
            // 快速射击：动画被加速，所以实际播放时间等于射击间隔
            shootAnimationTimer = fireRate * 0.9f; // 稍微提前一点结束，为下一发准备
            Debug.Log($"快速射击: 动画时长={shootAnimationTimer:F2}s (加速播放)");
        }
        else
        {
            // 慢速射击：正常播放完整动画
            shootAnimationTimer = baseAnimationDuration;
            Debug.Log($"慢速射击: 动画时长={shootAnimationTimer:F2}s (正常速度)");
        }

        // 重置冷却计时器
        shootCooldownTimer = fireRate;

        // 设置射击状态
        animator.SetBool(IS_SHOOTING_PARAM, true);
    }

    public void SetShootingState(bool shooting)
    {
        if (animator != null)
        {
            if (shooting)
            {
                animator.SetBool(IS_SHOOTING_PARAM, true);
                isShootingAnimationActive = true;
            }
            else
            {
                animator.SetBool(IS_SHOOTING_PARAM, false);
                isShootingAnimationActive = false;
                waitingForNextShot = false;
                shootAnimationTimer = 0f;
            }
        }
    }

    void UpdateSpeedValue()
    {
        bool isMoving = (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f);
        float speedValue = 0f;

        if (isMoving)
        {
            if (isShootingAnimationActive || waitingForNextShot)
            {
                // 射击或等待射击时：强制为行走速度（0.5）
                speedValue = 0.5f;
            }
            else if (isSprinting && verticalInput > 0 && !isCrouching)
            {
                // 向前奔跑：不在射击状态且没按ctrl
                speedValue = 1.0f;
            }
            else
            {
                // 正常行走或按ctrl移动
                speedValue = 0.5f;
            }

            // 按ctrl时速度减半
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

        animator.SetFloat(SPEED_PARAM, speedValue);
    }

    public void SetMovementInput(float horizontal, float vertical, bool sprinting)
    {
        horizontalInput = horizontal;
        verticalInput = vertical;
        isSprinting = sprinting;
    }

    public bool IsShooting()
    {
        return isShootingAnimationActive || waitingForNextShot;
    }

    public bool IsCrouching()
    {
        return isCrouching;
    }

    public bool IsShootingAnimationActive()
    {
        return isShootingAnimationActive;
    }

    public bool IsWaitingForNextShot()
    {
        return waitingForNextShot;
    }

    public float GetShootCooldown()
    {
        return shootCooldownTimer;
    }

    void OnGUI()
    {
        if (animator == null) return;

        float speed = animator.GetFloat(SPEED_PARAM);
        float fireSpeed = animator.GetFloat(FIRE_SPEED_PARAM);
        string state;

        if (speed < 0.1f)
            state = (isShootingAnimationActive || waitingForNextShot) ? "站立射击" : (isCrouching ? "蹲伏静止" : "待机");
        else if (speed < 0.3f)
            state = isCrouching ? "蹲伏移动" : ((isShootingAnimationActive || waitingForNextShot) ? "行走射击" : "行走");
        else if (speed < 0.7f)
            state = (isShootingAnimationActive || waitingForNextShot) ? "行走射击" : "行走";
        else
            state = "奔跑";

        GUI.Label(new Rect(10, 10, 300, 20), $"移动速度: {speed:F2}");
        GUI.Label(new Rect(10, 30, 300, 20), $"射击动画速度: {fireSpeed:F2}x");
        GUI.Label(new Rect(10, 50, 300, 20), $"动画播放中: {isShootingAnimationActive}");
        GUI.Label(new Rect(10, 70, 300, 20), $"等待下一发: {waitingForNextShot}");
        GUI.Label(new Rect(10, 90, 300, 20), $"动画剩余: {shootAnimationTimer:F2}s");
        GUI.Label(new Rect(10, 110, 300, 20), $"射击冷却: {shootCooldownTimer:F2}s");
        GUI.Label(new Rect(10, 130, 300, 20), $"状态: {state}");

        if (shootSystem != null)
        {
            float fireRate = shootSystem.GetFireRate();
            GUI.Label(new Rect(10, 150, 300, 20), $"武器射速: {fireRate:F2}s/发");
        }
    }
}