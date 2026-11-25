// Use English for comments in the code
using UnityEngine;

/// <summary>
/// Helper component attached to coins for VR hand collision and score management.
/// </summary>
public class CoinCollectible : MonoBehaviour
{
    [SerializeField] private int coinValue = 1;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the colliding object is the patient's virtual hand or controller
        if (other.CompareTag("Hand") || other.name.Contains("Hand") || other.name.Contains("Controller"))
        {
            // Register collected coin directly with ScoreManager
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.CoinCollected();
            }

            // Destroy coin object upon successful collection
            Destroy(gameObject);
        }
    }
}