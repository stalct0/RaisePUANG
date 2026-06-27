using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DeliveryGameManager : MonoBehaviour
{
    [Header("--- Game References ---")]
    public RectTransform playArea;
    public DeliverySpawner spawner;
    public DeliveryHud hud;
    public TMP_FontAsset uiFont;

    [Header("--- Prefabs ---")]
    public GameObject playerPrefab;
    public GameObject obstaclePrefab;
    public GameObject coinPrefab;
    public GameObject starPrefab;

    [Header("--- Lane Settings ---")]
    public int laneCount = 4;
    public int playerStartLaneIndex = 1;
    public float lanePadding = 100f;
    public float playerMoveLerpSpeed = 16f;

    [Header("--- Spawn Settings ---")]
    public float minSpawnInterval = 0.01f;
    public float maxSpawnInterval = 0.1f;
    [Range(0f, 1f)] public float obstacleChance = 0.55f;
    [Range(0f, 1f)] public float coinChance = 0.35f;
    [Range(0f, 1f)] public float starChance = 0.1f;
    public float obstacleFallSpeed = 520f;
    public float coinFallSpeed = 420f;
    public float starFallSpeed = 460f;

    [Header("--- Game Rules ---")]
    public int startLife = 3;
    public float gameDuration = 45f;
    public int scorePerCoin = 10;
    public int moneyPerCoin = 100;
    public int basePay = 500;
    public int scoreToPayMultiplier = 20;
    public int lifeBonusPay = 300;
    public float minInvincibleSeconds = 5f;
    public float maxInvincibleSeconds = 10f;

    [Header("--- Runtime Options ---")]
    public bool startOnPlay = true;

    [Header("--- Visual Settings ---")]
    public Vector2 playerSize = new Vector2(72f, 72f);
    public Vector2 itemSize = new Vector2(56f, 56f);

    public bool IsRunning { get; private set; }

    private readonly List<DeliveryFallingItem> activeItems = new List<DeliveryFallingItem>();
    private DeliveryPlayerController player;
    private int life;
    private int score;
    private int earnedMoneyFromCoins;
    private float remainingTime;
    private float invincibleRemaining;

    private void Start()
    {
        EnsureRuntimeObjects();
        if (startOnPlay)
        {
            StartGame();
        }
    }

    private void Update()
    {
        if (!IsRunning) return;

        remainingTime -= Time.deltaTime;
        invincibleRemaining = Mathf.Max(0f, invincibleRemaining - Time.deltaTime);

        hud.Refresh(life, score, remainingTime, invincibleRemaining > 0f, invincibleRemaining);

        if (life <= 0 || remainingTime <= 0f)
        {
            FinishGame();
        }
    }

    public void StartGame()
    {
        ClearItems();
        life = startLife;
        score = 0;
        earnedMoneyFromCoins = 0;
        remainingTime = gameDuration;
        invincibleRemaining = 0f;
        IsRunning = true;
        hud.HideResult();
        hud.Refresh(life, score, remainingTime, false, 0f);
    }

    public void SpawnRandomItem()
    {
        DeliveryItemType type = spawner.GetRandomItemType();
        GameObject prefab = GetItemPrefab(type);
        if (prefab == null)
        {
            Debug.LogError($"{type} prefab is not assigned.");
            return;
        }

        GameObject itemObject = Instantiate(prefab);
        itemObject.name = type.ToString();
        itemObject.transform.SetParent(playArea, false);

        RectTransform itemRect = itemObject.GetComponent<RectTransform>();
        itemRect.sizeDelta = itemSize;
        itemRect.anchoredPosition = new Vector2(GetLaneX(Random.Range(0, laneCount)), playArea.rect.height * 0.5f + itemSize.y);

        DeliveryFallingItem item = itemObject.GetComponent<DeliveryFallingItem>();
        if (item == null)
        {
            Debug.LogError($"{prefab.name} needs DeliveryFallingItem.");
            Destroy(itemObject);
            return;
        }

        item.Initialize(this, playArea, type, spawner.GetFallSpeed(type), scorePerCoin, moneyPerCoin);
        activeItems.Add(item);
    }

    public void HandleItemCollision(DeliveryFallingItem item)
    {
        if (!IsRunning || item == null || !activeItems.Contains(item)) return;

        ApplyItem(item);
        RemoveItem(item);
    }

    public void NotifyItemMissed(DeliveryFallingItem item)
    {
        RemoveItem(item);
    }

    private void EnsureRuntimeObjects()
    {
        if (playArea == null) playArea = GetComponent<RectTransform>();

        if (spawner == null)
        {
            spawner = GetComponentInChildren<DeliverySpawner>();
        }

        if (spawner == null)
        {
            Debug.LogError("DeliverySpawner is not assigned.");
            enabled = false;
            return;
        }

        if (player == null)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("Player prefab is not assigned.");
                enabled = false;
                return;
            }

            GameObject playerObject = Instantiate(playerPrefab);
            playerObject.name = "DeliveryPlayer";
            playerObject.transform.SetParent(playArea, false);
            RectTransform playerRect = playerObject.GetComponent<RectTransform>();
            playerRect.anchorMin = new Vector2(0.5f, 0f);
            playerRect.anchorMax = new Vector2(0.5f, 0f);
            playerRect.pivot = new Vector2(0.5f, 0.5f);
            playerRect.anchoredPosition = new Vector2(0f, 72f);
            playerRect.sizeDelta = playerSize;
            player = playerObject.GetComponent<DeliveryPlayerController>();
            if (player == null)
            {
                Debug.LogError($"{playerPrefab.name} needs DeliveryPlayerController.");
                enabled = false;
                return;
            }

            DeliveryCollisionReceiver collisionReceiver = playerObject.GetComponent<DeliveryCollisionReceiver>();
            if (collisionReceiver == null)
            {
                Debug.LogError($"{playerPrefab.name} needs DeliveryCollisionReceiver.");
                enabled = false;
                return;
            }

            collisionReceiver.Initialize(this);
        }

        if (hud == null)
        {
            hud = GetComponentInChildren<DeliveryHud>();
        }

        if (hud == null)
        {
            hud = CreateRuntimeHud();
        }

        CreateLaneGuidesIfNeeded();
        ApplyPublicSettingsToChildren();
        spawner.Initialize(this);
        player.Configure(playArea, laneCount, lanePadding);
        if (player != null) player.GetComponent<RectTransform>().sizeDelta = playerSize;
    }

    private void ApplyPublicSettingsToChildren()
    {
        player.laneCount = laneCount;
        player.startLaneIndex = playerStartLaneIndex;
        player.moveLerpSpeed = playerMoveLerpSpeed;
        player.lanePadding = lanePadding;

        spawner.minSpawnInterval = minSpawnInterval;
        spawner.maxSpawnInterval = maxSpawnInterval;
        spawner.obstacleChance = obstacleChance;
        spawner.coinChance = coinChance;
        spawner.starChance = starChance;
        spawner.obstacleFallSpeed = obstacleFallSpeed;
        spawner.coinFallSpeed = coinFallSpeed;
        spawner.starFallSpeed = starFallSpeed;
    }

    private DeliveryHud CreateRuntimeHud()
    {
        GameObject hudObject = new GameObject("DeliveryHud", typeof(RectTransform), typeof(DeliveryHud));
        hudObject.transform.SetParent(playArea, false);
        RectTransform hudRect = hudObject.GetComponent<RectTransform>();
        hudRect.anchorMin = Vector2.zero;
        hudRect.anchorMax = Vector2.one;
        hudRect.offsetMin = Vector2.zero;
        hudRect.offsetMax = Vector2.zero;

        DeliveryHud runtimeHud = hudObject.GetComponent<DeliveryHud>();
        runtimeHud.lifeText = CreateHudText(hudObject.transform, "LifeText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -48f), new Vector2(190f, -12f), TextAlignmentOptions.Left);
        runtimeHud.scoreText = CreateHudText(hudObject.transform, "ScoreText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -88f), new Vector2(230f, -52f), TextAlignmentOptions.Left);
        runtimeHud.timeText = CreateHudText(hudObject.transform, "TimeText", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-190f, -48f), new Vector2(-18f, -12f), TextAlignmentOptions.Right);
        runtimeHud.invincibleText = CreateHudText(hudObject.transform, "InvincibleText", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-230f, -88f), new Vector2(-18f, -52f), TextAlignmentOptions.Right);
        runtimeHud.resultText = CreateHudText(hudObject.transform, "ResultText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-220f, -110f), new Vector2(220f, 110f), TextAlignmentOptions.Center);
        runtimeHud.resultText.fontSize = 30f;
        runtimeHud.resultText.gameObject.SetActive(false);
        return runtimeHud;
    }

    private TextMeshProUGUI CreateHudText(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = 24f;
        text.color = Color.white;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        if (uiFont != null) text.font = uiFont;
        return text;
    }

    private void CreateLaneGuidesIfNeeded()
    {
        if (playArea.Find("LaneGuides") != null) return;

        GameObject guideRoot = new GameObject("LaneGuides", typeof(RectTransform));
        guideRoot.transform.SetParent(playArea, false);
        RectTransform rootRect = guideRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        for (int i = 1; i < laneCount; i++)
        {
            GameObject guide = new GameObject("LaneGuide", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            guide.transform.SetParent(guideRoot.transform, false);
            RectTransform guideRect = guide.GetComponent<RectTransform>();
            guideRect.anchorMin = new Vector2(0.5f, 0f);
            guideRect.anchorMax = new Vector2(0.5f, 1f);
            guideRect.pivot = new Vector2(0.5f, 0.5f);
            guideRect.sizeDelta = new Vector2(4f, 0f);
            float leftLane = GetLaneX(i - 1);
            float rightLane = GetLaneX(i);
            guideRect.anchoredPosition = new Vector2((leftLane + rightLane) * 0.5f, 0f);
            guide.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.18f);
        }
    }

    private void ApplyItem(DeliveryFallingItem item)
    {
        switch (item.itemType)
        {
            case DeliveryItemType.Obstacle:
                if (invincibleRemaining <= 0f)
                {
                    life = Mathf.Max(0, life - 1);
                }
                break;
            case DeliveryItemType.Coin:
                score += item.scoreValue;
                earnedMoneyFromCoins += item.coinMoneyValue;
                break;
            case DeliveryItemType.Star:
                invincibleRemaining = Random.Range(minInvincibleSeconds, maxInvincibleSeconds);
                break;
        }
    }

    private void FinishGame()
    {
        IsRunning = false;
        ClearItems();

        int pay = life > 0 ? basePay + earnedMoneyFromCoins + score * scoreToPayMultiplier + life * lifeBonusPay : 0;
        if (pay > 0)
        {
            ApplyPay(pay);
        }

        string result = life > 0
            ? $"배달 완료\n점수 {score}\n라이프 {life}\n알바비 +{pay}"
            : $"배달 실패\n라이프가 모두 깎여 알바비를 받지 못했다.\n점수 {score}";
        hud.ShowResult(result);
    }

    private void ApplyPay(int pay)
    {
        if (CampusLifeGameManager.Instance != null)
        {
            CampusLifeGameManager.Instance.TryApplyActivity(
                "배달 알바",
                new CampusLifeStatDelta
                {
                    money = pay,
                    condition = -5
                }
            );
        }
    }

    private float GetLaneX(int laneIndex)
    {
        if (laneCount <= 1) return 0f;

        float width = playArea.rect.width - lanePadding * 2f;
        float step = width / (laneCount - 1);
        return -width * 0.5f + step * Mathf.Clamp(laneIndex, 0, laneCount - 1);
    }

    private GameObject GetItemPrefab(DeliveryItemType type)
    {
        switch (type)
        {
            case DeliveryItemType.Obstacle: return obstaclePrefab;
            case DeliveryItemType.Coin: return coinPrefab;
            case DeliveryItemType.Star: return starPrefab;
            default: return null;
        }
    }

    private void ClearItems()
    {
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            if (activeItems[i] != null)
            {
                Destroy(activeItems[i].gameObject);
            }
        }

        activeItems.Clear();
    }

    private void RemoveItem(DeliveryFallingItem item)
    {
        if (item == null) return;

        activeItems.Remove(item);
        Destroy(item.gameObject);
    }
}
