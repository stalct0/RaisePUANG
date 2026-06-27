using UnityEngine;

public sealed class DeliverySpawner : MonoBehaviour
{
    [Header("--- Spawn Timing ---")]
    public float minSpawnInterval = 0.45f;
    public float maxSpawnInterval = 0.9f;

    [Header("--- Item Chance ---")]
    [Range(0f, 1f)] public float obstacleChance = 0.55f;
    [Range(0f, 1f)] public float coinChance = 0.35f;
    [Range(0f, 1f)] public float starChance = 0.35f;

    [Header("--- Star Force Cooldown ---")]
    [Tooltip("스타가 한 번 생성된 후, 다시 생성되기까지 필요한 강제 대기 시간(초)")]
    public float starCooldown = 5.0f; // 예: 5초 동안은 절대 안 나옴
    private float starTimer;

    [Header("--- Fall Speed ---")]
    public float obstacleFallSpeed = 520f;
    public float coinFallSpeed = 420f;
    public float starFallSpeed = 600f;

    private DeliveryGameManager manager;
    private float spawnTimer;

    private void Update()
    {
        if (manager == null || !manager.IsRunning) return;

        // 1. 스타 쿨타임 타이머 실시간 감소
        if (starTimer > 0f)
        {
            starTimer -= Time.deltaTime;
        }

        // 2. 전체 아이템 스폰 타이머 체크
        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;


        manager.SpawnRandomItem();
        ResetTimer();
    }

    public void Initialize(DeliveryGameManager owner)
    {
        manager = owner;
        starTimer = 0f;
        ResetTimer();
    }

    public float GetFallSpeed(DeliveryItemType itemType)
    {
        switch (itemType)
        {
            case DeliveryItemType.Obstacle: return obstacleFallSpeed;
            case DeliveryItemType.Coin: return coinFallSpeed;
            case DeliveryItemType.Star: return starFallSpeed;
            default: return obstacleFallSpeed;
        }
    }

 public DeliveryItemType GetRandomItemType()
    {
        float roll = Random.value;
        DeliveryItemType selectedType;

        // 1. 기본 확률로 아이템 선택
        if (roll < obstacleChance) 
            selectedType = DeliveryItemType.Obstacle;
        else if (roll < obstacleChance + coinChance) 
            selectedType = DeliveryItemType.Coin;
        else 
            selectedType = DeliveryItemType.Star;

        // 2. 만약 스타가 뽑혔는데 아직 쿨타임(텀)이 안 끝났다면?
        if (selectedType == DeliveryItemType.Star && starTimer > 0f)
        {
            // 장애물이나 코인 중 하나로 강제 치환 (여기서는 50% 확률로 결정)
            selectedType = (Random.value < 0.6f) ? DeliveryItemType.Obstacle : DeliveryItemType.Coin;
        }

        // 3. 최종적으로 스타가 정상 스폰된다면 쿨타임(텀)을 다시 세팅
        if (selectedType == DeliveryItemType.Star)
        {
            starTimer = starCooldown;
        }

        return selectedType;
    }

    private void ResetTimer()
    {
        spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);
    }
}
