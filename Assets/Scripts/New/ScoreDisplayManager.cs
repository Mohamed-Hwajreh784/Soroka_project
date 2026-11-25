// Use English for comments in the code
using TMPro;
using UnityEngine;

public class ScoreDisplayManager : MonoBehaviour
{
    public static ScoreDisplayManager Instance { get; private set; }

    private TextMeshProUGUI floatingHUDText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegisterFloatingHUD(TextMeshProUGUI textComponent)
    {
        floatingHUDText = textComponent;
        UpdateHUDDisplay();
    }

    private void Update()
    {
        // Continuously refresh score display on every frame during gameplay
        if (floatingHUDText != null && ScoreManager.Instance != null)
        {
            UpdateHUDDisplay();
        }
    }

    public void UpdateHUDDisplay()
    {
        if (floatingHUDText == null || ScoreManager.Instance == null) return;

        int score = ScoreManager.Instance.GetCurrentScore();
        int collected = ScoreManager.Instance.GetCollectedCoins();

        // Updates the live floating UI string
        floatingHUDText.text = $"Score: {score} | Coins: {collected}";
    }
}