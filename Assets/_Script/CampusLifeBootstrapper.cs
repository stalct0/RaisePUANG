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

        if (!ShouldShowCampusLifeHud())
        {
            RemoveCampusLifeHud();
        }
    }

    private static bool IsNovelScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        return activeScene.name == "Simulator" || Object.FindFirstObjectByType<NovelSceneManager>() != null;
    }

    private static bool ShouldShowCampusLifeHud()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        CampusLifeHud[] huds = Object.FindObjectsByType<CampusLifeHud>(FindObjectsSortMode.None);
        foreach (CampusLifeHud hud in huds)
        {
            if (hud.gameObject.scene == activeScene)
            {
                return true;
            }
        }

        return false;
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
