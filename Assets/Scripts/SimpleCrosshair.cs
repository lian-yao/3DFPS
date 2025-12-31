using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SimpleCrosshair : MonoBehaviour
{
    [Header("准星设置")]
    public Image crosshairImage;
    public Color normalColor = Color.white;
    public Color enemyColor = Color.red;

    [Header("大小变化")]
    public float defaultSize = 20f;
    public float shootSize = 30f;
    public float moveSize = 25f;
    public float changeSpeed = 10f;

    private RectTransform crosshairRect;
    private float currentSize;
    private Camera playerCamera;

    void Start()
    {
        // 获取摄像机
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            Debug.Log("使用主摄像机");
        }

        // 初始化准星
        InitializeCrosshair();

        Debug.Log("准星系统初始化完成");
    }

    void InitializeCrosshair()
    {
        // 如果crosshairImage未设置，尝试查找
        if (crosshairImage == null)
        {
            // 方法1：查找场景中现有的准星
            crosshairImage = GameObject.Find("Crosshair")?.GetComponent<Image>();

            if (crosshairImage == null)
            {
                // 方法2：自动创建准星
                crosshairImage = CreateAutoCrosshair();
            }
        }

        if (crosshairImage == null)
        {
            Debug.LogError("无法创建或找到准星图像！");
            enabled = false; // 禁用脚本，避免进一步错误
            return;
        }

        crosshairRect = crosshairImage.GetComponent<RectTransform>();
        currentSize = defaultSize;

        // 确保初始大小正确
        if (crosshairRect != null)
        {
            crosshairRect.sizeDelta = new Vector2(defaultSize, defaultSize);
        }
    }

    Image CreateAutoCrosshair()
    {
        Debug.Log("自动创建准星UI...");

        // 1. 创建Canvas
        GameObject canvasObj = new GameObject("AutoCrosshairCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // 确保在最前面

        // 添加Canvas Scaler（适应屏幕尺寸）
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // 2. 创建准星图像
        GameObject crosshairObj = new GameObject("AutoCrosshair");
        crosshairObj.transform.SetParent(canvas.transform);

        Image img = crosshairObj.AddComponent<Image>();

        // 创建简单的十字准星纹理（代码生成）
        CreateCrosshairTexture(img);

        // 3. 设置RectTransform
        RectTransform rt = crosshairObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(defaultSize, defaultSize);

        return img;
    }

    void CreateCrosshairTexture(Image image)
    {
        // 创建一个简单的红色圆点作为准星
        // 实际项目中可以使用图片资源
        image.color = Color.white;

        // 或者创建十字准星
        CreateCrossCrosshair(image);
    }

    void CreateCrossCrosshair(Image parentImage)
    {
        // 创建十字准星（由4个线条组成）
        CreateCrossLine(parentImage.transform, 0, 10, 2, 20, Color.white);   // 上
        CreateCrossLine(parentImage.transform, 0, -10, 2, 20, Color.white);  // 下
        CreateCrossLine(parentImage.transform, 10, 0, 20, 2, Color.white);   // 右
        CreateCrossLine(parentImage.transform, -10, 0, 20, 2, Color.white);  // 左

        // 中心点
        CreateCrossLine(parentImage.transform, 0, 0, 4, 4, Color.red);
    }

    void CreateCrossLine(Transform parent, float posX, float posY, float width, float height, Color color)
    {
        GameObject line = new GameObject("CrossLine");
        line.transform.SetParent(parent);

        Image img = line.AddComponent<Image>();
        img.color = color;

        RectTransform rt = line.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(posX, posY);
        rt.sizeDelta = new Vector2(width, height);
    }

    void Update()
    {
        // 如果准星图像为空，跳过更新
        if (crosshairImage == null || crosshairRect == null)
        {
            Debug.LogWarning("准星图像或RectTransform为空，跳过更新");
            return;
        }

        UpdateCrosshairColor();
        UpdateCrosshairSize();

        // 应用大小变化
        crosshairRect.sizeDelta = Vector2.Lerp(
            crosshairRect.sizeDelta,
            new Vector2(currentSize, currentSize),
            Time.deltaTime * changeSpeed
        );
    }

    void UpdateCrosshairColor()
    {
        if (crosshairImage == null || playerCamera == null) return;

        // 从屏幕中心发射射线
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            // 方法A：使用组件检测（推荐）
            EnemyHealth enemy = hit.collider.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                crosshairImage.color = enemyColor;
                return;
            }

            // 方法B：检查物体名字
            // if (hit.collider.gameObject.name.Contains("Enemy"))
            // {
            //     crosshairImage.color = enemyColor;
            //     return;
            // }
        }

        crosshairImage.color = normalColor;
    }

    void UpdateCrosshairSize()
    {
        if (crosshairRect == null) return;

        // 重置为基础大小
        currentSize = defaultSize;

        // 射击时变大
        if (Input.GetButton("Fire1"))
        {
            currentSize = shootSize;
        }

        // 移动时稍大
        float moveInput = Mathf.Abs(Input.GetAxis("Horizontal")) +
                         Mathf.Abs(Input.GetAxis("Vertical"));
        if (moveInput > 0.1f)
        {
            currentSize = Mathf.Lerp(defaultSize, moveSize, moveInput);
        }
    }

    // 外部调用：射击反馈
    public void OnShoot()
    {
        currentSize = shootSize;
    }

    // 调试：确保准星可见
    void OnGUI()
    {
        if (crosshairImage == null)
        {
            GUI.color = Color.red;
            GUI.Label(new Rect(10, 30, 300, 50), "准星图像为空！按R键重新初始化");

            if (Input.GetKeyDown(KeyCode.R))
            {
                InitializeCrosshair();
            }
        }
    }
}
