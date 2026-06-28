using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class DriveMiniGameController : MonoBehaviour
{
    [SerializeField] private TMP_Text resultText;
    
    [Header("Common")]
    [SerializeField] private GameObject dimPanel;
    [SerializeField] private GameObject drivePanel;

    [Header("Views")]
    [SerializeField] private GameObject introView;
    [SerializeField] private GameObject gameView;
    [SerializeField] private GameObject resultView;
    
    [Header("Game")]
    [SerializeField] private GameObject deliveryGameArea;
    [SerializeField] private DeliveryGameManager deliveryGameManager;
    
    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button noButton;
    [SerializeField] private Button closeButton;
    
    [Header("Debug")]
    [SerializeField] private Key debugOpenKey = Key.V;

    private bool isOpen;

    private void Start()
    {
        if (startButton != null)
            startButton.onClick.AddListener(StartMiniGame);

        if (noButton != null)
            noButton.onClick.AddListener(CancelIntro);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseMiniGame);

        CloseImmediate();
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!isOpen && Keyboard.current != null && Keyboard.current[debugOpenKey].wasPressedThisFrame)
        {
            OpenMiniGame();
        }
#endif
    }

    public void OpenMiniGame()
    {
        if (CampusLifeGameManager.Instance == null) return;
        if (!CampusLifeGameManager.Instance.IsPlaying) return;
        if (noButton != null)
            noButton.gameObject.SetActive(true);
        isOpen = true;

        CampusLifeGameManager.Instance.EnterMiniGame();

        dimPanel.SetActive(true);
        drivePanel.SetActive(true);

        introView.SetActive(true);
        gameView.SetActive(false);
        resultView.SetActive(false);

        closeButton.gameObject.SetActive(false);
    }

    private void StartMiniGame()
    {
        if (deliveryGameManager != null && !deliveryGameManager.CanStartGame)
        {
            if (introView != null) introView.SetActive(false);
            if (gameView != null) gameView.SetActive(false);
            if (resultView != null) resultView.SetActive(true);
            if (resultText != null)
                resultText.text = deliveryGameManager.GetRestartCooldownMessage();
            if (closeButton != null)
                closeButton.gameObject.SetActive(true);
            if (noButton != null)
                noButton.gameObject.SetActive(false);
            return;
        }

        if (introView != null) introView.SetActive(false);
        if (gameView != null) gameView.SetActive(true);
        if (resultView != null) resultView.SetActive(false);

        if (deliveryGameArea != null)
            deliveryGameArea.SetActive(true);

        if (closeButton != null)
            closeButton.gameObject.SetActive(false);
        if (deliveryGameManager != null)
            deliveryGameManager.StartGame();
        if (noButton != null)
            noButton.gameObject.SetActive(false);
    }
    private void CancelIntro()
    {
        CloseImmediate();

        if (CampusLifeGameManager.Instance != null &&
            CampusLifeGameManager.Instance.IsMiniGame)
        {
            CampusLifeGameManager.Instance.ExitMiniGame();
        }
    }
    public void ShowResult(string result)
    {
        if (deliveryGameArea != null)
            deliveryGameArea.SetActive(false);

        if (gameView != null)
            gameView.SetActive(false);

        if (resultView != null)
            resultView.SetActive(true);

        if (resultText != null)
            resultText.text = result;

        if (closeButton != null)
            closeButton.gameObject.SetActive(true);
    }
    
    private void CloseMiniGame()
    {
        CloseImmediate();

        if (CampusLifeGameManager.Instance != null &&
            CampusLifeGameManager.Instance.IsMiniGame)
        {
            CampusLifeGameManager.Instance.ExitMiniGame();
        }
    }

    private void CloseImmediate()
    {
        isOpen = false;
        if (noButton != null)
            noButton.gameObject.SetActive(false);
        if (dimPanel != null) dimPanel.SetActive(false);
        if (drivePanel != null) drivePanel.SetActive(false);
        if (introView != null) introView.SetActive(false);
        if (gameView != null) gameView.SetActive(false);
        if (resultView != null) resultView.SetActive(false);
    }
}
