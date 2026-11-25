// Use English for comments in the code
using UnityEngine;
using TMPro;

public class SessionUIManager : MonoBehaviour
{
    public static SessionUIManager Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI completionText; // The text component showing final score / stage end
    public GameObject endStageButton;
    public GameObject dynamicHUDObject;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void DisplayCompletionMessage(string message)
    {
        if (completionText != null)
        {
            completionText.text = message;
            completionText.gameObject.SetActive(true);
        }
    }

    public void ClearCompletionMessage()
    {
        if (completionText != null)
        {
            completionText.text = "";
            completionText.gameObject.SetActive(false);
        }
    }

    public void HideEndStageButton()
    {
        if (endStageButton != null)
        {
            endStageButton.SetActive(false);
        }
    }

    public void ShowEndStageButton()
    {
        if (endStageButton != null)
        {
            endStageButton.SetActive(true);
        }
    }

    public void DestroyDynamicHUD()
    {
        ClearCompletionMessage();

        if (dynamicHUDObject != null)
        {
            Destroy(dynamicHUDObject);
        }
    }

    public void CreateDynamicHUD(string modeName)
    {
        // Placeholder method for dynamic HUD creation if prefab is not directly assigned
    }
}