using UnityEngine;


public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float invincibleTime = 1.5f;
    [SerializeField] private float blinkInterval = 0.1f;

    private int currentHealth;
    private float invincibleUntil;
    private Renderer[] renderers;
    private bool isVisible = true;

    private void Start()
    {
        currentHealth = maxHealth;
        renderers = GetComponentsInChildren<Renderer>();
        if (GameManager.Instance != null) GameManager.Instance.UpdateHealth(currentHealth, maxHealth);
    }

    private void Update()
    {
        bool invincible = Time.time < invincibleUntil && currentHealth > 0;
        bool visible = !invincible || Mathf.Repeat(Time.time, blinkInterval * 2f) < blinkInterval;
        SetVisible(visible);
    }

    public void TakeDamage(int amount)
    {
        if (Time.time < invincibleUntil || currentHealth <= 0) return;

        invincibleUntil = Time.time + invincibleTime;
        currentHealth = Mathf.Max(currentHealth - amount, 0);

        if (GameFeedback.Instance != null) GameFeedback.Instance.PlayerHit(transform.position);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateHealth(currentHealth, maxHealth);
            if (currentHealth == 0) GameManager.Instance.LoseGame();
        }
    }

    private void SetVisible(bool visible)
    {
        if (visible == isVisible || renderers == null) return;
        isVisible = visible;

        foreach (Renderer modelRenderer in renderers)
        {
            if (modelRenderer != null) modelRenderer.enabled = visible;
        }
    }
}
