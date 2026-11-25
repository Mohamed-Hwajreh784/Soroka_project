// Use English for comments in the code
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using Meta.XR.MRUtilityKit;

public enum SideSelection { Both, Left, Right }
public enum DifficultyLevel { Easy, Medium, Hard }

public class SpawnerManager : MonoBehaviour
{
    public static SpawnerManager Instance { get; private set; }

    [Header("Spawn Prefabs")]
    public GameObject coinPrefab;
    public GameObject virusPrefab;
    public GameObject dynamicObstaclePrefab;
    public GameObject chairPrefab;

    [Header("UI & HUD Settings")]
    public GameObject scoreHudPrefab;

    [Header("UI Readouts (UI Panel)")]
    public TextMeshProUGUI chairCountText;
    public TextMeshProUGUI coinCountText;
    public TextMeshProUGUI virusCountText;

    [Header("Settings & Selection")]
    public LayerMask obstacleLayers;
    public GameObject selectionMenu;
    public GameObject secondaryMenuPanel;

    public SideSelection currentSide = SideSelection.Both;
    public DifficultyLevel currentDifficulty = DifficultyLevel.Medium;

    private List<GameObject> spawnedObjects = new List<GameObject>();
    private float sessionStartTime = 0f;
    private int totalCoinsSpawned = 0;
    private int virusesHitInSession = 0;
    private int obstaclesHitInSession = 0;

    private bool isAutoModeActive = false;
    private GameObject currentScoreHUDInstance;

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

    // Direct methods without string dependency
    public void SetSideLeft()
    {
        currentSide = SideSelection.Left;
        Debug.Log("[SpawnerManager] Side set to: LEFT");
    }

    public void SetSideRight()
    {
        currentSide = SideSelection.Right;
        Debug.Log("[SpawnerManager] Side set to: RIGHT");
    }

    public void SetSideBoth()
    {
        currentSide = SideSelection.Both;
        Debug.Log("[SpawnerManager] Side set to: BOTH");
    }

    public void SetDifficultyEasy()
    {
        currentDifficulty = DifficultyLevel.Easy;
        Debug.Log("[SpawnerManager] Difficulty set to: EASY");
    }

    public void SetDifficultyMedium()
    {
        currentDifficulty = DifficultyLevel.Medium;
        Debug.Log("[SpawnerManager] Difficulty set to: MEDIUM");
    }

    public void SetDifficultyHard()
    {
        currentDifficulty = DifficultyLevel.Hard;
        Debug.Log("[SpawnerManager] Difficulty set to: HARD");
    }

    public void SetDifficulty(string diff)
    {
        if (string.IsNullOrEmpty(diff)) return;

        string cleanDiff = System.Text.RegularExpressions.Regex.Replace(diff, "<.*?>", string.Empty).Trim();

        if (System.Enum.TryParse(cleanDiff, true, out DifficultyLevel parsedDiff))
        {
            currentDifficulty = parsedDiff;
            Debug.Log($"[SpawnerManager] Difficulty set via string to: {currentDifficulty}");
        }
        else
        {
            Debug.LogWarning($"[SpawnerManager] Failed to parse difficulty: '{diff}'");
        }
    }

    public void StartGame()
    {
        MRUKRoom room = MRUK.Instance != null ? MRUK.Instance.GetCurrentRoom() : null;
        if (room == null)
        {
            Debug.LogWarning("[SpawnerManager] MRUK Room not ready!");
            return;
        }

        isAutoModeActive = true;

        ResetMetrics();
        ClearAllSpawnedObjects();

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetGame();
        }

        TargetObject.CanCollect = true;

        if (selectionMenu != null) selectionMenu.SetActive(false);
        if (secondaryMenuPanel != null) secondaryMenuPanel.SetActive(false);

