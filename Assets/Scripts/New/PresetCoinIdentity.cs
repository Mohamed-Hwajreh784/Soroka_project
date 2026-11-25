// Use English for comments in the code
using UnityEngine;

public class PresetCoinIdentity : MonoBehaviour
{
    [Header("Audio Feedback")]
    public AudioClip successSound;        // Assign coin collection clip in Inspector

    [Header("Visual Effects")]
    public GameObject coinSuccessEffect;  // Assign Particle System prefab for the coin blast

    private PresetRehabManager manager;
    private int coinValue = 1;

    public void SetupCoin(PresetRehabManager managerInstance, int value)
    {
        manager = managerInstance;
        coinValue = value;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Restrict interaction strictly to hand reach movements
        bool isHandInteraction = other.CompareTag("Hand") ||
                                 other.name.Contains("Hand") ||
                                 other.name.Contains("Controller");

        if (isHandInteraction && manager != null)
        {
            PlaySpatialSound(successSound);
            SpawnVisualEffect();

            // Trigger successful collection through hand reach
            manager.RegisterCoinCollection(this.gameObject, coinValue);
            Destroy(this.gameObject);
        }
    }

    private void PlaySpatialSound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }
    }

    private void SpawnVisualEffect()
    {
        if (coinSuccessEffect != null)
        {
            GameObject effect = Instantiate(coinSuccessEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2.0f);
        }
    }
}