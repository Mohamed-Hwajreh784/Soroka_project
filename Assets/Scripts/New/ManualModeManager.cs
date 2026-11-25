// Use English for comments in the code
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using Meta.XR.MRUtilityKit;

public class ManualModeManager : MonoBehaviour
{
    public static ManualModeManager Instance { get; private set; }

    [Header("Auto Spawn (Chairs)")]
    public GameObject[] autoSpawnPrefabs;
    public TextMeshProUGUI chairCountText;

    [Header("Manual Tools")]
    public GameObject[] manualToolPrefabs;
    public TextMeshProUGUI selectedObjectText;

    [Header("Settings")]
    public LayerMask obstacleLayers;
    public GameObject selectionMenu;
    public float wallClearance = 0.4f;

    [Header("Score HUD Settings")]
    public GameObject scoreHudPrefab;

    [Header("End Stage Control")]
    public GameObject endStageButton;

    private List<GameObject> placedObjects = new List<GameObject>();
    private int selectedToolIndex = 0;
    private bool thumbstickMoved = false;
    private bool isEditModeActive = false;
    private bool isLocked = false;
    private bool scoreboardPlaced = false;

    // Track active instantiated Score HUD to allow destroying it on menu reset
    private GameObject currentScoreHUDInstance;

    // Session Metrics Tracking
    private float sessionStartTime = 0f;
    private int totalCoinsPlaced = 0;
    private int virusesHitInSession = 0;
    private int obstaclesHitInSession = 0;

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

    void Update()
    {
        if (isEditModeActive && !isLocked)
        {
            HandleJoystickNavigation();
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch)) SpawnObjectInFrontOfHand();

