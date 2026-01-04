using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelFollowCameraWithAnimator : MonoBehaviour
{
    [Header("引用设置")]
    public Transform cameraTransform;
    public Animator modelAnimator;

    [Header("旋转设置")]
    public float followSpeed = 5f;
    [Range(0f, 1f)]
    public float rotationWeight = 0.5f; // 旋转权重

    [Header("Animator参数")]
    public string rotationYParameter = "CameraRotationY";
    public string rotationSpeedParameter = "RotationSpeed";

    private float currentRotationY;
    private float targetRotationY;

    void Start()
    {
        // 自动获取组件
        if (modelAnimator == null)
            modelAnimator = GetComponent<Animator>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        if (cameraTransform == null || modelAnimator == null) return;

        // 获取摄像机Y轴旋转
        targetRotationY = cameraTransform.eulerAngles.y;

        // 平滑插值
        currentRotationY = Mathf.LerpAngle(currentRotationY, targetRotationY, followSpeed * Time.deltaTime);

        // 设置到Animator参数
        modelAnimator.SetFloat(rotationYParameter, currentRotationY);

        // 计算旋转速度（可选）
        float rotationDelta = Mathf.DeltaAngle(transform.eulerAngles.y, currentRotationY);
        modelAnimator.SetFloat(rotationSpeedParameter, Mathf.Abs(rotationDelta));
    }

    // 在Animator中使用这个值
    // 1. 在Animator中添加float参数 CameraRotationY
    // 2. 创建一个Blend Tree或使用Script控制旋转
}