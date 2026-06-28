using System.Collections.Generic;
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

    [Header("Heart Lifetime")]
    public float heartDisplaySeconds = 20f;
    public float collectGraceSeconds = 10f;

    private HeartPickup currentHeart;

    private int scheduledSemester = -1;
    private readonly List<float> spawnTimes = new();
    private int nextSpawnIndex;
    
    private bool firstHeartSpawned;
    private bool firstHeartFailed;

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (novelSceneManager == null)
            novelSceneManager = FindFirstObjectByType<NovelSceneManager>();

        if (autoFindForbiddenZones)
            FindForbiddenZones();
    }

    private void Update()
    {
        if (CampusLifeGameManager.Instance == null)
            return;

        if (!CampusLifeGameManager.Instance.IsPlaying)
            return;

        if (DatingProgressManager.Instance != null &&
            DatingProgressManager.Instance.RomanceSystemLocked)
        {
            return;
        }

        HandleFirstSemesterRequiredHeart();

        if (CampusLifeGameManager.Instance.CurrentSemester == 1)
            return;

        UpdateSemesterSchedule();

        if (currentHeart != null)
        {
            if (arrowIndicator != null)
                arrowIndicator.Track(targetCamera, player, currentHeart.Transform);

            return;
        }

        if (arrowIndicator != null)
            arrowIndicator.Hide();

        TrySpawnBySchedule();
    }

    private void UpdateSemesterSchedule()
    {
        int currentSemester = CampusLifeGameManager.Instance.CurrentSemester;

        if (scheduledSemester == currentSemester)
            return;

        scheduledSemester = currentSemester;
        nextSpawnIndex = 0;
        spawnTimes.Clear();

        int spawnCount = GetHeartSpawnCountForSemester(currentSemester);

        if (spawnCount <= 0)
            return;

        float duration = CampusLifeGameManager.Instance.SemesterDuration;

        if (spawnCount == 1)
        {
            spawnTimes.Add(Random.Range(0f, duration));
        }
        else if (spawnCount == 2)
        {
            spawnTimes.Add(Random.Range(0f, duration * 0.5f));
            spawnTimes.Add(Random.Range(duration * 0.5f, duration));
        }

        spawnTimes.Sort();

        Debug.Log($"[HeartSpawnManager] Semester {currentSemester}, Heart Spawn Count: {spawnCount}");
    }

    private int GetHeartSpawnCountForSemester(int semester)
    {
        // 1-1
        if (semester <= 1)
            return 0;

        // 1-2, 2-1
        if (semester == 2 || semester == 3)
            return 2;

        // 2-2부터
        if (DatingProgressManager.Instance == null)
            return 2;

        int previousSemesterDateCount = DatingProgressManager.Instance.PreviousSemesterDateCount;

        return previousSemesterDateCount >= 2 ? 1 : 2;
    }

    private void TrySpawnBySchedule()
    {
        if (nextSpawnIndex >= spawnTimes.Count)
            return;

        float currentTime = CampusLifeGameManager.Instance.CurrentTime;
        float targetTime = spawnTimes[nextSpawnIndex];

        if (currentTime < targetTime)
            return;

        SpawnHeart();
        nextSpawnIndex++;
    }

    public void NotifyHeartCollected(HeartPickup heart)
    {
        if (heart != currentHeart) return;

        Destroy(currentHeart.gameObject);
        currentHeart = null;

        if (arrowIndicator != null)
            arrowIndicator.Hide();

        if (novelSceneManager == null)
            return;

        if (CampusLifeGameManager.Instance != null &&
            CampusLifeGameManager.Instance.CurrentSemester == 1)
        {
            novelSceneManager.OpenRequiredFirstRomanceIntro(DatingCharacter.ChildhoodFriend);
        }
        else
        {
            novelSceneManager.OpenDatingIntro();
        }
    }

    public void NotifyHeartExpired(HeartPickup heart)
    {
        if (heart != currentHeart)
            return;

        Destroy(currentHeart.gameObject);
        currentHeart = null;

        if (arrowIndicator != null)
            arrowIndicator.Hide();
    }

    private void SpawnHeart()
    {
        if (heartPrefab == null || player == null)
            return;

        Vector2 spawnPosition = FindSpawnPosition();

        currentHeart = Instantiate(heartPrefab, spawnPosition, Quaternion.identity);
        currentHeart.name = "HeartPickup";
        currentHeart.Initialize(this, heartDisplaySeconds, collectGraceSeconds);

        Debug.Log("[HeartSpawnManager] Heart Spawned.");
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
                return candidate;
        }

        return hasFallback ? fallback : playerPosition;
    }

    private bool IsForbiddenSpawnPosition(Vector2 position)
    {
        if (forbiddenZoneColliders != null)
        {
            foreach (Collider2D zone in forbiddenZoneColliders)
            {
                if (zone != null && zone.bounds.Contains(position))
                    return true;
            }
        }

        if (forbiddenZoneRenderers != null)
        {
            foreach (Renderer zone in forbiddenZoneRenderers)
            {
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
    private void HandleFirstSemesterRequiredHeart()
    {
        if (DatingProgressManager.Instance == null)
            return;

        if (DatingProgressManager.Instance.FirstRomanceEventCompleted)
            return;

        if (DatingProgressManager.Instance.RomanceSystemLocked)
            return;

        if (CampusLifeGameManager.Instance.CurrentSemester != 1)
            return;

        float currentTime = CampusLifeGameManager.Instance.CurrentTime;
        float duration = CampusLifeGameManager.Instance.SemesterDuration;

        if (!firstHeartSpawned && currentTime >= duration / 3f)
        {
            SpawnHeart();
            firstHeartSpawned = true;
        }

        if (firstHeartSpawned &&
            currentHeart != null &&
            currentTime >= duration)
        {
            DatingProgressManager.Instance.LockRomanceSystem();
            Destroy(currentHeart.gameObject);
            currentHeart = null;

            if (arrowIndicator != null)
                arrowIndicator.Hide();
        }
    }
    public void NotifySemesterEnded()
    {
        if (CampusLifeGameManager.Instance == null)
            return;

        if (CampusLifeGameManager.Instance.CurrentSemester != 1)
            return;

        if (DatingProgressManager.Instance == null)
            return;

        if (!DatingProgressManager.Instance.FirstRomanceEventCompleted)
        {
            DatingProgressManager.Instance.LockRomanceSystem();

            if (currentHeart != null)
            {
                Destroy(currentHeart.gameObject);
                currentHeart = null;
            }

            if (arrowIndicator != null)
                arrowIndicator.Hide();
        }
    }
}