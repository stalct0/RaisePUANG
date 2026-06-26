using UnityEngine;

public static class CampusLifeBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        CampusLifeGameManager manager = Object.FindFirstObjectByType<CampusLifeGameManager>();
        if (manager == null)
        {
            GameObject runtimeRoot = new GameObject("CampusLifeRuntime");
            manager = runtimeRoot.AddComponent<CampusLifeGameManager>();
        }

        if (Object.FindFirstObjectByType<CampusLifeHud>() == null)
        {
            manager.gameObject.AddComponent<CampusLifeHud>();
        }
    }
}
