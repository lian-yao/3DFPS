using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// 类名需与文件名一致（若文件名为bigin.cs，类名应为bigin）
public class Begin : MonoBehaviour
{
    // 方法需用大括号闭合
    public void OnLoginButtonClick()
    {
        SceneManager.LoadScene("map1sence");
    }
}
