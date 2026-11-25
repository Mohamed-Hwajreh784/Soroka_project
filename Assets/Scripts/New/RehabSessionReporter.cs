// Use English for comments in the code
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
using System.Diagnostics;
#endif

public class RehabSessionReporter : MonoBehaviour
{
    public static RehabSessionReporter Instance { get; private set; }

    [Header("UI References")]
    public GameObject summaryCanvas;
    public TMP_Text summaryText;
    public TMP_InputField participantInputField;

    [Header("Participant Configuration")]
    [SerializeField] private string currentParticipantId = "P001";

    private readonly List<StageData> sessionStages = new List<StageData>();
    private string currentSessionFilePath = "";

    [System.Serializable]
    public class StageData
    {
        public string timestamp;
        public string participantId;
        public string stageName;
        public string side;
        public string difficulty;
        public int finalScore;
        public int coinsCollected;
        public int totalCoins;
        public int virusesHit;
        public int obstaclesHit;
        public float durationSeconds;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            currentParticipantId = PlayerPrefs.GetString("LastParticipantID", "P001");
            StartNewPlayerSession(currentParticipantId);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetParticipantID(string newId)
    {
        if (!string.IsNullOrWhiteSpace(newId))
        {
            currentParticipantId = newId.Trim();
            PlayerPrefs.SetString("LastParticipantID", currentParticipantId);
            PlayerPrefs.Save();
        }
    }

    public void AdvanceToNextParticipant()
    {
        if (currentParticipantId.StartsWith("P") && int.TryParse(currentParticipantId.Substring(1), out int idNum))
        {
            currentParticipantId = $"P{(idNum + 1):D3}";
        }
        else
        {
            currentParticipantId = $"P_{DateTime.Now:HHmmss}";
        }

        PlayerPrefs.SetString("LastParticipantID", currentParticipantId);
        PlayerPrefs.Save();
        StartNewPlayerSession(currentParticipantId);
    }

    public void StartNewPlayerSession(string participantId = null)
    {
        if (!string.IsNullOrEmpty(participantId))
        {
            currentParticipantId = participantId;
        }
        else if (participantInputField != null && !string.IsNullOrWhiteSpace(participantInputField.text))
        {
            currentParticipantId = participantInputField.text.Trim();
        }

        PlayerPrefs.SetString("LastParticipantID", currentParticipantId);
        PlayerPrefs.Save();

        sessionStages.Clear();

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"RehabReport_{currentParticipantId}_{timestamp}.csv";
        string directoryPath;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        directoryPath = Path.Combine(userProfile, "Downloads");
#else
        directoryPath = Application.persistentDataPath;
#endif

        currentSessionFilePath = Path.Combine(directoryPath, fileName);
        UnityEngine.Debug.Log($"[RehabSessionReporter] Session initialized for Subject: {currentParticipantId} at path: {currentSessionFilePath}");
    }

    public void RecordAndShowStageReport(
        string stageName,
        int score,
        int coinsCollected,
        int totalCoins,
        int virusesHit,
        int obstaclesHit,
        float duration,
        string side,
        string difficulty)
    {
        StageData data = new StageData
        {
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            participantId = currentParticipantId,
            stageName = string.IsNullOrEmpty(stageName) ? "Auto Mode" : stageName,
            side = string.IsNullOrEmpty(side) ? "NaN" : side,
            difficulty = string.IsNullOrEmpty(difficulty) ? "NaN" : difficulty,
            finalScore = score,
            coinsCollected = coinsCollected,
            totalCoins = totalCoins,
            virusesHit = virusesHit,
            obstaclesHit = obstaclesHit,
            durationSeconds = duration
        };

        sessionStages.Add(data);
        UnityEngine.Debug.Log($"[RehabSessionReporter] Recorded Stage: {data.stageName} | Score: {data.finalScore}");
    }

    public void ShowFullSessionSummaryUI()
    {
        if (summaryCanvas != null) summaryCanvas.SetActive(true);

        if (summaryText != null)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"<size=120%><b>--- SESSION SUMMARY: {currentParticipantId} ---</b></size>\n");

            for (int i = 0; i < sessionStages.Count; i++)
            {
                var stage = sessionStages[i];
                sb.AppendLine($"<b>Round {i + 1} ({stage.stageName}):</b>");
                sb.AppendLine($" Side: {stage.side} | Difficulty: {stage.difficulty}");
                sb.AppendLine($" Final Score: {stage.finalScore} | Coins: {stage.coinsCollected}/{stage.totalCoins}");
                sb.AppendLine($" Viruses Hit: {stage.virusesHit} | Obstacles Hit: {stage.obstaclesHit}");
                sb.AppendLine($" Duration: {stage.durationSeconds:F1}s");
                sb.AppendLine("------------------------------------");
            }

            summaryText.text = sb.ToString();
        }
    }

    public void ExportToCSV()
    {
        if (string.IsNullOrEmpty(currentSessionFilePath))
        {
            StartNewPlayerSession(currentParticipantId);
        }

        if (sessionStages.Count == 0)
        {
            UnityEngine.Debug.LogWarning("[RehabSessionReporter] No stages recorded to export.");
            return;
        }

        try
        {
            // UTF8 with BOM ensures Excel formats headers properly from left to right
            using (StreamWriter writer = new StreamWriter(currentSessionFilePath, false, new UTF8Encoding(true)))
            {
                // Write CSV Header
                writer.WriteLine("Timestamp,ParticipantID,Stage Name,Side,Difficulty,Final Score,Coins Collected,Total Coins,Viruses Hit,Obstacles Hit,Duration (s)");

                // Write all recorded stages in this session
                foreach (StageData stage in sessionStages)
                {
                    string line = $"\"{stage.timestamp}\",\"{stage.participantId}\",\"{stage.stageName}\",\"{stage.side}\",\"{stage.difficulty}\"," +
                                  $"{stage.finalScore},{stage.coinsCollected},{stage.totalCoins}," +
                                  $"{stage.virusesHit},{stage.obstaclesHit},{stage.durationSeconds:F1}";
                    writer.WriteLine(line);
                }
            }

            UnityEngine.Debug.Log("[RehabSessionReporter] Exported all session stages to CSV successfully: " + currentSessionFilePath);

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            if (File.Exists(currentSessionFilePath))
            {
                Process.Start("explorer.exe", "/select,\"" + currentSessionFilePath.Replace("/", "\\") + "\"");
            }
#endif
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("[RehabSessionReporter] Error exporting CSV: " + ex.Message);
        }
    }

    public void ClearSessionData()
    {
        sessionStages.Clear();
        if (summaryCanvas != null) summaryCanvas.SetActive(false);
    }
}