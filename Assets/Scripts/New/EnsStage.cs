// Use English for comments in the code
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class EndStageButton : MonoBehaviour
{
    [Header("Script References")]
    [Tooltip("רפרנס ל-PresetRehabManager. אם ישאר ריק, הקוד יחפש אותו אוטומטית בסצנה.")]
    [SerializeField] private PresetRehabManager presetManager;

    private Button buttonComponent;

    private void Awake()
    {
        buttonComponent = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (buttonComponent != null)
        {
            buttonComponent.onClick.AddListener(OnEndStageClicked);
        }
    }

    private void OnDisable()
    {
        if (buttonComponent != null)
        {
            buttonComponent.onClick.RemoveListener(OnEndStageClicked);
        }
    }

    private void Start()
    {
        // שיוך אוטומטי של PresetRehabManager אם הוא לא חובור ב-Inspector
        if (presetManager == null)
        {
            presetManager = FindObjectOfType<PresetRehabManager>();
        }
    }

    /// <summary>
    /// פונקציה שנקראת כאשר לוחצים על כפתור סיום השלב ב-VR (באמצעות Poke / Ray Interactor / לחיצה על UI).
    /// </summary>
    public void OnEndStageClicked()
    {
        // 1. הפעלת מנגנון סיום השלב והמעבר ב-Manager
        if (presetManager != null)
        {
            // קריאה לפונקציית האיפוס והחזרה לתפריט ב-Manager
            presetManager.ResetAndReturnToMenu();
        }

        // 2. הדפסת הודעת אישור ל-Console
        Debug.Log("[UI] 'End Stage' button pressed by user/therapist.");
    }
}