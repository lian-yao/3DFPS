using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSAnimationController : MonoBehaviour
{
    public Animator animator;
    private CharacterController characterController;

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (animator == null) return;

        // 获取输入
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector2 input = new Vector2(horizontal, vertical);

        // 计算速度值（0=idle, 0.5=walk, 1=run）
        float speed = 0f;

        if (input.magnitude > 0.1f) // 有输入
        {
            if (Input.GetKey(KeyCode.LeftShift) && vertical > 0)
            {
                speed = 1.0f; // 向前跑步
            }
            else
            {
                speed = 0.5f; // 走路
            }
        }
        else
        {
            speed = 0f; // 静止
        }

        // 只用一个参数控制！
        animator.SetFloat("Speed", speed);

        // 如果需要跳跃
        if (characterController != null && characterController.isGrounded)
        {
            if (Input.GetButtonDown("Jump"))
            {
                animator.SetTrigger("Jump");
            }
        }

        // 调试显示
        //OnGUI();
    }

    void OnGUI()
    {
        if (animator == null) return;

        GUI.Label(new Rect(10, 10, 300, 20), $"速度值: {animator.GetFloat("Speed"):F2}");

        string state = "unknown";
        float speed = animator.GetFloat("Speed");
        if (speed < 0.1f) state = "idle";
        else if (speed < 0.7f) state = "walking";
        else state = "running";

        GUI.Label(new Rect(10, 30, 300, 20), $"当前状态: {state}");
    }
}
