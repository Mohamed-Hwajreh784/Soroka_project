using UnityEngine;

public class LeftHandMenuActivator : MonoBehaviour
{
    [Header("UI Settings")]
    public GameObject bubbleMenu; // נשאר רק בשביל המראה של התפריט, לא חובה למחיקה

    [Header("Deletion Settings")]
    public float detectionRadius = 0.2f;
    public LayerMask deleteableLayers;

    void Update()
    {
        // ניהול התפריט נשאר אותו דבר כי אמרת שהוא עובד לך סבבה
        HandleMenuVisibility();

        // מחיקה ישירה: רק קירבה + לחיצה על Trigger
        HandleDirectTriggerDeletion();
    }

    private void HandleMenuVisibility()
    {
        bool rightStick = OVRInput.Get(OVRInput.Button.SecondaryThumbstick, OVRInput.Controller.RTouch);
        bool leftStick = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);
        bool btnX = OVRInput.Get(OVRInput.Button.Three, OVRInput.Controller.LTouch);

        if (bubbleMenu != null)
        {
            bubbleMenu.SetActive(rightStick || leftStick || btnX);
        }
    }

    private void HandleDirectTriggerDeletion()
    {
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
        {
            // 1. בדיקה אם הלחיצה עובדת
            Debug.Log("1. Trigger detected on Left Controller!");

            Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius, deleteableLayers);

            // 2. בדיקה אם משהו בכלל נמצא ברדיוס
            Debug.Log("2. Found " + hitColliders.Length + " objects in radius.");

            foreach (var hit in hitColliders)
            {
                // 3. הדפסת השם והתג של מה שנגענו בו
                Debug.Log("3. Touching object: " + hit.name + " with Tag: " + hit.tag);

                if (hit.CompareTag("Coin") || hit.CompareTag("Virus")  || hit.CompareTag("DynamicObstacle") || hit.CompareTag("Score"))
                {
                    ExecuteDeletion(hit.gameObject);
                    break;
                }
            }
        }
    }

    private void ExecuteDeletion(GameObject obj)
    {
        Debug.Log("Direct Deletion: " + obj.name);

        // רטט בבקר לאישור פעולה
        OVRInput.SetControllerVibration(0.1f, 0.2f, OVRInput.Controller.LTouch);

        bool isCoin = obj.CompareTag("Coin");
        Destroy(obj);

        // עדכון ניקוד אם זה היה מטבע
        if (isCoin && GameStateManager.Instance != null)
        {
            Invoke("UpdateCoinCount", 0.05f);
        }
    }

    private void UpdateCoinCount()
    {
        int count = GameObject.FindGameObjectsWithTag("Coin").Length;
        GameStateManager.Instance.SetTotalCoins(count);
    }

    private void OnDrawGizmosSelected()
    {
        // הכדור הירוק שראית בסרטון - עוזר לראות את טווח המחיקה
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, detectionRadius);
    }
}