        SpawnChairsInRoom(room);
        SpawnCoinsInRoom(room);
        SpawnVirusesInRoom(room);

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SetTotalCoins(totalCoinsSpawned);
        }

        SpawnScoreHUD();

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.StartGameLogic();
        }
    }

    private void ResetMetrics()
    {
        sessionStartTime = Time.time;
        totalCoinsSpawned = 0;
        virusesHitInSession = 0;
        obstaclesHitInSession = 0;
    }

    private void SpawnChairsInRoom(MRUKRoom room)
    {
        int count = GetValueFromText(chairCountText);
        if (chairPrefab != null && count > 0)
        {
            SpawnRandomGroup(room, chairPrefab, count);
        }
    }

    private void SpawnCoinsInRoom(MRUKRoom room)
    {
        int coinCount = GetValueFromText(coinCountText);
        if (coinPrefab != null && coinCount > 0)
        {
            totalCoinsSpawned = SpawnRandomGroup(room, coinPrefab, coinCount);
        }
    }

    private void SpawnVirusesInRoom(MRUKRoom room)
    {
        int virusCount = GetValueFromText(virusCountText);
        if (virusPrefab != null && virusCount > 0)
        {
            SpawnRandomGroup(room, virusPrefab, virusCount);
        }
    }

    private int SpawnRandomGroup(MRUKRoom room, GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return 0;
        int spawned = 0;
        int attempts = 0;
        LabelFilter floorFilter = new LabelFilter(MRUKAnchor.SceneLabels.FLOOR);

        Transform mainCam = Camera.main != null ? Camera.main.transform : null;

        while (spawned < count && attempts < 1000)
        {
            attempts++;

            if (room.GenerateRandomPositionOnSurface(MRUK.SurfaceType.FACING_UP, 0.1f, floorFilter, out Vector3 randomPos, out _))
            {
                if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 0.5f, NavMesh.AllAreas))
                {
                    Vector3 finalPos = hit.position;
                    finalPos.y = room.transform.position.y;

                    if (mainCam != null && currentSide != SideSelection.Both)
                    {
                        Vector3 dirToPoint = (finalPos - mainCam.position).normalized;
                        float dotProduct = Vector3.Dot(mainCam.right, dirToPoint);

                        if (currentSide == SideSelection.Left && dotProduct > 0.1f) continue;
                        if (currentSide == SideSelection.Right && dotProduct < -0.1f) continue;
                    }

                    if (Physics.CheckSphere(finalPos, 0.5f, obstacleLayers)) continue;

                    bool tooClose = false;
                    float minDistance = currentDifficulty == DifficultyLevel.Hard ? 0.4f : 0.6f;

                    foreach (GameObject existing in spawnedObjects)
                    {
                        if (existing != null && Vector3.Distance(finalPos, existing.transform.position) < minDistance)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (tooClose) continue;

                    GameObject obj = Instantiate(prefab, finalPos, Quaternion.identity);
                    obj.layer = LayerMask.NameToLayer("VirtualObjects");

                    spawnedObjects.Add(obj);
                    spawned++;
                }
            }
        }

        return spawned;
    }

    public void RegisterVirusHit()
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
            SessionUIManager.Instance.CreateDynamicHUD("Auto Mode");
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

    public void CompleteAutoSession(int finalScore, int coinsCollected)
    {
        float sessionDuration = Time.time - sessionStartTime;
        TargetObject.CanCollect = false;

        if (isAutoModeActive && RehabSessionReporter.Instance != null)
        {
            RehabSessionReporter.Instance.RecordAndShowStageReport(
                "Auto Mode",
                finalScore,
                coinsCollected,
                totalCoinsSpawned,
                virusesHitInSession,
                obstaclesHitInSession,
                sessionDuration,
                currentSide.ToString(),
                currentDifficulty.ToString()
            );

            RehabSessionReporter.Instance.ShowFullSessionSummaryUI();
            RehabSessionReporter.Instance.ExportToCSV();
        }

        isAutoModeActive = false;
        ClearAllSpawnedObjects();
        DestroyScoreHUD();
    }

    public void ResetPlacementData()
    {
        isAutoModeActive = false;
        TargetObject.CanCollect = false;
        ClearAllSpawnedObjects();
        DestroyScoreHUD();
        ResetMetrics();

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetGame();
        }
    }

    private void ClearAllSpawnedObjects()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedObjects.Clear();
    }

    private int GetValueFromText(TextMeshProUGUI t) => (t != null && int.TryParse(t.text, out int r)) ? r : 0;
}