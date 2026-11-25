// Use English for comments in the code
using UnityEngine;
using System.Collections;

public class DestroyOnTouch : MonoBehaviour
{
    [Header("Audio Feedback")]
    public AudioClip successSound;        // Assign coin collection clip in Inspector
    public AudioClip failureSound;        // Assign virus/chair penalty clip in Inspector

    [Header("Visual Effects")]
    public GameObject coinSuccessEffect;  // Assign Particle System prefab for the coin blast

    private Collider myCollider;
    private bool isChairCooldownActive = false; // Prevents rapid point loss from standing inside a static obstacle
    private bool hasBeenTriggered = false;      // Ensures interaction runs exactly once before destruction

    private Renderer cachedRenderer;
    private Color originalColor = Color.white;

    private SpawnerManager spawnerManager;
    private ManualModeManager manualManager;

    void Awake()
    {
        myCollider = GetComponent<Collider>();
        if (myCollider != null)
        {
            myCollider.enabled = false;
        }

        cachedRenderer = GetComponentInChildren<Renderer>();
        if (cachedRenderer != null && cachedRenderer.material != null)
        {
            originalColor = cachedRenderer.material.color;
        }
    }

    void Start()
    {
        spawnerManager = UnityEngine.Object.FindFirstObjectByType<SpawnerManager>();
        manualManager = UnityEngine.Object.FindFirstObjectByType<ManualModeManager>();

        StartCoroutine(EnableColliderWithDelay());
    }

    private IEnumerator EnableColliderWithDelay()
    {
        yield return new WaitForSeconds(1.0f);
        if (myCollider != null)
        {
            myCollider.enabled = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ScoreManager.Instance != null && !ScoreManager.Instance.IsGameActive())
        {
            return;
        }

        bool isHand = other.CompareTag("Hand") || other.name.Contains("Hand") || other.name.Contains("Controller");
        bool isPlayerBody = other.CompareTag("Player");

        if (gameObject.CompareTag("Coin") && isHand && !hasBeenTriggered)
        {
            HandleCoinInteraction();
        }
        else if ((gameObject.CompareTag("Virus") || gameObject.CompareTag("Chair") || gameObject.CompareTag("DynamicObstacle")) && isPlayerBody)
        {
            HandleObstacleInteraction();
        }
    }

    private void HandleCoinInteraction()
    {
        hasBeenTriggered = true;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.CoinCollected();
        }

        PlaySpatialSound(successSound);
        SpawnVisualEffect();

        ToggleVisualsAndPhysics(false);
        Destroy(gameObject, 1.5f);
    }

    private void HandleObstacleInteraction()
    {
        if (gameObject.CompareTag("Virus") && !hasBeenTriggered)
        {
            hasBeenTriggered = true;

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.SubtractScore(1);
            }

            if (spawnerManager != null) spawnerManager.RegisterVirusHit();
            if (manualManager != null) manualManager.RegisterVirusHit();

            PlaySpatialSound(failureSound);

            ToggleVisualsAndPhysics(false);
            Destroy(gameObject, 1.5f);
        }
        else if (gameObject.CompareTag("Chair") || gameObject.CompareTag("DynamicObstacle"))
        {
            if (isChairCooldownActive) return;

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.SubtractScore(1);
            }

            if (spawnerManager != null) spawnerManager.RegisterObstacleHit();
            if (manualManager != null) manualManager.RegisterObstacleHit();

            PlaySpatialSound(failureSound);

            if (cachedRenderer != null && cachedRenderer.material != null)
            {
                cachedRenderer.material.color = Color.gray;
            }

            StartCoroutine(ChairHitCooldownRoutine());
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

    private void ToggleVisualsAndPhysics(bool state)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = state;

        if (myCollider != null) myCollider.enabled = state;
    }

    private IEnumerator ChairHitCooldownRoutine()
    {
        isChairCooldownActive = true;
        yield return new WaitForSeconds(2.0f);

        if (cachedRenderer != null && cachedRenderer.material != null)
        {
            cachedRenderer.material.color = originalColor;
        }

        isChairCooldownActive = false;
    }
}