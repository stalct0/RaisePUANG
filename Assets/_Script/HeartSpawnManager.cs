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

    [Header("Schedule")]
    [SerializeField] private float firstMeetingSpawnRatio = 0.333f;

    private HeartPickup currentHeart;

    private int scheduledSemester = -1;
    private bool shouldSpawnThisSemester;
    private bool hasSpawnedThisSemester;
    private float scheduledSpawnTime;

    private readonly HashSet<int> randomDateSemesters = new();

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (novelSceneManager == null)
            novelSceneManager = FindFirstObjectByType<NovelSceneManager>();

        if (autoFindForbiddenZones)
            FindForbiddenZones();

        DecideRandomDateSemesters();
    }

    private void Update()
    {
        if (CampusLifeGameManager.Instance == null)
            return;

        if (!CampusLifeGameManager.Instance.IsPlaying)
            return;

        if (DatingProgressManager.Instance != null &&
            DatingProgressManager.Instance.RomanceSystemLocked)
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

        TrySpawnScheduledHeart();
    }

    private void DecideRandomDateSemesters()
    {
        randomDateSemesters.Clear();

        List<int> candidates = new()
        {
            2, // 1-2
            3, // 2-1
            4, // 2-2
            5, // 3-1
            6, // 3-2
            7  // 4-1
        };

        for (int i = 0; i < 3 && candidates.Count > 0; i++)
        {
            int index = Random.Range(0, candidates.Count);
            randomDateSemesters.Add(candidates[index]);
            candidates.RemoveAt(index);
        }

        Debug.Log("[HeartSpawnManager] Random date semesters: " + string.Join(", ", randomDateSemesters));
    }

    private void UpdateSemesterSchedule()
    {
        int currentSemester = CampusLifeGameManager.Instance.CurrentSemester;

        if (scheduledSemester == currentSemester)
            return;

        scheduledSemester = currentSemester;
        shouldSpawnThisSemester = false;
        hasSpawnedThisSemester = false;
        scheduledSpawnTime = -1f;

        float duration = CampusLifeGameManager.Instance.SemesterDuration;

        if (currentSemester == 1)
        {
            if (CanSpawnFirstMeetingHeart())
            {
                shouldSpawnThisSemester = true;
                scheduledSpawnTime = duration * firstMeetingSpawnRatio;
            }

            return;
        }

        if (currentSemester == 8)
        {
            shouldSpawnThisSemester = true;
            scheduledSpawnTime = Random.Range(0f, duration);
            return;
        }

        if (randomDateSemesters.Contains(currentSemester))
        {
            shouldSpawnThisSemester = true;
            scheduledSpawnTime = Random.Range(0f, duration);
        }
    }

    private bool CanSpawnFirstMeetingHeart()
    {
        if (DatingProgressManager.Instance == null)
            return false;

        return !DatingProgressManager.Instance.FirstRomanceEventCompleted &&
               !DatingProgressManager.Instance.RomanceSystemLocked;
    }

    private void TrySpawnScheduledHeart()
    {
        if (!shouldSpawnThisSemester)
            return;

        if (hasSpawnedThisSemester)
            return;

        if (CampusLifeGameManager.Instance.CurrentTime < scheduledSpawnTime)
            return;

        if (!CanSpawnHeartThisSemester())
        {
            hasSpawnedThisSemester = true;
            return;
        }

        SpawnHeart();
        hasSpawnedThisSemester = true;
    }

    private bool CanSpawnHeartThisSemester()
    {
        if (DatingProgressManager.Instance == null)
            return false;

        if (DatingProgressManager.Instance.RomanceSystemLocked)
            return false;

        int semester = CampusLifeGameManager.Instance.CurrentSemester;
        int completedDateCount = DatingProgressManager.Instance.CompletedRegularDateCount;

        if (semester == 1)
        {
            return !DatingProgressManager.Instance.FirstRomanceEventCompleted;
        }

        if (!DatingProgressManager.Instance.FirstRomanceEventCompleted)
            return false;

        if (DatingProgressManager.Instance.DatingEnded)
            return false;

        if (semester == 8)
        {
            return completedDateCount < 3;
        }

        return completedDateCount < 2;
    }

    public void NotifyHeartCollected(HeartPickup heart)
    {
        if (heart != currentHeart)
            return;

        Destroy(currentHeart.gameObject);
        currentHeart = null;

        if (arrowIndicator != null)
            arrowIndicator.Hide();

        if (novelSceneManager == null)
            return;

        if (CampusLifeGameManager.Instance != null &&
            CampusLifeGameManager.Instance.CurrentSemester == 1)
        {
            novelSceneManager.OpenRequiredFirstRomanceIntro(DatingCharacter.None);
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

    public void NotifySemesterEnded()
    {
        if (CampusLifeGameManager.Instance == null)
            return;

        if (CampusLifeGameManager.Instance.CurrentSemester != 1)
            return;

        if (DatingProgressManager.Instance == null)
            return;

        if (DatingProgressManager.Instance.FirstRomanceEventCompleted)
            return;

        DatingProgressManager.Instance.LockRomanceSystem();

        if (currentHeart != null)
        {
            Destroy(currentHeart.gameObject);
            currentHeart = null;
        }

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

        float display = heartDisplaySeconds;
        float grace = collectGraceSeconds;

        if (CampusLifeGameManager.Instance != null &&
            CampusLifeGameManager.Instance.CurrentSemester == 1)
        {
            display = CampusLifeGameManager.Instance.SemesterDuration;
            grace = 9999f;
        }

        currentHeart.Initialize(this, display, grace);

        Debug.Log($"[HeartSpawnManager] Heart spawned in semester {CampusLifeGameManager.Instance.CurrentSemester}");
    }

    private Vector2 FindSpawnPosition()
    {
        Vector2 playerPosition = player.position;
        Vector2 fallback = playerPosition;
        bool hasFallback = false;

        for (int i = 0; i < 80; i++)
        {
            Vector2 candidate = new(
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
}