using UnityEngine;
using UnityEngine.SceneManagement;

public static class CampusLifeBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (IsNovelScene())
        {
            RemoveCampusLifeHud();
            return;
        }

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

    private static bool IsNovelScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        return activeScene.name == "Simulator" || Object.FindFirstObjectByType<NovelSceneManager>() != null;
    }

    private static void RemoveCampusLifeHud()
    {
        CampusLifeHud[] huds = Object.FindObjectsByType<CampusLifeHud>(FindObjectsSortMode.None);
        foreach (CampusLifeHud hud in huds)
        {
            Object.Destroy(hud);
        }

        GameObject hudCanvas = GameObject.Find("CampusLifeHudCanvas");
        if (hudCanvas != null)
        {
            Object.Destroy(hudCanvas);
        }
    }
}
