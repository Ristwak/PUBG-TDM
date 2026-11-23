
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [Header("Player Health")]
    private const float maxHealth = 150f;

    public float currentHealth;
    public Slider healthbarSlider;
    public GameObject playerUI;
    public GameObject healthAndCrosshair;
    public GameObject crossHairUI;

    [Header("Refs & Physics")]
    private PlayerControllerManager playerControllerManager;
    private AnimatorManager animatorManager;
    private InputManager inputManager;
    private PlayerManager playerManager;

    private Vector3 movedir;
    private Transform cameraGameObject;
    private Rigidbody playerRigidBody;
    private PhotonView view;

    [Header("Falling And Landing")]
    public float inAirTimer;
    public float leapingVelocity;
    public float fallingVelocity;
    public float rayCastHeight = 0.5f;
    public float groundCheckDistance = 1f;
    public LayerMask groundLayer;

    [Header("Movement Flags")]
    public bool isMoving;
    public bool isGrounded;
    public bool isSprinting;
    public bool isJumping;

    [Header("Movement Values")]
    public float movementSpeed = 2f;
    public float rotationSpeed = 13f;
    public float sprintingSpeed = 7f;

    [Header("Jump Variables")]
    public float jumpHeight = 4f;
    public float gravityIntensity = -15f;

    public int playerTeam;

    private void Awake()
    {
        view = GetComponent<PhotonView>();

        currentHealth = maxHealth;

        animatorManager = GetComponent<AnimatorManager>();
        playerManager = GetComponent<PlayerManager>();
        inputManager = GetComponent<InputManager>();
        playerRigidBody = GetComponent<Rigidbody>();

        if (healthbarSlider != null)
        {
            healthbarSlider.minValue = 0f;
            healthbarSlider.maxValue = maxHealth;
            healthbarSlider.value = currentHealth;
        }
    }

    private void Start()
    {
        if (!view.IsMine)
        {
            if (playerRigidBody != null)
                Destroy(playerRigidBody);

            if (playerUI != null)
                Destroy(playerUI);

            return;
        }

        if (playerManager != null &&
            playerManager.CameraManager != null)
        {
            cameraGameObject = playerManager.CameraManager.CameraTransform;
        }

        if (cameraGameObject == null)
            Debug.LogError("Local player camera reference is missing.");

        if (view.Owner != null &&
            view.Owner.CustomProperties.TryGetValue("Team", out object teamValue))
        {
            playerTeam = (int)teamValue;
        }

        if (crossHairUI != null)
        {
            RectTransform crossHairRectTransform =
                crossHairUI.GetComponent<RectTransform>();

            if (crossHairRectTransform != null)
            {
                crossHairRectTransform.sizeDelta = new Vector2(
                    crossHairRectTransform.sizeDelta.x,
                    200f
                );
            }
        }

        if (view.InstantiationData != null &&
            view.InstantiationData.Length > 0)
        {
            PhotonView controllerView =
                PhotonView.Find((int)view.InstantiationData[0]);

            if (controllerView != null)
                playerControllerManager =
                    controllerView.GetComponent<PlayerControllerManager>();
        }

        if (playerControllerManager == null)
            Debug.LogError("PlayerControllerManager reference could not be found.");
    }

    public void HandleAllMovement()
    {
        if (!view.IsMine || playerRigidBody == null)
            return;

        if (Input.GetKey(KeyCode.Escape))
        {
            if (healthAndCrosshair != null)
                healthAndCrosshair.SetActive(false);

            return;
        }

        if (healthAndCrosshair != null)
            healthAndCrosshair.SetActive(true);

        HandleFallingAndLanding();

        if (playerManager != null && playerManager.isInteracting)
            return;

        HandleMovement();
        HandleRotation();
    }

    private void HandleMovement()
    {
        if (isJumping || cameraGameObject == null)
            return;

        Vector3 forward = cameraGameObject.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = cameraGameObject.right;
        right.y = 0f;
        right.Normalize();

        movedir = forward * inputManager.verticalInput +
                  right * inputManager.horizontalInput;

        movedir.Normalize();

        isMoving = inputManager.movementAmount > 0.01f;

        float speed = isSprinting ? sprintingSpeed : movementSpeed;

        Vector3 movementVelocity = movedir * speed;

        Vector3 currentVelocity = playerRigidBody.velocity;

        playerRigidBody.velocity = new Vector3(
            movementVelocity.x,
            currentVelocity.y,
            movementVelocity.z
        );
    }

    private void HandleRotation()
    {
        if (isJumping || cameraGameObject == null)
            return;

        Vector3 targetDir =
            cameraGameObject.forward * inputManager.verticalInput +
            cameraGameObject.right * inputManager.horizontalInput;

        targetDir.y = 0f;

        if (targetDir.sqrMagnitude < 0.001f)
            return;

        targetDir.Normalize();

        Quaternion targetRotation = Quaternion.LookRotation(targetDir);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void HandleFallingAndLanding()
    {
        RaycastHit hit;

        Vector3 rayCastOrigin = transform.position;
        rayCastOrigin.y += rayCastHeight;

        Vector3 targetPosition = transform.position;

        if (!isGrounded && !isJumping)
        {
            if (playerManager != null &&
                !playerManager.isInteracting)
            {
                animatorManager.PlayTargetAnim("Falling", true);
            }

            inAirTimer += Time.deltaTime;

            playerRigidBody.AddForce(
                transform.forward * leapingVelocity
            );

            playerRigidBody.AddForce(
                Vector3.down * fallingVelocity * inAirTimer
            );
        }

        bool groundHit = Physics.SphereCast(
            rayCastOrigin,
            0.2f,
            Vector3.down,
            out hit,
            groundCheckDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        if (groundHit)
        {
            if (!isGrounded &&
                playerManager != null &&
                !playerManager.isInteracting)
            {
                animatorManager.PlayTargetAnim("Landing", true);
            }

            targetPosition.y = hit.point.y;

            inAirTimer = 0f;
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }

        if (isGrounded && !isJumping)
        {
            if (playerManager != null &&
                playerManager.isInteracting ||
                inputManager.movementAmount > 0f)
            {
                transform.position = Vector3.Lerp(
                    transform.position,
                    targetPosition,
                    Time.deltaTime / 0.1f
                );
            }
            else
            {
                transform.position = targetPosition;
            }
        }
    }

    public void HandleJumping()
    {
        if (!view.IsMine || !isGrounded || isJumping)
            return;

        isJumping = true;

        animatorManager.animator.SetBool("isJumping", true);
        animatorManager.PlayTargetAnim("Jump", false);

        float jumpingVelocity = Mathf.Sqrt(
            -2f * gravityIntensity * jumpHeight
        );

        Vector3 playerVelocity = playerRigidBody.velocity;
        playerVelocity.x = movedir.x;
        playerVelocity.z = movedir.z;
        playerVelocity.y = jumpingVelocity;

        playerRigidBody.velocity = playerVelocity;
        isGrounded = false;
    }

    public void SetIsJumping(bool value)
    {
        isJumping = value;
    }

    public void ApplyDamage(float damageValue)
    {
        if (view.IsMine)
            view.RPC("RPC_TakeDamage", RpcTarget.All, damageValue);
    }

    [PunRPC]
    private void RPC_TakeDamage(float damage)
    {
        if (!view.IsMine)
            return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);

        if (healthbarSlider != null)
            healthbarSlider.value = currentHealth;

        if (currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        if (playerControllerManager != null)
            playerControllerManager.Die();

        if (ScoreBoard.instance != null)
            ScoreBoard.instance.PlayerDied(playerTeam);
    }
}
