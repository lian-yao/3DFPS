using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlashScreen : MonoBehaviour
{
    public Image flashImage;
    public Color flashColor = new Color(0.533f, 1f, 0.533f, 0.2f); // µ­ÂÌ°ëÍ¸
    public float flashTime = 0.1f;

    void Start()
    {
        flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0);
    }

    public void Flash()
    {
        flashImage.color = flashColor;
        Invoke("HideFlash", flashTime);
    }

    void HideFlash()
    {
        flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0);
    }
}