using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TitleFloat : MonoBehaviour
{
    // 浮动幅度（数值越小越平缓）
    public float floatRange = 15f;
    // 浮动速度
    public float floatSpeed = 1f;
    // 初始位置
    private Vector3 originalPos;

    void Start()
    {
        originalPos = transform.localPosition;
    }

    void Update()
    {
        // 正弦曲线实现平滑上下浮动，无卡顿
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatRange;
        transform.localPosition = new Vector3(originalPos.x, originalPos.y + yOffset, originalPos.z);
    }
}