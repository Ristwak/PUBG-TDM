
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private InputManager inputManager;
    private Transform playerTransform;
    private Transform cameraTransform;
    private PlayerMovement playerMovement;

    [Header("Camera Movement")]
    public Transform cameraPivot;
    private Vector3 cameraFollowVelocity = Vector3.zero;

    public float cameraFollowSpeed = 0.3f;
    public float cameraLookSpeed = 2f;
    public float cameraPivotSpeed = 2f;

    public float lookAngle;
    public float pivotAngle;

    public float minPivotAngle = -30f;
    public float maxPivotAngle = 30f;

    [Header("Camera Collision")]
    public LayerMask collisionLayer;
    public float defaultPosition;
    public float cameraCollisionOffset = 0.2f;
    public float minCollisionOffset = 0.2f;
    public float cameraCollisionRadius = 0.2f;

    [Header("Scope")]
    public Camera mainCamera;
    private float originalFOV = 60f;

    private Vector3 cameraVectorPosition;

    public Transform CameraTransform => cameraTransform;

    private void Awake()
    {
        PlayerManager playerManager = GetComponentInParent<PlayerManager>();

        if (playerManager == null)
        {
            Debug.LogError("CameraManager must be inside the Player prefab.");
            return;
        }

        playerTransform = playerManager.transform;
        playerMovement = playerManager.GetComponent<PlayerMovement>();
        inputManager = playerManager.GetComponent<InputManager>();

        mainCamera = GetComponentInChildren<Camera>(true);

        if (mainCamera == null)
        {
            Debug.LogError("No Camera found under CameraManager.");
            return;
        }

        cameraTransform = mainCamera.transform;
        defaultPosition = cameraTransform.localPosition.z;
        cameraVectorPosition = cameraTransform.localPosition;
        originalFOV = mainCamera.fieldOfView;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void HandleAllCameraMovement()
    {
        if (cameraTransform == null || cameraPivot == null ||
            playerTransform == null || inputManager == null)
            return;

        FollowTarget();
        RotateCamera();
        CameraCollision();
        HandleScope();
    }

    private void FollowTarget()
    {
        Vector3 targetPosition = Vector3.SmoothDamp(
            transform.position,
            playerTransform.position,
            ref cameraFollowVelocity,
            cameraFollowSpeed
        );

        transform.position = targetPosition;
    }

    private void RotateCamera()
    {
        lookAngle += inputManager.cameraInputX * cameraLookSpeed;
        pivotAngle += inputManager.cameraInputY * cameraPivotSpeed;

        pivotAngle = Mathf.Clamp(pivotAngle, minPivotAngle, maxPivotAngle);

        transform.rotation = Quaternion.Euler(0f, lookAngle, 0f);
        cameraPivot.localRotation = Quaternion.Euler(pivotAngle, 0f, 0f);

        if (playerMovement != null &&
            !playerMovement.isMoving &&
            !playerMovement.isSprinting)
        {
            playerTransform.rotation = Quaternion.Euler(0f, lookAngle, 0f);
        }
    }

    private void CameraCollision()
    {
        float targetPosition = defaultPosition;

        RaycastHit hit;

        Vector3 direction = cameraTransform.position - cameraPivot.position;
        direction.Normalize();

        if (Physics.SphereCast(
            cameraPivot.position,
            cameraCollisionRadius,
            direction,
            out hit,
            Mathf.Abs(defaultPosition),
            collisionLayer,
            QueryTriggerInteraction.Ignore))
        {
            float distance = Vector3.Distance(cameraPivot.position, hit.point);
            targetPosition = -(distance - cameraCollisionOffset);
        }

        if (Mathf.Abs(targetPosition) < minCollisionOffset)
            targetPosition = -minCollisionOffset;

        cameraVectorPosition = cameraTransform.localPosition;

        cameraVectorPosition.z = Mathf.Lerp(
            cameraTransform.localPosition.z,
            targetPosition,
            0.2f
        );

        cameraTransform.localPosition = cameraVectorPosition;
    }

    private void HandleScope()
    {
        if (mainCamera == null || inputManager == null)
            return;

        mainCamera.fieldOfView = inputManager.scopeInput ? 30f : originalFOV;
    }
}
