// Use English for comments in the code
using UnityEngine;

public class PresetElement : MonoBehaviour
{
    public enum ElementType { Coin, Virus, Chair }

    [Header("Element Settings")]
    public ElementType elementType = ElementType.Coin;

    [Tooltip("Value used when elementType is Coin (e.g., Yellow = 1, Green = 3, Blue = 5)")]
    public int scoreValue = 1;

    [Header("Audio Feedback")]
    public AudioClip successSound;        // Assign coin collection clip in Inspector
    public AudioClip failureSound;        // Assign virus/chair penalty clip in Inspector

    [Header("Visual Effects")]
    public GameObject coinSuccessEffect;  // Assign Particle System prefab for the coin blast

    private PresetRehabManager manager;

    private void Start()
    {
        if (manager == null)
        {
            manager = FindObjectOfType<PresetRehabManager>();
        }
    }

    public void SetupElement(PresetRehabManager managerInstance)
    {
        manager = managerInstance;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Restrict interaction strictly to hand reach movements
        bool isHandInteraction = other.CompareTag("Hand") ||
                                 other.name.Contains("Hand") ||
                                 other.name.Contains("Controller");

        if (isHandInteraction && manager != null)
        {
            switch (elementType)
            {
                case ElementType.Coin:
                    PlaySpatialSound(successSound);
                    SpawnVisualEffect();
                    manager.RegisterCoinCollection(this.gameObject, scoreValue);
                    Destroy(gameObject);
                    break;

                case ElementType.Virus:
                    PlaySpatialSound(failureSound);
                    manager.RegisterVirusHit(this.gameObject);
                    Destroy(gameObject);
                    break;

                case ElementType.Chair:
                    PlaySpatialSound(failureSound);
                    manager.RegisterChairHit();
                    break;
            }
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