using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    // 游戏主场景名称（必须和Build Settings里一致）
    public string test;
    public AudioSource BGM;
    public Texture2D cursorTex;
    public FlashScreen flashScreen;
    void Start()
    {
        if (BGM != null)
        {
            BGM.Play();
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (cursorTex != null)
        {
            Cursor.SetCursor(cursorTex, new Vector2(cursorTex.width / 2, cursorTex.height / 2), CursorMode.Auto);
        }

        // 兼容低版本的调试：仅提示检查Build Settings
        Debug.Log("=== 请检查Build Settings中的场景列表 ===");
        Debug.Log("当前已打包的场景数量：" + SceneManager.sceneCountInBuildSettings);
    }

    void Update()
    {
        // 按空格键进入游戏
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (flashScreen != null) flashScreen.Flash();
            // 先判断场景是否存在
            if (string.IsNullOrEmpty(test))
            {
                Debug.LogError("错误：test字段未赋值！请在Inspector面板填写游戏主场景名称");
                return;
            }

            // 尝试加载场景（添加异常捕获）
            try
            {
                // 先检查场景是否在Build列表中（低版本兼容写法）
                bool sceneExists = false;
                // 遍历所有已打包场景（低版本通用逻辑）
                for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    // 低版本通过解析路径获取场景名
                    string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                    string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                    if (sceneName == test)
                    {
                        sceneExists = true;
                        break;
                    }
                }

                if (!sceneExists)
                {
                    Debug.LogError("错误：场景[" + test + "]未添加到Build Settings！");
                    return;
                }

                // 加载场景
                SceneManager.LoadScene(test);
                if (BGM != null)
                {
                    BGM.Stop();
                }
                Debug.Log("正在加载场景：" + test);
            }
            catch (System.Exception e)
            {
                Debug.LogError("加载场景失败：" + e.Message);
            }
        }

        // 按ESC退出游戏
        if (Input.GetKeyDown(KeyCode.Escape))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}