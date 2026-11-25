// Use English for comments in the code
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;

public class TargetObject : MonoBehaviour
{
    // Collection is disabled by default until Lock (Button B) is pressed
    public static bool CanCollect = false;
    public static Action<string> OnObjectHit;

    [Header("Settings")]
    public float collectDistance = 0.25f;
    public int bodySegments = 5;

    [Header("Audio Feedback")]
    public AudioClip successSound;
    public AudioClip failureSound;

    [Header("Visual Effects")]
    public GameObject coinSuccessEffect;

    private List<Transform> handPoints = new List<Transform>();
    private bool hasBeenCollected = false;

    private Renderer cachedRenderer;
    private Color originalColor = Color.white;
    private Grabbable grabComponent;
    private Camera mainCam;

    private SpawnerManager spawnerManager;
    private ManualModeManager manualManager;

    void Start()
    {
        mainCam = Camera.main;

        spawnerManager = UnityEngine.Object.FindFirstObjectByType<SpawnerManager>();
        manualManager = UnityEngine.Object.FindFirstObjectByType<ManualModeManager>();

        GameObject leftHand = GameObject.Find("LeftHandAnchor");
        GameObject rightHand = GameObject.Find("RightHandAnchor");

        if (leftHand != null) handPoints.Add(leftHand.transform);
        if (rightHand != null) handPoints.Add(rightHand.transform);

        cachedRenderer = GetComponentInChildren<Renderer>();
        if (cachedRenderer != null && cachedRenderer.material != null)
        {
            originalColor = cachedRenderer.material.color;
        }

        grabComponent = GetComponent<Grabbable>();

        if (GameStateManager.Instance != null && GameStateManager.Instance.isAutoMode)
        {
            DisableGrabbing();
        }
    }

    void Update()
    {
        // Do nothing during manual edit mode until Button B is pressed (CanCollect becomes true)
        if (!CanCollect || hasBeenCollected) return;

        float sqrCollectDistance = collectDistance * collectDistance;

        // Hand contact detection
        foreach (Transform hand in handPoints)
        {
            if (hand != null && (transform.position - hand.position).sqrMagnitude < sqrCollectDistance)
            {
                HandleCollection();
                return;
            }
        }

        // Virtual Body Detection for non-coins
        if (!gameObject.CompareTag("Coin") && mainCam != null)
        {
            Vector3 headPos = mainCam.transform.position;
            float currentHeight = headPos.y;
            float lowestPoint = 0.5f;
            float stepSize = (currentHeight - lowestPoint) / bodySegments;

            for (int i = 0; i <= bodySegments; i++)
            {
                Vector3 samplePoint = new Vector3(headPos.x, currentHeight - (i * stepSize), headPos.z);

                if ((transform.position - samplePoint).sqrMagnitude < sqrCollectDistance)
                {
                    HandleCollection();
                    return;
                }
            }
        }
    }

    private void HandleCollection()
    {
        hasBeenCollected = true;

        StartCoroutine(TriggerVibrationRoutine());
        OnObjectHit?.Invoke(gameObject.tag);

        if (gameObject.CompareTag("Coin"))
        {
            PlaySpatialSound(successSound);
            SpawnVisualEffect();

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.CoinCollected();
            }

            ToggleVisuals(false);
            Destroy(gameObject, 1.0f);
        }
        else if (gameObject.CompareTag("Virus"))
        {
            PlaySpatialSound(failureSound);

            if (spawnerManager != null) spawnerManager.RegisterVirusHit();
            if (manualManager != null) manualManager.RegisterVirusHit();

            ToggleVisuals(false);
            Destroy(gameObject, 1.0f);
        }
        else if (gameObject.CompareTag("Chair") || gameObject.CompareTag("DynamicObstacle"))
        {
            PlaySpatialSound(failureSound);

            if (spawnerManager != null) spawnerManager.RegisterObstacleHit();
            if (manualManager != null) manualManager.RegisterObstacleHit();

            if (cachedRenderer != null && cachedRenderer.material != null)
            {
                cachedRenderer.material.color = Color.gray;
            }

            StartCoroutine(ResetObjectRoutine(2.0f));
        }
    }

    private void PlaySpatialSound(AudioClip clip)
    {
        if (clip != null && mainCam != null)
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

    private void ToggleVisuals(bool state)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = state;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = state;
    }

    private IEnumerator TriggerVibrationRoutine()
    {
        OVRInput.SetControllerVibration(0.5f, 0.5f, OVRInput.Controller.RTouch);
        yield return new WaitForSeconds(0.15f);
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
    }

    private void DisableGrabbing()
    {
        if (grabComponent != null) grabComponent.enabled = false;

        var physicsGrab = GetComponent<PhysicsGrabbable>();
        if (physicsGrab != null) physicsGrab.enabled = false;
    }

    private IEnumerator ResetObjectRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        hasBeenCollected = false;
        if (cachedRenderer != null && cachedRenderer.material != null)
        {
            cachedRenderer.material.color = originalColor;
        }
    }
}