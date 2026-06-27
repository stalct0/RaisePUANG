using TMPro;
using UnityEngine;

public sealed class DeliveryHud : MonoBehaviour
{
    public TextMeshProUGUI lifeText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI invincibleText;
    public TextMeshProUGUI resultText;

    public void Refresh(int life, int score, float remainingTime, bool isInvincible, float invincibleRemaining)
    {
        if (lifeText != null) lifeText.text = $"Life {life}";
        if (scoreText != null) scoreText.text = $"Score {score}";
        if (timeText != null) timeText.text = $"Time {Mathf.CeilToInt(Mathf.Max(0f, remainingTime))}";
        if (invincibleText != null)
        {
            invincibleText.text = isInvincible ? $"STAR {invincibleRemaining:0.0}s" : string.Empty;
        }
    }

    public void ShowResult(string result)
    {
        if (resultText == null) return;

        resultText.gameObject.SetActive(true);
        resultText.text = result;
    }

    public void HideResult()
    {
        if (resultText != null) resultText.gameObject.SetActive(false);
    }
}
