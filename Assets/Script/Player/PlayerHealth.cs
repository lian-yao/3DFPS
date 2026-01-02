using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("ÉúÃüÉèÖÃ")]
    public float maxHealth = 100f;
    public float currentHealth;
    public Image healthBar; // UIÑªÌõ
    public GameObject deathUI;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateHealthUI()
    {
        if (healthBar != null)
            healthBar.fillAmount = currentHealth / maxHealth;
    }

    void Die()
    {
        // Íæ¼ÒËÀÍöÂß¼­
        Debug.Log("Íæ¼ÒËÀÍö");
        Time.timeScale = 0f; // ÔİÍ£ÓÎÏ·
        //deathUI.SetActive(true);
        //Cursor.lockState = CursorLockMode.None;
    }
}
