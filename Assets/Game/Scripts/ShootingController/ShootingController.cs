using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ShootingController : MonoBehaviour
{
    Animator animator;
    InputManager inputManager;
    PlayerManager playerManager;
    PlayerMovement playerMovement;

    public Transform firePoint;

    [Header("Shooting Variables")]
    public float fireRate = 0f;
    public float fireRange = 100f;
    public float fireDamage = 15f;
    private float nextFireTime = 0f;

    [Header("Reloading")]
    public int maxAmmo = 30;
    private int currentAmmo;
    public float reloadTime = 1.5f;
    public int totalAmmo = 90;

    [Header("Shooting Flags")]
    public bool isShooting;
    public bool isWalking;
    public bool isShootingInput;
    public bool isReloading = false;
    public bool isScopeInput;

    [Header("Sound Effects")]
    public AudioSource soundAudioSource;
    public AudioClip shootingSoundClip;
    public AudioClip reloadingSoundClip;

    [Header("Effects")]
    public ParticleSystem muzzleFlash;
    public ParticleSystem bloodEffect;

    PhotonView view;

    public int playerTeam;

    void Start()
    {
        view = GetComponent<PhotonView>();
        animator = GetComponent<Animator>();
        inputManager = GetComponent<InputManager>();
        playerManager = GetComponent<PlayerManager>();
        playerMovement = GetComponent<PlayerMovement>();
        currentAmmo = maxAmmo;

        if (view.Owner.CustomProperties.ContainsKey("Team"))
        {
            int team = (int)view.Owner.CustomProperties["Team"];
            playerTeam = team;
        }
    }

    private void Update()
    {
        if (!view.IsMine)
            return;

        RectTransform rectTransform = playerMovement.healthAndCrosshair.GetComponent<RectTransform>();
        Debug.Log("Crosshair Height in Update: " + rectTransform.rect.height);
        if (isReloading || playerMovement.isSprinting)
        {
            animator.SetBool("Shoot", false);
            animator.SetBool("ShootingMovement", false);
            animator.SetBool("ShootWalk", false);
            return;
        }

        isWalking = playerMovement.isMoving;
        isShootingInput = inputManager.fireInput;
        isScopeInput = inputManager.scopeInput;

        if (totalAmmo == 0)
        {
            isShootingInput = false;
        }

        if (isShootingInput && isWalking)
        {
            if (Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + 1f / fireRate;
                Shoot();
                animator.SetBool("ShootWalk", true);
            }

            animator.SetBool("Shoot", false);
            animator.SetBool("ShootingMovement", true);
            isShooting = true;
        }
        else if (isShootingInput)
        {
            if (Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + 1f / fireRate;
                Shoot();
            }

            animator.SetBool("Shoot", true);
            animator.SetBool("ShootingMovement", false);
            animator.SetBool("ShootWalk", false);
            isShooting = true;
        }
        else if (isScopeInput)
        {
            animator.SetBool("Shoot", false);
            animator.SetBool("ShootingMovement", true);
            animator.SetBool("ShootWalk", false);
            isShooting = false;
        }
        else
        {
            animator.SetBool("Shoot", false);
            animator.SetBool("ShootingMovement", false);
            animator.SetBool("ShootWalk", false);
            isShooting = false;
        }

        if (inputManager.reloadInput && currentAmmo < maxAmmo)
        {
            Reload();
        }
    }

    void Shoot()
    {
        if (currentAmmo > 0)
        {
            RaycastHit hit;
            if (Physics.Raycast(firePoint.position, firePoint.forward, out hit, fireRange))
            {
                Debug.Log(hit.transform.name);

                Vector3 hitPoint = hit.point;
                Vector3 hitNormal = hit.normal;

                PlayerMovement playerMovementDamage = hit.collider.GetComponent<PlayerMovement>();
                if (playerMovementDamage != null && playerMovementDamage.playerTeam != playerTeam)
                {
                    playerMovementDamage.ApplyDamage(fireDamage);
                    view.RPC("RPC_Shoot", RpcTarget.All, hitPoint, hitNormal);
                }
            }

            muzzleFlash.Play();
            soundAudioSource.PlayOneShot(shootingSoundClip);
            currentAmmo--;
        }
        else if (totalAmmo > 0)
        {
            Reload();
        }
    }

    [PunRPC]
    void RPC_Shoot(Vector3 hitPoint, Vector3 hitNormal)
    {
        ParticleSystem blood = Instantiate(bloodEffect, hitPoint, Quaternion.LookRotation(hitNormal));
        Destroy(blood.gameObject, blood.main.duration);
    }

    void Reload()
    {
        if (!isReloading && currentAmmo < maxAmmo && totalAmmo > 0)
        {
            animator.SetTrigger(isShootingInput && isWalking ? "ShootReload" : "Reload");
            isReloading = true;
            soundAudioSource.PlayOneShot(reloadingSoundClip);
            Invoke("FinishReloading", reloadTime);
        }
    }

    void FinishReloading()
    {
        if (totalAmmo > 0)
        {
            int ammoToReload = Mathf.Min(maxAmmo - currentAmmo, totalAmmo);
            currentAmmo += ammoToReload;
            totalAmmo -= ammoToReload;
            isReloading = false;
            animator.ResetTrigger("ShootReload");
            animator.ResetTrigger("Reload");
        }
    }
}
