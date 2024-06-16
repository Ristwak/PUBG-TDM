using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [Header("Player Health")]
    const float maxHealth = 150f;
    public float currentHealth;
    public Slider healthbarSlider;
    public GameObject playerUI;
    public GameObject healthAndCrosshair;
    public GameObject crossHairUI;

    [Header("Refs & Physics")]
    PlayerControllerManager playerControllerManager;
    AnimatorManager animatorManager;
    InputManager inputManager;
    PlayerManager playerManager;
    Vector3 movedir;
    Transform cameraGameObject;
    Rigidbody playerRigidBody;

    [Header("Falling And Landing")]
    public float inAirTimer;
    public float leapingVelocity;
    public float fallingVelocity;
    public float rayCastHeight = 0.5f;
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
    public float jumpHeight = 4;
    public float gravityIntensity = -15f;

    PhotonView view;

    public int playerTeam;

    private void Awake()
    {
        view = GetComponent<PhotonView>();
        currentHealth = maxHealth;
        animatorManager = GetComponent<AnimatorManager>();
        playerManager = GetComponent<PlayerManager>();
        inputManager = GetComponent<InputManager>();
        playerRigidBody = GetComponent<Rigidbody>();
        cameraGameObject = Camera.main.transform;

        playerControllerManager = PhotonView.Find((int)view.InstantiationData[0]).GetComponent<PlayerControllerManager>();

        healthbarSlider.minValue = 0f;
        healthbarSlider.maxValue = maxHealth;
        healthbarSlider.value = currentHealth;
    }

    void Start()
    {
        if (!view.IsMine)
        {
            Destroy(playerRigidBody);
            Destroy(playerUI);
        }

        if (view.Owner.CustomProperties.ContainsKey("Team"))
        {
            int team = (int)view.Owner.CustomProperties["Team"];
            playerTeam = team;
        }

        RectTransform crossHairRectTransform = crossHairUI.GetComponent<RectTransform>();
        crossHairRectTransform.sizeDelta = new Vector2(crossHairRectTransform.sizeDelta.x, 200);

        Debug.Log("Crosshair Height Set to: " + crossHairRectTransform.rect.height);
    }

    public void HandleAllMovement()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            healthAndCrosshair.SetActive(false);
        }
        else
        {
            healthAndCrosshair.SetActive(true);
            HandleFallingAndLanding();
            if (playerManager.isInteracting)
            {
                return;
            }
            HandleMovement();
            HandleRotation();
        }
    }

    void HandleMovement()
    {
        if (isJumping)
            return;

        movedir = new Vector3(cameraGameObject.forward.x, 0f, cameraGameObject.forward.z) * inputManager.verticalInput;
        movedir = movedir + cameraGameObject.right * inputManager.horizontalInput;
        movedir.Normalize();

        movedir.y = 0;

        if (isSprinting)
        {
            movedir = movedir * sprintingSpeed;
        }
        else
        {
            if (inputManager.movementAmount >= 0.5f)
            {
                movedir = movedir * movementSpeed;
                isMoving = true;
            }

            if (inputManager.movementAmount <= 0f)
            {
                isMoving = false;
            }
        }

        Vector3 movementVelocity = movedir;
        playerRigidBody.velocity = movementVelocity;
    }

    void HandleRotation()
    {
        if (isJumping)
            return;

        Vector3 targetDir = Vector3.zero;

        targetDir = cameraGameObject.forward * inputManager.verticalInput;
        targetDir = targetDir + cameraGameObject.right * inputManager.horizontalInput;
        targetDir.Normalize();
        targetDir.y = 0;

        if (targetDir == Vector3.zero)
        {
            targetDir = transform.forward;
        }

        Quaternion targetRotation = Quaternion.LookRotation(targetDir);
        Quaternion playerRotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        transform.rotation = playerRotation;
    }

    void HandleFallingAndLanding()
    {
        RaycastHit hit;
        Vector3 rayCastOrigin = transform.position;
        Vector3 targetPosition;
        rayCastOrigin.y = rayCastOrigin.y + rayCastHeight;
        targetPosition = transform.position;

        if (!isGrounded && !isJumping)
        {
            if (!playerManager.isInteracting)
            {
                animatorManager.PlayTargetAnim("Falling", true);
            }

            inAirTimer = inAirTimer + Time.deltaTime;
            playerRigidBody.AddForce(transform.forward * leapingVelocity);
            playerRigidBody.AddForce(-Vector3.up * fallingVelocity * inAirTimer);
        }

        if (Physics.SphereCast(rayCastOrigin, 0.2f, -Vector3.up, out hit, groundLayer))
        {
            if (!isGrounded && !playerManager.isInteracting)
            {
                animatorManager.PlayTargetAnim("Landing", true);
            }

            Vector3 rayCastHitPoint = hit.point;
            targetPosition.y = rayCastHitPoint.y;
            inAirTimer = 0;
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }

        if (isGrounded && !isJumping)
        {
            if (playerManager.isInteracting || inputManager.movementAmount > 0)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime / 0.1f);
            }
            else
            {
                transform.position = targetPosition;
            }
        }
    }

    public void HandleJumping()
    {
        if (isGrounded)
        {
            animatorManager.animator.SetBool("isJumping", true);
            animatorManager.PlayTargetAnim("Jump", false);

            float jumpingVelocity = Mathf.Sqrt(-2 * gravityIntensity * jumpHeight);
            Vector3 playerVelocity = movedir;
            playerVelocity.y = jumpingVelocity;
            playerRigidBody.velocity = playerVelocity;

            isJumping = false;
        }
    }

    public void SetIsJumping(bool isJumping)
    {
        this.isJumping = isJumping;
    }

    public void ApplyDamage(float damageValue)
    {
        view.RPC("RPC_TakeDamage", RpcTarget.All, damageValue);
    }

    [PunRPC]
    void RPC_TakeDamage(float damage)
    {
        if (!view.IsMine)
            return;
        currentHealth -= damage;
        healthbarSlider.value = currentHealth;
        if (currentHealth <= 0)
        {
            Die();
        }

        Debug.Log("Damage Taken " + damage);
        Debug.Log("Current Taken " + currentHealth);
    }

    private void Die()
    {
        playerControllerManager.Die();

        ScoreBoard.instance.PlayerDied(playerTeam);
    }
}
