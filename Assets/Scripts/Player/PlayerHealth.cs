using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour, IHealth
{
    [Header("��������")]
    public float maxHealth = 100f;          // ����������ֵ
    public float currentHealth;             // ��ҵ�ǰ����ֵ
    
    /// <summary>
    /// 玩家是否死亡（IHealth接口实现）
    /// </summary>
    public bool IsDead => currentHealth <= 0f;
    public Image healthBar;                 // UIѪ����Image���ͣ�������Ϊ���ģʽ��
    public GameObject deathUI;              // ������ʾUI����ѡ������ԭ������UI��
    [Tooltip("��ѡ���Ƿ���������ֵ������0")]
    public bool clampHealth = true;         // ��ֹ����ֵΪ����

    // ��������������¼�����GameManager������
    public System.Action OnPlayerDead;

    void Start()
    {
        // ��ʼ������ֵ
        currentHealth = maxHealth;
        // ��ʼ��Ѫ��UI
        UpdateHealthUI();
        // ��ʼ��������UI
        if (deathUI != null)
        {
            deathUI.SetActive(false);
        }
    }

    /// <summary>
    /// �ܵ��˺��ķ������ⲿ�ɵ��ã�������˹�������Ѫʱ��
    /// </summary>
    /// <param name="damage">�ܵ����˺�ֵ</param>
    public void TakeDamage(float damage)
    {
        // ȷ���˺�ֵΪ����
        damage = Mathf.Abs(damage);
        // �۳�����ֵ
        currentHealth -= damage;

        // ��������ֵ������0����ѡ��
        if (clampHealth)
        {
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }

        // ����Ѫ��UI
        UpdateHealthUI();

        // ����ֵΪ0ʱ���������߼�
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// �ָ�����ֵ�ķ�������ѡ�������Ѫ����
    /// </summary>
    /// <param name="healAmount">�ָ�������ֵ</param>
    public void Heal(float healAmount)
    {
        // ȷ���ָ�ֵΪ����
        healAmount = Mathf.Abs(healAmount);
        // ��������ֵ
        currentHealth += healAmount;
        // ��������ֵ���������ֵ
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        // ����Ѫ��UI
        UpdateHealthUI();
    }

    /// <summary>
    /// ����Ѫ��UI��ʾ
    /// </summary>
    void UpdateHealthUI()
    {
        // ��ֹѪ�����δ��ֵ���¿�ָ�����
        if (healthBar != null)
        {
            // ������������0~1��
            float fillRatio = currentHealth / maxHealth;
            // ͬ����Ѫ���������
            healthBar.fillAmount = fillRatio;
        }
        else
        {
            Debug.LogWarning("Ѫ��Image���δ��ֵ������Inspector�������קѪ������PlayerHealth�ű���healthBar�ֶ�");
        }
    }

    /// <summary>
    /// ��������߼�
    /// </summary>
    void Die()
    {
        Debug.Log("���������");

        // ���������������¼���֪ͨGameManager��ʾʧ�ܽ��棩
        OnPlayerDead?.Invoke();

        // ����ԭ�������߼�����ѡ����ֻ��GameManager��ʧ�ܽ��棬��ע�ͣ�
        // ��ͣ��Ϸ��GameManager�ᴦ����ͣ�������ע�ͣ�
        // Time.timeScale = 0f;

        // ��ʾԭ������UI����ѡ������ֻ��GameManager��ͳһʧ�ܽ��棩
        // if (deathUI != null)
        // {
        //     deathUI.SetActive(true);
        // }
        // else
        // {
        //     Debug.LogWarning("����UIδ��ֵ������Inspector�������ק����UI����PlayerHealth�ű���deathUI�ֶ�");
        // }

        // ������꣨GameManager��ʧ�ܽ�����Ҫ�����ť��������
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ��ѡ����������ƶ�/���������
        // GetComponent<PlayerMovement>().enabled = false;
        // GetComponent<PlayerShoot>().enabled = false;
    }

    /// <summary>
    /// �����������ֵ�����縴����¿�ʼ��Ϸʱ���ã�
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
        // �ָ���Ϸʱ��
        Time.timeScale = 1f;
        // ��������UI
        if (deathUI != null)
        {
            deathUI.SetActive(false);
        }
        // ����������꣨��ѡ��
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}