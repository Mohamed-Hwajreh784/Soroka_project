// Use English for comments in the code
using UnityEngine;
using TMPro;

public class SessionController : MonoBehaviour
{
    public static SessionController Instance { get; private set; }

    [Header("UI & Menus")]
    public MenuController menuController;
    public GameObject blueMenuObject;
    public GameObject gameplayCanvas;
    public TMP_Text scoreText;

    [Header("Session Tracking")]
    public bool isAutomaticMode = true; // Set this via your UI buttons

    private int currentScore = 0;
    private int totalCoins = 0;
    private int collectedCoins = 0;
    private bool isSessionActive = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Call this from SpawnerManager right before spawning objects automatically
    public void StartAutomaticSession(int coinCount)
    {
        isAutomaticMode = true;
        totalCoins = coinCount;
        collectedCoins = 0;
        currentScore = 0;
        isSessionActive = true;

        if (gameplayCanvas != null) gameplayCanvas.SetActive(true);
        if (blueMenuObject != null) blueMenuObject.SetActive(false);

        UpdateUI();
        Debug.Log($"[SessionController] Auto Session Started with {totalCoins} coins.");
    }

    // Call this from your target script (TargetObject) when a coin is hit
    public void RegisterCoinCollected()
    {
        if (!isSessionActive) return;

        collectedCoins++;
        currentScore += 1;
        UpdateUI();

        Debug.Log($"[SessionController] Coin Collected! Progress: {collectedCoins}/{totalCoins}");

        if (totalCoins > 0 && collectedCoins >= totalCoins)
        {
            if (isAutomaticMode)
            {
                EndSessionAndReturn();
            }
            else
            {
                Debug.Log("[SessionController] Manual mode active. Clinician controls exit.");
            }
        }
    }

    public void RegisterPenalty()
    {
        if (!isSessionActive) return;
        currentScore = Mathf.Max(0, currentScore - 1); // Prevent negative clinical score
        UpdateUI();
    }

    public void EndSessionAndReturn()
    {
        isSessionActive = false;
        Debug.Log("[SessionController] Session Finished. Returning to Main Menu.");

        // Clean up UI instantly
        if (gameplayCanvas != null) gameplayCanvas.SetActive(false);

        if (menuController != null)
        {
            menuController.ResetAndGoToMainMenu();
        }
        else if (blueMenuObject != null)
        {
            blueMenuObject.SetActive(true);
        }
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {currentScore} (Coins: {collectedCoins}/{totalCoins})";
        }
    }
}