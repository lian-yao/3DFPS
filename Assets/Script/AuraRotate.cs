using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AuraRotate : MonoBehaviour
{
    public float rotateSpeed = 30f; // 旋转速度，慢一点更高级

    void Update()
    {
        transform.Rotate(0, 0, rotateSpeed * Time.deltaTime); // 绕Z轴旋转
    }
}