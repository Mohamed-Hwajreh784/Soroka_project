// Use English for comments in the code
using System;
using System.IO;
using System.Text;
using UnityEngine;

public class ContinuousTrackingLogger : MonoBehaviour
{
    public static ContinuousTrackingLogger Instance { get; private set; }

    [Header("Tracking Transforms")]
    public Transform headTransform;      // Assign CenterEyeAnchor / Main Camera
    public Transform leftHandTransform;  // Assign LeftHandAnchor / Left Controller
    public Transform rightHandTransform; // Assign RightHandAnchor / Right Controller

    [Header("Logging Settings")]
    public string filePrefix = "KinematicLog";
    public bool isLoggingActive = false;

    private string filePath;
    private StringBuilder csvBuffer = new StringBuilder();
    private float startTime;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Auto-assign Main Camera if headTransform is missing
        if (headTransform == null && Camera.main != null)
        {
            headTransform = Camera.main.transform;
        }

        InitializeLogFile();
        StartLogging();
    }

    private void InitializeLogFile()
    {
        string directoryPath;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        // Save directly to Downloads folder on Windows PC (matching RehabSessionReporter)
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        directoryPath = Path.Combine(userProfile, "Downloads");
#else
        // Save to Android/data/<Package_Name>/files/ on Meta Quest (matching RehabSessionReporter)
        directoryPath = Application.persistentDataPath;
#endif

        string timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        filePath = Path.Combine(directoryPath, $"{filePrefix}_{timeStamp}.csv");

        // Write CSV Header
        string header = "Timestamp,ElapsedTime_Sec," +
                        "Head_PosX,Head_PosY,Head_PosZ,Head_RotX,Head_RotY,Head_RotZ," +
                        "LeftHand_PosX,LeftHand_PosY,LeftHand_PosZ,LeftHand_RotX,LeftHand_RotY,LeftHand_RotZ," +
                        "RightHand_PosX,RightHand_PosY,RightHand_PosZ,RightHand_RotX,RightHand_RotY,RightHand_RotZ";

        File.WriteAllText(filePath, header + "\n", Encoding.UTF8);
    }

    public void StartLogging()
    {
        startTime = Time.time;
        isLoggingActive = true;
        Debug.Log($"[ContinuousTrackingLogger] Logging started at: {filePath}");
    }

    public void StopLogging()
    {
        isLoggingActive = false;
        FlushBufferToFile();
        Debug.Log("[ContinuousTrackingLogger] Logging stopped and saved.");
    }

    private void FixedUpdate()
    {
        if (!isLoggingActive) return;

        float elapsedTime = Time.time - startTime;
        string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        // Extract Head Pose
        Vector3 headPos = headTransform != null ? headTransform.position : Vector3.zero;
        Vector3 headRot = headTransform != null ? headTransform.eulerAngles : Vector3.zero;

        // Extract Left Hand Pose
        Vector3 leftPos = leftHandTransform != null ? leftHandTransform.position : Vector3.zero;
        Vector3 leftRot = leftHandTransform != null ? leftHandTransform.eulerAngles : Vector3.zero;

        // Extract Right Hand Pose
        Vector3 rightPos = rightHandTransform != null ? rightHandTransform.position : Vector3.zero;
        Vector3 rightRot = rightHandTransform != null ? rightHandTransform.eulerAngles : Vector3.zero;
        // Build CSV Data Line
        string dataLine = $"{timeStamp},{elapsedTime:F3}," +
                          $"{headPos.x:F4},{headPos.y:F4},{headPos.z:F4},{headRot.x:F2},{headRot.y:F2},{headRot.z:F2}," +
                          $"{leftPos.x:F4},{leftPos.y:F4},{leftPos.z:F4},{leftRot.x:F2},{leftRot.y:F2},{leftRot.z:F2}," +
                          $"{rightPos.x:F4},{rightPos.y:F4},{rightPos.z:F4},{rightRot.x:F2},{rightRot.y:F2},{rightRot.z:F2}";

        csvBuffer.AppendLine(dataLine);

        // Periodically write buffer to disk to prevent memory issues
        if (csvBuffer.Length > 5000)
        {
            FlushBufferToFile();
        }
    }

    private void FlushBufferToFile()
    {
        if (csvBuffer.Length > 0 && !string.IsNullOrEmpty(filePath))
        {
            File.AppendAllText(filePath, csvBuffer.ToString(), Encoding.UTF8);
            csvBuffer.Clear();
        }
    }

    private void OnDisable()
    {
        StopLogging();
    }

    private void OnApplicationQuit()
    {
        StopLogging();
    }
}