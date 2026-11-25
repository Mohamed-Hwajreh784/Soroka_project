// Use English for comments in the code
using UnityEngine;
using System;
using System.Collections;

public enum GameMode { Manual, Automatic }

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;
    public GameMode selectedMode;

    public bool isAutoMode => selectedMode == GameMode.Automatic;

    [Header("External References")]
    public MenuController menuController;

    // Events for UI communication
    public static event Action<int, int, int> OnScoreUpdated; // score, collectedCoins, totalCoins
    public static event Action<Color> OnScoreFeedbackTriggered; // success/penalty color
    public static event Action OnGameReset;

    private int score = 0;
    private int totalCoins = 0;
    private int collectedCoins = 0;
    private bool isGameActive = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        TargetObject.OnObjectHit += HandleObjectHit;
    }

    private void OnDisable()
    {
        TargetObject.OnObjectHit -= HandleObjectHit;
    }

    public void SetTotalCoins(int amount)
    {
        totalCoins = amount;
        NotifyScoreChange();
    }

    private void HandleObjectHit(string objectTag)
    {
        if (!isGameActive) return;

        string cleanTag = objectTag.ToLower().Trim();

        switch (cleanTag)
        {
            case "coin":
                score += 1;
                collectedCoins++;
                OnScoreFeedbackTriggered?.Invoke(Color.green);
                CheckWinCondition();
                break;

            case "virus":
                score -= 1;
                OnScoreFeedbackTriggered?.Invoke(Color.red);

                if (ManualModeManager.Instance != null)
                {
                    ManualModeManager.Instance.RegisterVirusHit();
                }
                break;

            case "chair":
            case "dynamicobstacle":
                score -= 1;
                OnScoreFeedbackTriggered?.Invoke(Color.red);

                if (ManualModeManager.Instance != null)
                {
                    ManualModeManager.Instance.RegisterObstacleHit();
                }
                break;
        }

        NotifyScoreChange();
    }

    private void NotifyScoreChange()
    {
        OnScoreUpdated?.Invoke(score, collectedCoins, totalCoins);
    }

    public void StartGameLogic()
    {
        score = 0;
        collectedCoins = 0;
        isGameActive = true;
        TargetObject.CanCollect = true;

        int finalCount = GameObject.FindGameObjectsWithTag("Coin").Length;
        SetTotalCoins(finalCount);
    }

    public void EndStageManually()
    {
        if (!isGameActive) return;

        isGameActive = false;
        TargetObject.CanCollect = false;

        if (ManualModeManager.Instance != null)
        {
            ManualModeManager.Instance.CompleteManualSession(score, collectedCoins);
        }

        if (SessionUIManager.Instance != null)
        {
            SessionUIManager.Instance.DisplayCompletionMessage($"STAGE ENDED\nFinal Score: {score}\nCoins: {collectedCoins}/{totalCoins}");
            SessionUIManager.Instance.HideEndStageButton();
        }

        StartCoroutine(EndStageCleanupRoutine());
    }

    private IEnumerator EndStageCleanupRoutine()
    {
        yield return new WaitForSeconds(3f);

        // Clear score text and destroy HUDs before returning to menu
        if (SessionUIManager.Instance != null)
        {
            SessionUIManager.Instance.ClearCompletionMessage();
            SessionUIManager.Instance.DestroyDynamicHUD();
        }

        if (ManualModeManager.Instance != null)
        {
            ManualModeManager.Instance.DestroyScoreHUD();
        }

        if (menuController != null)
        {
            menuController.ResetAndGoToMainMenu();
        }

        OnGameReset?.Invoke();
    }

    void CheckWinCondition()
    {
        if (totalCoins > 0 && collectedCoins >= totalCoins)
        {
            StartCoroutine(FinishSessionRoutine());
        }
    }

    IEnumerator FinishSessionRoutine()
    {
        isGameActive = false;
        TargetObject.CanCollect = false;

        if (ManualModeManager.Instance != null)
        {
            ManualModeManager.Instance.CompleteManualSession(score, collectedCoins);
        }

        // Display final score on screen for 4 seconds
        if (SessionUIManager.Instance != null)
        {
            string summaryText = $"STAGE COMPLETED!\nFinal Score: {score}\nCoins Collected: {collectedCoins}/{totalCoins}";
            SessionUIManager.Instance.DisplayCompletionMessage(summaryText);
            SessionUIManager.Instance.HideEndStageButton();
        }

        yield return new WaitForSeconds(4f);

        // Completely hide/destroy text and HUD before transitioning to main menu
        if (SessionUIManager.Instance != null)
        {
            SessionUIManager.Instance.ClearCompletionMessage();
            SessionUIManager.Instance.DestroyDynamicHUD();
        }

        if (ManualModeManager.Instance != null)
        {
            ManualModeManager.Instance.DestroyScoreHUD();
        }

        if (menuController != null)
        {
            menuController.ResetAndGoToMainMenu();
        }

        OnGameReset?.Invoke();
    }

    public void AddToScore(int amount)
    {
        if (!isGameActive) return;

        score += amount;
        NotifyScoreChange();
    }

    public void ResetGameWorld()
    {
        string[] tagsToClear = { "Coin", "Virus", "Chair", "DynamicObstacle" };
        foreach (string tag in tagsToClear)
        {
            foreach (GameObject obj in GameObject.FindGameObjectsWithTag(tag)) Destroy(obj);
        }

        score = 0;
        totalCoins = 0;
        collectedCoins = 0;
        isGameActive = false;
        TargetObject.CanCollect = false;

        if (ManualModeManager.Instance != null)
        {
            ManualModeManager.Instance.DestroyScoreHUD();
        }

        if (SessionUIManager.Instance != null)
        {
            SessionUIManager.Instance.HideEndStageButton();
            SessionUIManager.Instance.ClearCompletionMessage();
            SessionUIManager.Instance.DestroyDynamicHUD();
        }

        OnGameReset?.Invoke();
    }
}