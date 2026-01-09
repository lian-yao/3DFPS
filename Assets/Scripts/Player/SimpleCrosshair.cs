using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SimpleCrosshair : MonoBehaviour
{
    [Header("???????")]
    public Image crosshairImage;
    public Color normalColor = Color.white;
    public Color enemyColor = Color.red;

    [Header("??ß≥?Å£")]
    public float defaultSize = 20f;
    public float shootSize = 30f;
    public float moveSize = 25f;
    public float changeSpeed = 10f;

    private RectTransform crosshairRect;
    private float currentSize;
    private Camera playerCamera;

    void Start()
    {
        // ????????
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            Debug.Log("??????????");
        }

        // ????????
        InitializeCrosshair();

        Debug.Log("?????????????");
    }

    void InitializeCrosshair()
    {
        // ???crosshairImage¶ƒ????????????
        if (crosshairImage == null)
        {
            // ????1??????????????ß÷????
            crosshairImage = GameObject.Find("Crosshair")?.GetComponent<Image>();

            if (crosshairImage == null)
            {
                // ????2????????????
                crosshairImage = CreateAutoCrosshair();
            }
        }

        if (crosshairImage == null)
        {
            Debug.LogError("??????????????????");
            enabled = false; // ????????????????????
            return;
        }

        crosshairRect = crosshairImage.GetComponent<RectTransform>();
        currentSize = defaultSize;

        // ????????ß≥???
        if (crosshairRect != null)
        {
            crosshairRect.sizeDelta = new Vector2(defaultSize, defaultSize);
        }
    }

    Image CreateAutoCrosshair()
    {
        Debug.Log("??????????UI...");

        // 1. ????Canvas
        GameObject canvasObj = new GameObject("AutoCrosshairCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // ??????????

        // ????Canvas Scaler??????????¥Ñ
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // 2. ??????????
        GameObject crosshairObj = new GameObject("AutoCrosshair");
        crosshairObj.transform.SetParent(canvas.transform);

        Image img = crosshairObj.AddComponent<Image>();

        // ????????????????????????????
        CreateCrosshairTexture(img);

        // 3. ????RectTransform
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
        // ?????????????????????
        // ???????ß·???????????
        image.color = Color.white;

        // ?????????????
        CreateCrossCrosshair(image);
    }

    void CreateCrossCrosshair(Image parentImage)
    {
        // ?????????????4??????????
        CreateCrossLine(parentImage.transform, 0, 10, 2, 20, Color.white);   // ??
        CreateCrossLine(parentImage.transform, 0, -10, 2, 20, Color.white);  // ??
        CreateCrossLine(parentImage.transform, 10, 0, 20, 2, Color.white);   // ??
        CreateCrossLine(parentImage.transform, -10, 0, 20, 2, Color.white);  // ??

        // ?????
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
        // ?????????????????????
        if (crosshairImage == null || crosshairRect == null)
        {
            Debug.LogWarning("???????RectTransform????????????");
            return;
        }

        UpdateCrosshairColor();
        UpdateCrosshairSize();

        // ????ß≥?Å£
        crosshairRect.sizeDelta = Vector2.Lerp(
            crosshairRect.sizeDelta,
            new Vector2(currentSize, currentSize),
            Time.deltaTime * changeSpeed
        );
    }

    void UpdateCrosshairColor()
    {
        if (crosshairImage == null || playerCamera == null) return;

        // ????????????????
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            // ????A????????????????
            EnemyHealth enemy = hit.collider.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                crosshairImage.color = enemyColor;
                return;
            }

            // ????B?????????????
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

        // ???????????ß≥
        currentSize = defaultSize;

        // ???????
        if (Input.GetButton("Fire1"))
        {
            currentSize = shootSize;
        }

        // ???????
        float moveInput = Mathf.Abs(Input.GetAxis("Horizontal")) +
                         Mathf.Abs(Input.GetAxis("Vertical"));
        if (moveInput > 0.1f)
        {
            currentSize = Mathf.Lerp(defaultSize, moveSize, moveInput);
        }
    }

    // ??????????????
    public void OnShoot()
    {
        currentSize = shootSize;
    }

    // ?????????????
    void OnGUI()
    {
        if (crosshairImage == null)
        {
            GUI.color = Color.red;
            GUI.Label(new Rect(10, 30, 300, 50), "????????????R??????????");

            if (Input.GetKeyDown(KeyCode.R))
            {
                InitializeCrosshair();
            }
        }
    }
}
