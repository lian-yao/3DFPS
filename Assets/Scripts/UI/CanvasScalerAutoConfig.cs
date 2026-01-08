using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Canvas Scaler 自动配置脚本
/// 确保血条画布有正确的缩放设置，适配不同屏幕尺寸
/// </summary>
public class CanvasScalerAutoConfig : MonoBehaviour
{
    [Header("Canvas Scaler 设置")]
    public CanvasScaler.ScaleMode uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    public Vector2 referenceResolution = new Vector2(1920, 1080);
    public CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
    public float matchWidthOrHeight = 0.5f;

    private void Awake()
    {
        // 查找或添加 Canvas Scaler 组件
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("未找到 Canvas 组件");
            return;
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        // 配置 Canvas Scaler
        scaler.uiScaleMode = uiScaleMode;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = screenMatchMode;
        scaler.matchWidthOrHeight = matchWidthOrHeight;
        scaler.referencePixelsPerUnit = 100;

        // 确保 Canvas 设置正确
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        Debug.Log("Canvas Scaler 已自动配置完成");
    }
}