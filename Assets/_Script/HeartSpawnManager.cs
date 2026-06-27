using UnityEngine;

public sealed class HeartSpawnManager : MonoBehaviour
{
    public DatingIntroController datingIntroController;
    
    [Header("--- References ---")]
    public HeartPickup heartPrefab;
    public Transform player;
    public Camera targetCamera;
    public HeartArrowIndicator arrowIndicator;

    [Header("--- Spawn Area ---")]
    public Vector2 spawnMin = new Vector2(-19f, -21f);
    public Vector2 spawnMax = new Vector2(26f, 15f);
    public float minDistanceFromPlayer = 5f;
    public bool autoFindForbiddenZones = true;
    public Collider2D[] forbiddenZoneColliders;
    public Renderer[] forbiddenZoneRenderers;

    [Header("--- Timing ---")]
    public bool spawnOnStart = true;
    public float respawnDelay = 20f;
    public float heartDisplaySeconds = 20f;
    public float collectGraceSeconds = 10f;

    private HeartPickup currentHeart;
    private float respawnTimer;

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (autoFindForbiddenZones)
        {
            FindForbiddenZones();
        }

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
        if (currentHeart != null)
        {
            if (arrowIndicator != null)
            {
                arrowIndicator.Track(targetCamera, player, currentHeart.Transform);
            }

            return;
        }

        if (arrowIndicator != null)
        {
            arrowIndicator.Hide();
        }

        respawnTimer -= Time.deltaTime;
        if (respawnTimer <= 0f)
        {
            SpawnHeart();
        }
    }

    public void NotifyHeartCollected(HeartPickup heart)
    {
        if (heart != currentHeart) return;

        Destroy(currentHeart.gameObject);
        currentHeart = null;
        respawnTimer = respawnDelay;
        if (arrowIndicator != null) arrowIndicator.Hide();
        if (datingIntroController != null)
        {
            datingIntroController.OpenDatingIntro();
        }
    }

    public void NotifyHeartExpired(HeartPickup heart)
    {
        if (heart != currentHeart) return;

        Destroy(currentHeart.gameObject);
        currentHeart = null;
        respawnTimer = respawnDelay;
        if (arrowIndicator != null) arrowIndicator.Hide();
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
        Vector2 candidate = playerPosition;
        Vector2 fallbackOutsideForbidden = playerPosition;
        bool hasFallbackOutsideForbidden = false;

        for (int i = 0; i < 80; i++)
        {
            candidate = new Vector2(Random.Range(spawnMin.x, spawnMax.x), Random.Range(spawnMin.y, spawnMax.y));
            bool isForbidden = IsForbiddenSpawnPosition(candidate);
            if (!isForbidden && !hasFallbackOutsideForbidden)
            {
                fallbackOutsideForbidden = candidate;
                hasFallbackOutsideForbidden = true;
            }

            if (!isForbidden && Vector2.Distance(candidate, playerPosition) >= minDistanceFromPlayer)
            {
                return candidate;
            }
        }

        return hasFallbackOutsideForbidden ? fallbackOutsideForbidden : playerPosition;
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

    private bool IsForbiddenSpawnPosition(Vector2 position)
    {
        if (forbiddenZoneColliders != null)
        {
            for (int i = 0; i < forbiddenZoneColliders.Length; i++)
            {
                Collider2D zone = forbiddenZoneColliders[i];
                if (zone != null && zone.bounds.Contains(position))
                {
                    return true;
                }
            }
        }

        if (forbiddenZoneRenderers != null)
        {
            for (int i = 0; i < forbiddenZoneRenderers.Length; i++)
            {
                Renderer zone = forbiddenZoneRenderers[i];
                if (zone != null && zone.bounds.Contains(position))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static Collider2D FindCollider(string objectName)
    {
        GameObject zoneObject = GameObject.Find(objectName);
        return zoneObject != null ? zoneObject.GetComponent<Collider2D>() : null;
    }

    private static Renderer FindRenderer(string objectName)
    {
        GameObject zoneObject = GameObject.Find(objectName);
        return zoneObject != null ? zoneObject.GetComponent<Renderer>() : null;
    }
}
