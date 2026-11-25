using UnityEngine;
using Meta.XR.MRUtilityKit;

public class DynamicObstacle : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 1.2f;
    public LayerMask wallLayer;

    [Header("Player Body Modeling")]
    public float bodyRadius = 0.25f;
    private Transform cameraTransform;

    private Vector3 pointA, pointB, targetPoint;
    private bool isMoving = false;
    private bool isHit = false;

    // English comment: Initialize and setup references
    void Start()
    {
        FindCamera();

        // English comment: Ensure the obstacle has a tag recognized by GameStateManager
        if (!gameObject.CompareTag("Virus") && !gameObject.CompareTag("Chair"))
        {
            gameObject.tag = "Virus";
        }
    }

    void Update()
    {
        // English comment: Trigger movement ONLY ONCE when B button is pressed
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            StartMovingWithMRUK();
        }

        // English comment: Keep moving infinitely back and forth along the locked path
        if (isMoving && !isHit)
        {
            MoveObstacle();
            CheckFiveSegmentCollision();
        }
    }

    // English comment: Automatically find the VR Camera (CenterEyeAnchor)
    void FindCamera()
    {
        GameObject camObj = GameObject.Find("CenterEyeAnchor");
        if (camObj != null)
        {
            cameraTransform = camObj.transform;
        }
        else if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    // English comment: Smoothly move the obstacle between the two locked points infinitely
    void MoveObstacle()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPoint, speed * Time.deltaTime);

        // English comment: If reached target, switch to the other fixed point (Infinite loop)
        if (Vector3.Distance(transform.position, targetPoint) < 0.05f)
        {
            targetPoint = (targetPoint == pointA) ? pointB : pointA;
        }
    }

    // English comment: Calculate and LOCK the path based on camera rotation at the exact moment of button press
    public void StartMovingWithMRUK()
    {
        if (isHit || cameraTransform == null) return;

        // English comment: 1. Get the camera's right direction at this exact frame
        Vector3 dir = cameraTransform.right;
        dir.y = 0; // English comment: Flatten to keep it perfectly horizontal
        dir.Normalize();

        RaycastHit hitR, hitL;

        // English comment: 2. Scan walls along this locked direction
        if (Physics.Raycast(transform.position, dir, out hitR, 15f, wallLayer) &&
            Physics.Raycast(transform.position, -dir, out hitL, 15f, wallLayer))
        {
            pointA = hitR.point - (dir * 0.4f);
            pointB = hitL.point + (dir * 0.4f);
        }
        else
        {
            // English comment: Fallback to fixed distance if no walls hit
            pointA = transform.position - (dir * 1.5f);
            pointB = transform.position + (dir * 1.5f);
        }

        // English comment: 3. Lock the height (Y) to the obstacle's current height
        pointA.y = transform.position.y;
        pointB.y = transform.position.y;

        // English comment: 4. Start the movement loop towards the first point
        targetPoint = pointB;
        isMoving = true;
    }

    private void CheckFiveSegmentCollision()
    {
        if (cameraTransform == null) return;

        float headH = cameraTransform.position.y;
        float[] heights = { headH, headH * 0.75f, headH * 0.5f, headH * 0.25f, 0.1f };

        foreach (float h in heights)
        {
            Vector3 segmentPos = new Vector3(cameraTransform.position.x, h, cameraTransform.position.z);

            if (Vector3.Distance(transform.position, segmentPos) < bodyRadius)
            {
                HandlePlayerCollision();
                break;
            }
        }
    }

    void HandlePlayerCollision()
    {
        isHit = true;
        isMoving = false;

        OVRInput.SetControllerVibration(0.5f, 0.5f, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(0.5f, 0.5f, OVRInput.Controller.LTouch);

        TargetObject.OnObjectHit?.Invoke(gameObject.tag);
        Debug.Log($"Dynamic Obstacle hit reported via Tag: {gameObject.tag}");

        Destroy(gameObject, 0.1f);
    }

    void OnDrawGizmos()
    {
        if (cameraTransform != null)
        {
            Gizmos.color = Color.yellow;
            float h = cameraTransform.position.y;
            float[] checkHeights = { h, h * 0.75f, h * 0.5f, h * 0.25f, 0.1f };
            foreach (float height in checkHeights)
            {
                Vector3 segmentPos = new Vector3(cameraTransform.position.x, height, cameraTransform.position.z);
                Gizmos.DrawWireSphere(segmentPos, bodyRadius);
            }
        }
    }
}