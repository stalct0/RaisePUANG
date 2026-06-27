using UnityEngine;

public sealed class DeliveryFallingItem : MonoBehaviour
{
    public DeliveryItemType itemType;
    public float fallSpeed = 420f;
    public int scoreValue = 10;
    public int coinMoneyValue = 100;

    public RectTransform RectTransform { get; private set; }

    private DeliveryGameManager manager;
    private RectTransform playArea;

    private void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        RectTransform.anchoredPosition += Vector2.down * (fallSpeed * Time.deltaTime);

        if (playArea != null && RectTransform.anchoredPosition.y < -playArea.rect.height * 0.5f - 120f)
        {
            manager.NotifyItemMissed(this);
        }
    }

    public void Initialize(DeliveryGameManager owner, RectTransform area, DeliveryItemType type, float speed, int score, int money)
    {
        manager = owner;
        playArea = area;
        itemType = type;
        fallSpeed = speed;
        scoreValue = score;
        coinMoneyValue = money;
    }
}
