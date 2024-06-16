using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerManager : MonoBehaviour
{
    InputManager inputManager;
    PlayerMovement playerMovement;
    CameraManager cameraManager;
    Animator animator;
    public bool isInteracting;

    PhotonView view;

    private void Awake()
    {
        view = GetComponent<PhotonView>();
        animator = GetComponent<Animator>();
        inputManager = GetComponent<InputManager>();
        playerMovement = GetComponent<PlayerMovement>();
        cameraManager = FindObjectOfType<CameraManager>();
    }

    void Start()
    {
        if(!view.IsMine)
        {
            Destroy(GetComponentInChildren<CameraManager>().gameObject);
        }
    }
    
    private void Update()
    {
        if (!view.IsMine)
            return;
        inputManager.HandleAllInputs();
    }

    private void FixedUpdate()
    {
        if (!view.IsMine)
            return;
        playerMovement.HandleAllMovement();
    }

    private void LateUpdate()
    {
        if (!view.IsMine)
            return;
        cameraManager.HandleAllCameraMovement();

        isInteracting = animator.GetBool("isInteracting");
        playerMovement.isJumping = animator.GetBool("isJumping");
        animator.SetBool("isGrounded", playerMovement.isGrounded);
    }
}
