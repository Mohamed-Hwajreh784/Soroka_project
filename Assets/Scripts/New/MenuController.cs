// Use English for comments in the code
using UnityEngine;

public class MenuController : MonoBehaviour
{
    [Header("Manager References")]
    public ManualModeManager manualModeManager;

    [Header("Main UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsGroup;
    public GameObject manualPanel;

    [Header("Bubble Menu Objects (On Hand)")]
    public GameObject autoBubble;
    public GameObject manualBubble;

    [Header("Visual Effects")]
    public GameObject effectMeshObject;

    public void ResetAndStayInSettings()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.ResetGameWorld();

        if (manualModeManager != null)
            manualModeManager.ResetPlacementData();

        if (effectMeshObject != null) effectMeshObject.SetActive(false);

        TargetObject.CanCollect = false;

        // Reset spawner placement data if active
        var spawnerManager = Object.FindFirstObjectByType<SpawnerManager>();
        if (spawnerManager != null)
        {
            spawnerManager.ResetPlacementData();
        }
    }

    public void ResetAndGoToMainMenu()
    {
        ResetAndStayInSettings();
        SetPanelState(true, false, false);

        if (autoBubble != null) autoBubble.SetActive(false);
        if (manualBubble != null) manualBubble.SetActive(false);
    }

    public void GoToAutoSettings()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.selectedMode = GameMode.Automatic;

        SetPanelState(false, true, false);
        if (autoBubble != null) autoBubble.SetActive(true);
        if (manualBubble != null) manualBubble.SetActive(false);
    }

    public void GoToManualSettings()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.selectedMode = GameMode.Manual;

        SetPanelState(false, false, true);
        if (manualBubble != null) manualBubble.SetActive(true);
        if (autoBubble != null) autoBubble.SetActive(false);
    }

    private void SetPanelState(bool main, bool auto, bool manual)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(main);
        if (settingsGroup != null) settingsGroup.SetActive(auto);
        if (manualPanel != null) manualPanel.SetActive(manual);
    }
}