            // Pressing Button Two (B) locks the environment and activates game collection time
            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch)) StartCoroutine(LockEnvironmentRoutine());
        }
    }

    public void StartGame()
    {
        MRUKRoom room = MRUK.Instance != null ? MRUK.Instance.GetCurrentRoom() : null;
        if (room == null) return;

        ResetMetrics();

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetGame();
        }

        SpawnChairsInRoom(room);

        if (selectionMenu != null) selectionMenu.SetActive(false);

        if (endStageButton != null)
        {
            endStageButton.SetActive(true);
        }

        if (SessionUIManager.Instance != null)
        {
            SessionUIManager.Instance.ShowEndStageButton();
        }

        if (selectedObjectText != null) selectedObjectText.gameObject.SetActive(false);
        isEditModeActive = false;
        isLocked = false;
    }

    public void ActivateManualPlacement()
    {
        isEditModeActive = true;
        isLocked = false;

        // Disable object collection while editing/placing objects
        TargetObject.CanCollect = false;

        ResetMetrics();

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetGame();
        }

        if (endStageButton != null)
        {
            endStageButton.SetActive(false);
        }

        if (selectedObjectText != null)
        {
            selectedObjectText.gameObject.SetActive(true);
            UpdateToolUI();
        }
    }

    private void ResetMetrics()
    {
        sessionStartTime = Time.time;
        totalCoinsPlaced = 0;
        virusesHitInSession = 0;
        obstaclesHitInSession = 0;
    }

    private void SpawnChairsInRoom(MRUKRoom room)
    {
        int singleCount = GetValueFromText(chairCountText);

        if (autoSpawnPrefabs.Length > 0) SpawnRandomGroup(room, autoSpawnPrefabs[0], singleCount);
    }

    private void SpawnRandomGroup(MRUKRoom room, GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;
        int spawned = 0;
        int attempts = 0;
        LabelFilter floorFilter = new LabelFilter(MRUKAnchor.SceneLabels.FLOOR);

        while (spawned < count && attempts < 1000)
        {
            attempts++;

            if (room.GenerateRandomPositionOnSurface(MRUK.SurfaceType.FACING_UP, 0.1f, floorFilter, out Vector3 randomPos, out _))
            {
                if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 0.5f, NavMesh.AllAreas))
                {
                    Vector3 finalPos = hit.position;
                    finalPos.y = room.transform.position.y;

                    if (Physics.CheckSphere(finalPos, 0.5f, obstacleLayers)) continue;

                    bool tooClose = false;
                    foreach (GameObject existing in placedObjects)
                    {
                        if (existing != null && Vector3.Distance(finalPos, existing.transform.position) < 0.6f)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (tooClose) continue;

                    GameObject obj = Instantiate(prefab, finalPos, Quaternion.identity);
                    obj.layer = LayerMask.NameToLayer("VirtualObjects");

                    placedObjects.Add(obj);
                    spawned++;
                }
            }
        }
    }

    public void ResetPlacementData()
    {
        scoreboardPlaced = false;
        TargetObject.CanCollect = false;
        ClearAllPlacedObjects();
        DestroyScoreHUD();
        isEditModeActive = false;
        isLocked = false;
        ResetMetrics();

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetGame();
        }

        if (selectedObjectText != null) selectedObjectText.gameObject.SetActive(false);

        if (endStageButton != null)
        {
            endStageButton.SetActive(false);
        }
    }

    void SpawnObjectInFrontOfHand()
    {
        ClearMissingObjects();
        if (manualToolPrefabs.Length == 0) return;

        if (selectedToolIndex < 0 || selectedToolIndex >= manualToolPrefabs.Length) return;

        GameObject selectedPrefab = manualToolPrefabs[selectedToolIndex];
        bool isScoreboard = selectedPrefab.name.ToLower().Contains("score");

        if (isScoreboard && scoreboardPlaced) return;

        Vector3 spawnPos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch) +
                          (OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch) * Vector3.forward * 0.08f);

        if (Physics.CheckSphere(spawnPos, 0.02f, obstacleLayers)) return;

        GameObject newObj = Instantiate(selectedPrefab, spawnPos, OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch));
        newObj.layer = LayerMask.NameToLayer("VirtualObjects");
        newObj.tag = selectedPrefab.tag;
        placedObjects.Add(newObj);

        if (selectedPrefab.name.ToLower().Contains("coin") || newObj.CompareTag("Coin"))
        {
            totalCoinsPlaced++;
        }

        if (isScoreboard) scoreboardPlaced = true;
    }

    void HandleJoystickNavigation()
    {
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
        if (Mathf.Abs(input.x) > 0.7f && !thumbstickMoved)
        {
            selectedToolIndex = (input.x > 0) ? (selectedToolIndex + 1) % manualToolPrefabs.Length : (selectedToolIndex - 1 + manualToolPrefabs.Length) % manualToolPrefabs.Length;
            UpdateToolUI();
            thumbstickMoved = true;
        }
        else if (Mathf.Abs(input.x) < 0.2f) thumbstickMoved = false;
    }

    void UpdateToolUI()
    {
        if (selectedObjectText == null || manualToolPrefabs.Length == 0) return;

        if (selectedToolIndex < 0 || selectedToolIndex >= manualToolPrefabs.Length) return;

        string cleanName = manualToolPrefabs[selectedToolIndex].name;

        if (cleanName.ToLower().Contains("coin"))
        {
            cleanName = "Coin";
        }
        else if (cleanName.ToLower().Contains("virus") || cleanName.ToLower().Contains("virous"))
        {
            cleanName = "Virus";
        }
        else if (cleanName.ToLower().Contains("obstacle") || cleanName.ToLower().Contains("dynamic"))
        {
            cleanName = "Dynamic Obstacle";
        }

        selectedObjectText.text = cleanName;
    }

    public void RegisterVirusHit(GameObject virusObj = null)
    {
        virusesHitInSession++;
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SubtractScore(1);
        }
    }

    public void RegisterObstacleHit()
    {
        obstaclesHitInSession++;
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SubtractScore(1);
        }
    }

    private void ClearMissingObjects() => placedObjects.RemoveAll(item => item == null);

    private void ClearAllPlacedObjects()
    {
        foreach (GameObject obj in placedObjects)
        {
            if (obj != null) Destroy(obj);
        }
        placedObjects.Clear();
    }

    private int GetValueFromText(TextMeshProUGUI t) => (t != null && int.TryParse(t.text, out int r)) ? r : 0;

    IEnumerator LockEnvironmentRoutine()
    {
        isLocked = true;
        isEditModeActive = false;
        sessionStartTime = Time.time;

        // 1. Enable collection when game phase starts
        TargetObject.CanCollect = true;

        // 2. Initialize ScoreManager with actual manual coins placed
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SetTotalCoins(totalCoinsPlaced);
        }

        // 3. Spawn floating Score HUD
        SpawnScoreHUD();

        if (endStageButton != null)
        {
            endStageButton.SetActive(true);
        }
        else if (SessionUIManager.Instance != null)
        {
            SessionUIManager.Instance.ShowEndStageButton();
        }

        if (GameStateManager.Instance != null) GameStateManager.Instance.StartGameLogic();

        selectedObjectText.text = "<color=red>LOCKED</color>";
        yield return new WaitForSeconds(2f);
        selectedObjectText.gameObject.SetActive(false);
    }

    public void TriggerEndStage()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.EndStageManually();
        }
        else
        {
            int currentScore = ScoreManager.Instance != null ? ScoreManager.Instance.GetCurrentScore() : 0;
            int collectedCoins = ScoreManager.Instance != null ? ScoreManager.Instance.GetCollectedCoins() : 0;
            CompleteManualSession(currentScore, collectedCoins);
        }
    }

    public void CompleteManualSession(int finalScore, int coinsCollected)
    {
        float sessionDuration = Time.time - sessionStartTime;
        TargetObject.CanCollect = false;

        if (endStageButton != null)
        {
            endStageButton.SetActive(false);
        }
        else if (SessionUIManager.Instance != null)
        {
            SessionUIManager.Instance.HideEndStageButton();
        }

        // Ensure Side and Difficulty are strictly reported as "NaN" in Manual Mode
        if (RehabSessionReporter.Instance != null)
        {
            RehabSessionReporter.Instance.RecordAndShowStageReport(
                "Manual Mode",
                finalScore,
                coinsCollected,
                totalCoinsPlaced,
                virusesHitInSession,
                obstaclesHitInSession,
                sessionDuration,
                "NaN",
                "NaN"
            );

            RehabSessionReporter.Instance.ShowFullSessionSummaryUI();
            RehabSessionReporter.Instance.ExportToCSV();
        }

        ClearAllPlacedObjects();
        DestroyScoreHUD();
    }

    private void SpawnScoreHUD()
    {
        DestroyScoreHUD();

        if (scoreHudPrefab != null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Vector3 spawnPos = mainCam.transform.position + (mainCam.transform.forward * 1.2f);
                Quaternion spawnRot = Quaternion.LookRotation(mainCam.transform.forward);

                currentScoreHUDInstance = Instantiate(scoreHudPrefab, spawnPos, spawnRot);
                currentScoreHUDInstance.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

                TextMeshProUGUI hudText = currentScoreHUDInstance.GetComponentInChildren<TextMeshProUGUI>();

                if (hudText != null && ScoreDisplayManager.Instance != null)
                {
                    ScoreDisplayManager.Instance.RegisterFloatingHUD(hudText);
                }
            }
        }
        else if (SessionUIManager.Instance != null)
        {
            SessionUIManager.Instance.CreateDynamicHUD("Manual Mode");
        }
    }

    public void DestroyScoreHUD()
    {
        if (currentScoreHUDInstance != null)
        {
            Destroy(currentScoreHUDInstance);
            currentScoreHUDInstance = null;
        }
    }
}