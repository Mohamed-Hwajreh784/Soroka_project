// Use English for comments in the code
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class PresetRehabManager : MonoBehaviour
{
    public enum GamePhase { None, Easy, CognitiveMedium, UnlimitedHard, Finished }

    [Header("UI Setup")]
    public GameObject mainMenuPanel;

    [Header("Audio Clips")]
    public AudioClip coinSound;
    public AudioClip virusSound;

    [Header("End Stage Button UI")]
    public GameObject endStageCanvas;

    [Header("End Stage Button Positioning")]
    public float endStageButtonDistance = 1.5f;

    [Header("Prefabs to Spawn")]
    public GameObject yellowCoinPrefab;
    public GameObject greenBigCoinPrefab;
    public GameObject blueSmallCoinPrefab;
    public GameObject virusPrefab;
    public GameObject staticObstaclePrefab;

    [Header("Coin Score Values")]
    public int yellowCoinValue = 1;
    public int greenCoinValue = 3;
    public int blueCoinValue = 5;

    [Header("Level Durations (Seconds)")]
    public float easyLevelDuration = 15f;
    public float mediumLevelDuration = 30f;

    [Header("Calibration Settings")]
    public float seatedThresholdHeight = 1.35f;

    private GamePhase currentPhase = GamePhase.None;
    private List<GameObject> activeLevelObjects = new List<GameObject>();
    private List<GameObject> activeChairs = new List<GameObject>();
    private int coinsRemainingInLevel = 0;
    private int totalScore = 0;
    private int stageScore = 0;
    private float timeRemaining = 0f;
    private float timeElapsedInLevel = 0f;
    private bool isTimerRunning = false;
    private bool isTransitioning = false;

    private int totalCoinsSpawnedInLevel = 0;
    private int virusesHitInLevel = 0;
    private int collisionsInLevel = 0;

    private GameObject dynamicHUDCanvas;
    private TextMeshProUGUI dynamicPhaseText;
    private TextMeshProUGUI dynamicTimerText;
    private TextMeshProUGUI dynamicScoreText;
    private Transform vrCamera;

    private void Start()
    {
        if (endStageCanvas != null)
        {
            endStageCanvas.SetActive(false);
        }
    }

    private void Update()
    {
        if (isTimerRunning)
        {
            timeElapsedInLevel += Time.deltaTime;

            if (currentPhase != GamePhase.UnlimitedHard)
            {
                if (timeRemaining > 0)
                {
                    timeRemaining -= Time.deltaTime;
                    UpdateDynamicUI();
                }
                else
                {
                    HandleLevelTimeout();
                }
            }
            else
            {
                UpdateDynamicUI();
            }
        }
    }

    public void StartPresetSequence()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        totalScore = 0;
        isTransitioning = false;

        vrCamera = Camera.main != null ? Camera.main.transform : null;
        if (vrCamera == null)
        {
            GameObject centerEye = GameObject.Find("CenterEyeAnchor");
            if (centerEye != null) vrCamera = centerEye.transform;
        }

        CreateDynamicHUD();
        PositionAndShowEndStageButton();

        InitEasyLevel();
    }

    private void PositionAndShowEndStageButton()
    {
        if (endStageCanvas != null)
        {
            if (vrCamera != null)
            {
                Vector3 forwardDirection = vrCamera.forward;
                forwardDirection.y = 0f;
                forwardDirection.Normalize();

                Vector3 targetPosition = vrCamera.position + (forwardDirection * endStageButtonDistance);
                targetPosition.y = vrCamera.position.y;

                endStageCanvas.transform.position = targetPosition;
                endStageCanvas.transform.rotation = Quaternion.LookRotation(endStageCanvas.transform.position - vrCamera.position);
            }

            endStageCanvas.SetActive(true);
        }
    }

    public void OnEndStageButtonPressed()
    {
        if (isTransitioning || currentPhase == GamePhase.None) return;

        RecordCurrentStagePerformance();
        StartCoroutine(TransitionDelayRoutine());
    }

    private void InitEasyLevel()
    {
        ClearCurrentLevelObjects(clearChairs: true);
        currentPhase = GamePhase.Easy;
        ResetStageStats();

        totalCoinsSpawnedInLevel = 7;
        SpawnObjectsForLevel(7, 0, 0, 0, 0);
        SetupTimer(easyLevelDuration);
        UpdateDynamicUI();
    }

    private void InitMediumLevel()
    {
        ClearCurrentLevelObjects(clearChairs: true);
        currentPhase = GamePhase.CognitiveMedium;
        ResetStageStats();

        totalCoinsSpawnedInLevel = 6;
        SpawnObjectsForLevel(0, 3, 3, 0, 0);
        SetupTimer(mediumLevelDuration);
        UpdateDynamicUI();
    }

    private void InitHardLevel()
    {
        ClearCurrentLevelObjects(clearChairs: true);
        currentPhase = GamePhase.UnlimitedHard;
        ResetStageStats();

        totalCoinsSpawnedInLevel = 5;
        SpawnObjectsForLevel(5, 0, 0, 5, 5);
        SetupTimer(9999f);
        UpdateDynamicUI();
    }

    private void ResetStageStats()
    {
        isTransitioning = false;
        stageScore = 0;
        timeElapsedInLevel = 0f;
        virusesHitInLevel = 0;
        collisionsInLevel = 0;
    }

    private void SetupTimer(float duration)
    {
        timeRemaining = duration;
        isTimerRunning = true;
    }

    public void RegisterCoinCollection(GameObject collectedCoin, int pointValue = 1)
    {
        if (isTransitioning) return;

        if (collectedCoin != null && activeLevelObjects.Contains(collectedCoin))
        {
            activeLevelObjects.Remove(collectedCoin);

            if (coinSound != null)
            {
                AudioSource.PlayClipAtPoint(coinSound, collectedCoin.transform.position);
            }

            stageScore += pointValue;
            totalScore += pointValue;
            coinsRemainingInLevel--;

            if (coinsRemainingInLevel <= 0)
            {
                RecordCurrentStagePerformance();
                StartCoroutine(TransitionDelayRoutine());
            }
            else
            {
                UpdateDynamicUI();
            }
        }
    }

    public void RegisterVirusHit(GameObject hitVirus)
    {
        if (isTransitioning) return;

        if (hitVirus != null && activeLevelObjects.Contains(hitVirus))
        {
            activeLevelObjects.Remove(hitVirus);

            if (virusSound != null)
            {
                AudioSource.PlayClipAtPoint(virusSound, hitVirus.transform.position);
            }

            virusesHitInLevel++;
            stageScore -= 1;
            totalScore -= 1;
            UpdateDynamicUI();
        }
    }

    public void RegisterObstacleHit()
    {
        if (isTransitioning) return;
        collisionsInLevel++;
        stageScore--;
        totalScore--;
        UpdateDynamicUI();
    }

    public void RegisterChairHit()
    {
        RegisterObstacleHit();
    }

    private void RecordCurrentStagePerformance()
    {
        int coinsCollected = totalCoinsSpawnedInLevel - coinsRemainingInLevel;

        string stageNameStr = "Preset Mode";

        switch (currentPhase)
        {
            case GamePhase.Easy:
                stageNameStr = "Preset - Stage 1 (Easy)";
                break;
            case GamePhase.CognitiveMedium:
                stageNameStr = "Preset - Stage 2 (Cognitive Medium)";
                break;
            case GamePhase.UnlimitedHard:
                stageNameStr = "Preset - Stage 3 (Unlimited Hard)";
                break;
        }

        // Pass "NaN" for both Side and Difficulty in Preset mode
        if (RehabSessionReporter.Instance != null)
        {
            RehabSessionReporter.Instance.RecordAndShowStageReport(
                stageNameStr,
                stageScore,
                coinsCollected,
                totalCoinsSpawnedInLevel,
                virusesHitInLevel,
                collisionsInLevel,
                timeElapsedInLevel,
                "NaN",
                "NaN"
            );
        }
    }

    private IEnumerator TransitionDelayRoutine()
    {
        isTimerRunning = false;
        isTransitioning = true;

        ClearCurrentLevelObjects(clearChairs: false);

        int coinsCollected = totalCoinsSpawnedInLevel - coinsRemainingInLevel;

        if (dynamicPhaseText != null)
        {
            dynamicPhaseText.text = "STAGE SUMMARY: " + currentPhase.ToString().ToUpper();
            dynamicPhaseText.color = Color.yellow;
        }

        if (dynamicTimerText != null)
        {
            dynamicTimerText.fontSize = 55;
            dynamicTimerText.text = $"Coins: {coinsCollected}/{totalCoinsSpawnedInLevel} | Viruses: {virusesHitInLevel}\n" +
                                    $"Collisions: {collisionsInLevel} | Time: {Mathf.FloorToInt(timeElapsedInLevel)}s";
        }

        if (dynamicScoreText != null)
        {
            dynamicScoreText.text = "STAGE SCORE: " + stageScore;
        }

        yield return new WaitForSeconds(5.0f);

        if (currentPhase == GamePhase.UnlimitedHard)
        {
            currentPhase = GamePhase.Finished;

            if (dynamicPhaseText != null)
            {
                dynamicPhaseText.text = "GAME FINISHED!";
                dynamicPhaseText.color = Color.green;
            }

            yield return new WaitForSeconds(3.0f);
            CompleteSequence();
        }
        else
        {
            TransitionToNextPhase();
        }
    }

    private void TransitionToNextPhase()
    {
        if (currentPhase == GamePhase.Easy)
            InitMediumLevel();
        else if (currentPhase == GamePhase.CognitiveMedium)
            InitHardLevel();
    }

    private void HandleLevelTimeout()
    {
        RecordCurrentStagePerformance();
        StartCoroutine(TransitionDelayRoutine());
    }

    private void CompleteSequence()
    {
        isTimerRunning = false;

        if (RehabSessionReporter.Instance != null)
        {
            RehabSessionReporter.Instance.ShowFullSessionSummaryUI();
            RehabSessionReporter.Instance.ExportToCSV();
        }

        ResetAndReturnToMenu();
    }

    public void ResetAndReturnToMenu()
    {
        isTimerRunning = false;
        isTransitioning = false;
        ClearCurrentLevelObjects(clearChairs: true);
        currentPhase = GamePhase.None;
        totalScore = 0;
        stageScore = 0;

        if (endStageCanvas != null)
            endStageCanvas.SetActive(false);

        if (dynamicHUDCanvas != null) Destroy(dynamicHUDCanvas);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    private void SpawnObjectsForLevel(int yellowCoins, int greenCoins, int blueCoins, int viruses, int chairs)
    {
        coinsRemainingInLevel = yellowCoins + greenCoins + blueCoins;

        for (int i = 0; i < yellowCoins; i++) CreateObjectInstance(yellowCoinPrefab, yellowCoinValue, isChair: false);
        for (int i = 0; i < greenCoins; i++) CreateObjectInstance(greenBigCoinPrefab, greenCoinValue, isChair: false);
        for (int i = 0; i < blueCoins; i++) CreateObjectInstance(blueSmallCoinPrefab, blueCoinValue, isChair: false);
        for (int i = 0; i < viruses; i++) CreateObjectInstance(virusPrefab, 0, isChair: false);
        for (int i = 0; i < chairs; i++) CreateObjectInstance(staticObstaclePrefab, 0, isChair: true);
    }

    private void CreateObjectInstance(GameObject prefab, int pointValue = 1, bool isChair = false)
    {
        if (prefab == null) return;

        float userHeight = (vrCamera != null) ? vrCamera.position.y : 1.6f;
        float minHeight;
        float maxHeight;

        if (userHeight < seatedThresholdHeight)
        {
            minHeight = Mathf.Max(0.3f, userHeight - 0.5f);
            maxHeight = userHeight + 0.15f;
        }
        else
        {
            minHeight = Mathf.Max(0.5f, userHeight - 0.8f);
            maxHeight = userHeight + 0.35f;
        }

        float rangeLimit = (currentPhase == GamePhase.Easy) ? 0.7f : 1.5f;

        Vector3 randomPosition = transform.position + new Vector3(
            Random.Range(-rangeLimit, rangeLimit),
            Random.Range(minHeight, maxHeight),
            Random.Range(-rangeLimit, rangeLimit)
        );

        GameObject instance = Instantiate(prefab, randomPosition, Quaternion.identity);

        PresetElement element = instance.GetComponent<PresetElement>();
        if (element != null)
        {
            element.SetupElement(this);
            element.scoreValue = pointValue;
        }

        if (isChair)
        {
            activeChairs.Add(instance);
        }
        else
        {
            activeLevelObjects.Add(instance);
        }
    }

    private void ClearCurrentLevelObjects(bool clearChairs = false)
    {
        foreach (GameObject target in activeLevelObjects)
        {
            if (target != null) Destroy(target);
        }
        activeLevelObjects.Clear();

        if (clearChairs)
        {
            foreach (GameObject chair in activeChairs)
            {
                if (chair != null) Destroy(chair);
            }
            activeChairs.Clear();
        }
    }

    private void CreateDynamicHUD()
    {
        GameObject canvasObj = new GameObject("Dynamic_HUD_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        if (vrCamera != null)
        {
            canvasObj.transform.position = vrCamera.position + (vrCamera.forward * 1.5f);
            canvasObj.transform.rotation = Quaternion.LookRotation(canvasObj.transform.position - vrCamera.position);
        }
        else
        {
            canvasObj.transform.position = transform.position + new Vector3(0, 1.6f, 1.5f);
            canvasObj.transform.rotation = Quaternion.identity;
        }

        canvasObj.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);

        GameObject phaseObj = new GameObject("Dynamic_Phase_Text");
        phaseObj.transform.SetParent(canvasObj.transform, false);
        dynamicPhaseText = phaseObj.AddComponent<TextMeshProUGUI>();
        dynamicPhaseText.fontSize = 75;
        dynamicPhaseText.color = Color.cyan;
        dynamicPhaseText.alignment = TextAlignmentOptions.Center;

        RectTransform phaseRect = phaseObj.GetComponent<RectTransform>();
        phaseRect.sizeDelta = new Vector2(1200, 150);
        phaseRect.anchoredPosition = new Vector2(0, 200);
        dynamicPhaseText.enableWordWrapping = false;

        GameObject timerObj = new GameObject("Dynamic_Timer_Text");
        timerObj.transform.SetParent(canvasObj.transform, false);
        dynamicTimerText = timerObj.AddComponent<TextMeshProUGUI>();
        dynamicTimerText.fontSize = 85;
        dynamicTimerText.color = Color.white;
        dynamicTimerText.alignment = TextAlignmentOptions.Center;

        RectTransform timerRect = timerObj.GetComponent<RectTransform>();
        timerRect.sizeDelta = new Vector2(1200, 150);
        timerRect.anchoredPosition = new Vector2(0, 0);
        dynamicTimerText.enableWordWrapping = false;

        GameObject scoreObj = new GameObject("Dynamic_Score_Text");
        scoreObj.transform.SetParent(canvasObj.transform, false);
        dynamicScoreText = scoreObj.AddComponent<TextMeshProUGUI>();
        dynamicScoreText.fontSize = 80;
        dynamicScoreText.color = Color.white;
        dynamicScoreText.alignment = TextAlignmentOptions.Center;

        RectTransform scoreRect = scoreObj.GetComponent<RectTransform>();
        scoreRect.sizeDelta = new Vector2(1200, 150);
        scoreRect.anchoredPosition = new Vector2(0, -200);
        dynamicScoreText.enableWordWrapping = false;

        dynamicHUDCanvas = canvasObj;
    }

    private void UpdateDynamicUI()
    {
        if (currentPhase == GamePhase.Finished) return;

        if (dynamicPhaseText != null)
        {
            if (currentPhase == GamePhase.UnlimitedHard)
                dynamicPhaseText.text = "MODE: UNLIMITED HARD";
            else if (currentPhase == GamePhase.CognitiveMedium)
                dynamicPhaseText.text = "MODE: COGNITIVE MEDIUM";
            else
                dynamicPhaseText.text = "MODE: " + currentPhase.ToString().ToUpper();
        }

        if (dynamicTimerText != null && !isTransitioning)
        {
            dynamicTimerText.fontSize = 85;

            if (currentPhase == GamePhase.UnlimitedHard)
                dynamicTimerText.text = "TIME ELAPSED: " + Mathf.FloorToInt(timeElapsedInLevel).ToString() + "s";
            else
                dynamicTimerText.text = "TIME: " + Mathf.CeilToInt(timeRemaining).ToString() + "s";
        }

        if (dynamicScoreText != null)
        {
            dynamicScoreText.text = "STAGE SCORE: " + stageScore.ToString();
        }
    }
}