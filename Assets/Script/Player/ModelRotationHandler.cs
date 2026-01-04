using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelRotationHandler : MonoBehaviour
{
    [Header("引用")]
    public Camera playerCamera;  // 模型内的摄像机

    [Header("旋转设置")]
    public float rotationSpeed = 5f;
    public bool updateRotation = true;

    private Transform modelTransform;  // SK_FP_CH_Default_Root

    void Start()
    {
        // 获取模型子对象
        if (transform.childCount > 0)
        {
            modelTransform = transform.GetChild(0);
        }

        // 自动查找摄像机
        if (playerCamera == null && modelTransform != null)
        {
            playerCamera = modelTransform.GetComponentInChildren<Camera>();
        }

        Debug.Log($"初始化: 旋转轴={name}, 模型={modelTransform?.name}, 摄像机={playerCamera?.name}");
    }

    void LateUpdate()
    {
        if (!updateRotation || playerCamera == null) return;

        // 让旋转轴跟随摄像机Y轴旋转
        float targetY = playerCamera.transform.eulerAngles.y;
        Quaternion targetRotation = Quaternion.Euler(0, targetY, 0);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );

        // 确保模型本地旋转为0
        if (modelTransform != null)
        {
            modelTransform.localRotation = Quaternion.identity;
        }
    }

    void OnDrawGizmosSelected()
    {
        // 可视化旋转轴
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 2);
        Gizmos.DrawWireSphere(transform.position, 0.1f);
    }
}
