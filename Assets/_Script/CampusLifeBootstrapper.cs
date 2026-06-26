using UnityEngine;

public static class CampusLifeBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        CampusLifeGameManager manager = Object.FindAnyObjectByType<CampusLifeGameManager>();
        if (manager == null)
        {
            GameObject runtimeRoot = new GameObject("CampusLifeRuntime");
            manager = runtimeRoot.AddComponent<CampusLifeGameManager>();
        }

        if (Object.FindAnyObjectByType<CampusLifeHud>() == null)
        {
            manager.gameObject.AddComponent<CampusLifeHud>();
        }

        if (Object.FindAnyObjectByType<CourseRegistrationMinigameController>() == null)
        {
            manager.gameObject.AddComponent<CourseRegistrationMinigameController>();
        }
    }
}
