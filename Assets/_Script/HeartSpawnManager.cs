using UnityEngine;

public sealed class HeartSpawnManager : MonoBehaviour
{
    [Header("References")]
    public HeartPickup heartPrefab;
    public Transform player;
    public Camera targetCamera;
    public HeartArrowIndicator arrowIndicator;
    public NovelSceneManager novelSceneManager;

    [Header("Spawn Area")]
    public Vector2 spawnMin = new Vector2(-19f, -21f);
    public Vector2 spawnMax = new Vector2(26f, 15f);
    public float minDistanceFromPlayer = 5f;

    [Header("Forbidden Zones")]
    public bool autoFindForbiddenZones = true;
    public Collider2D[] forbiddenZoneColliders;
    public Renderer[] forbiddenZoneRenderers;

    [Header("Timing")]
    public bool spawnOnStart = true;
    public float respawnDelay = 20f;
    public float heartDisplaySeconds = 20f;
    public float collectGraceSeconds = 10f;

    private HeartPickup currentHeart;
    private float respawnTimer;

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (novelSceneManager == null)
            novelSceneManager = FindFirstObjectByType<NovelSceneManager>();

        if (autoFindForbiddenZones)
            FindForbiddenZones();

        if (spawnOnStart)
        {
            SpawnHeart();
        }
        else
        {
            respawnTimer = respawnDelay;
        }
    }

    private void Update()
    {
        if (CampusLifeGameManager.Instance != null &&
            !CampusLifeGameManager.Instance.IsPlaying)
        {
            return;
        }

        if (currentHeart != null)
        {
            if (arrowIndicator != null)
                arrowIndicator.Track(targetCamera, player, currentHeart.Transform);

            return;
        }

        if (arrowIndicator != null)
            arrowIndicator.Hide();

        respawnTimer -= Time.deltaTime;

        if (respawnTimer <= 0f)
            SpawnHeart();
    }

    public void NotifyHeartCollected(HeartPickup heart)
    {
        if (heart != currentHeart) return;

        Destroy(currentHeart.gameObject);
        currentHeart = null;
        respawnTimer = respawnDelay;

        if (arrowIndicator != null)
            arrowIndicator.Hide();

        if (novelSceneManager != null)
            novelSceneManager.OpenDatingIntro();
    }

    public void NotifyHeartExpired(HeartPickup heart)
    {
        if (heart != currentHeart) return;

        Destroy(currentHeart.gameObject);
        currentHeart = null;
        respawnTimer = respawnDelay;

        if (arrowIndicator != null)
            arrowIndicator.Hide();
    }

    private void SpawnHeart()
    {
        if (heartPrefab == null || player == null) return;

        Vector2 spawnPosition = FindSpawnPosition();

        currentHeart = Instantiate(heartPrefab, spawnPosition, Quaternion.identity);
        currentHeart.name = "HeartPickup";
        currentHeart.Initialize(this, heartDisplaySeconds, collectGraceSeconds);
    }

    private Vector2 FindSpawnPosition()
    {
        Vector2 playerPosition = player.position;
        Vector2 fallback = playerPosition;
        bool hasFallback = false;

        for (int i = 0; i < 80; i++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(spawnMin.x, spawnMax.x),
                Random.Range(spawnMin.y, spawnMax.y)
            );

            bool forbidden = IsForbiddenSpawnPosition(candidate);

            if (!forbidden && !hasFallback)
            {
                fallback = candidate;
                hasFallback = true;
            }

            if (!forbidden && Vector2.Distance(candidate, playerPosition) >= minDistanceFromPlayer)
            {
                return candidate;
            }
        }

        return hasFallback ? fallback : playerPosition;
    }

    private bool IsForbiddenSpawnPosition(Vector2 position)
    {
        if (forbiddenZoneColliders != null)
        {
            for (int i = 0; i < forbiddenZoneColliders.Length; i++)
            {
                Collider2D zone = forbiddenZoneColliders[i];

                if (zone != null && zone.bounds.Contains(position))
                    return true;
            }
        }

        if (forbiddenZoneRenderers != null)
        {
            for (int i = 0; i < forbiddenZoneRenderers.Length; i++)
            {
                Renderer zone = forbiddenZoneRenderers[i];

                if (zone != null && zone.bounds.Contains(position))
                    return true;
            }
        }

        return false;
    }

    private void FindForbiddenZones()
    {
        forbiddenZoneColliders = new[]
        {
            FindCollider("LectureZone"),
            FindCollider("TeamProjZone"),
            FindCollider("DrinkZone")
        };

        forbiddenZoneRenderers = new[]
        {
            FindRenderer("WorkZone")
        };
    }

    private static Collider2D FindCollider(string objectName)
    {
        GameObject obj = GameObject.Find(objectName);
        return obj != null ? obj.GetComponent<Collider2D>() : null;
    }

    private static Renderer FindRenderer(string objectName)
    {
        GameObject obj = GameObject.Find(objectName);
        return obj != null ? obj.GetComponent<Renderer>() : null;
    }
}