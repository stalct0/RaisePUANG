using UnityEngine;
using UnityEngine.InputSystem;

public class DriveMiniGameController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dimPanel;
    [SerializeField] private GameObject drivePanel;

    [Header("Game")]
    [SerializeField] private DeliveryGameManager deliveryGameManager;

    [Header("Debug")]
    [SerializeField] private Key debugOpenKey = Key.V;
    [SerializeField] private Key closeKey = Key.Escape;

    private bool isOpen;

    private void Start()
    {
        if (drivePanel != null)
            drivePanel.SetActive(false);
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!isOpen && Keyboard.current != null && Keyboard.current[debugOpenKey].wasPressedThisFrame)
        {
            OpenDriveMiniGame();
        }
#endif

        if (isOpen && Keyboard.current != null && Keyboard.current[closeKey].wasPressedThisFrame)
        {
            CloseDriveMiniGame();
        }
    }

    public void OpenDriveMiniGame()
    {
        if (isOpen) return;

        if (CampusLifeGameManager.Instance == null)
            return;

        if (!CampusLifeGameManager.Instance.IsPlaying)
            return;

        isOpen = true;

        CampusLifeGameManager.Instance.EnterMiniGame();

        if (dimPanel != null)
            dimPanel.SetActive(true);

        if (drivePanel != null)
            drivePanel.SetActive(true);

        if (deliveryGameManager != null)
            deliveryGameManager.StartGame();
    }

    public void CloseDriveMiniGame()
    {
        if (!isOpen) return;

        isOpen = false;

        if (drivePanel != null)
            drivePanel.SetActive(false);

        if (dimPanel != null)
            dimPanel.SetActive(false);

        if (CampusLifeGameManager.Instance != null &&
            CampusLifeGameManager.Instance.IsMiniGame)
        {
            CampusLifeGameManager.Instance.ExitMiniGame();
        }
    }
}