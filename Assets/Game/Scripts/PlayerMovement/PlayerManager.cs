
using UnityEngine;
using Photon.Pun;

public class PlayerManager : MonoBehaviour
{
    private InputManager inputManager;
    private PlayerMovement playerMovement;
    private CameraManager cameraManager;
    private Animator animator;
    private PhotonView view;

    public bool isInteracting;

    public CameraManager CameraManager => cameraManager;

    private void Awake()
    {
        view = GetComponent<PhotonView>();
        animator = GetComponent<Animator>();
        inputManager = GetComponent<InputManager>();
        playerMovement = GetComponent<PlayerMovement>();

        cameraManager = GetComponentInChildren<CameraManager>(true);
    }

    private void Start()
    {
        if (!view.IsMine)
        {
            if (cameraManager != null)
                Destroy(cameraManager.gameObject);

            return;
        }

        if (cameraManager == null)
            Debug.LogError("Local player has no CameraManager child.");
    }

    private void Update()
    {
        if (!view.IsMine)
            return;

        if (inputManager != null)
            inputManager.HandleAllInputs();
    }

    private void FixedUpdate()
    {
        if (!view.IsMine)
            return;

        if (playerMovement != null)
            playerMovement.HandleAllMovement();
    }

    private void LateUpdate()
    {
        if (!view.IsMine)
            return;

        if (cameraManager != null)
            cameraManager.HandleAllCameraMovement();

        if (animator != null)
        {
            isInteracting = animator.GetBool("isInteracting");

            if (playerMovement != null)
            {
                playerMovement.isJumping = animator.GetBool("isJumping");
                animator.SetBool("isGrounded", playerMovement.isGrounded);
            }
        }
    }
}
