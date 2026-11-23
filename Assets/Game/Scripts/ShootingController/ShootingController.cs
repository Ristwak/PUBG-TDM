
using UnityEngine;
using Photon.Pun;

public class ShootingController : MonoBehaviour
{
    private Animator animator;
    private InputManager inputManager;
    private PlayerMovement playerMovement;
    private PhotonView view;

    public Transform firePoint;

    [Header("Shooting Variables")]
    public float fireRate = 5f;
    public float fireRange = 100f;
    public float fireDamage = 15f;

    private float nextFireTime;

    [Header("Reloading")]
    public int maxAmmo = 30;
    private int currentAmmo;
    public float reloadTime = 1.5f;
    public int totalAmmo = 90;

    [Header("Shooting Flags")]
    public bool isShooting;
    public bool isWalking;
    public bool isShootingInput;
    public bool isReloading;
    public bool isScopeInput;

    [Header("Sound Effects")]
    public AudioSource soundAudioSource;
    public AudioClip shootingSoundClip;
    public AudioClip reloadingSoundClip;

    [Header("Effects")]
    public ParticleSystem muzzleFlash;
    public ParticleSystem bloodEffect;

    public int playerTeam;

    private bool invalidFireRateWarningShown;

    private void Awake()
    {
        view = GetComponent<PhotonView>();
        animator = GetComponent<Animator>();
        inputManager = GetComponent<InputManager>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Start()
    {
        if (view.Owner != null &&
            view.Owner.CustomProperties.TryGetValue("Team", out object teamValue))
        {
            playerTeam = (int)teamValue;
        }

        currentAmmo = maxAmmo;
    }

    private void Update()
    {
        if (!view.IsMine)
            return;

        if (isReloading)
        {
            SetShootingAnimations(false, false, false);
            return;
        }

        isWalking = playerMovement.isMoving;
        isShootingInput = inputManager.fireInput;
        isScopeInput = inputManager.scopeInput;

        if (fireRate <= 0f)
        {
            if (!invalidFireRateWarningShown)
            {
                Debug.LogWarning("ShootingController: Fire Rate must be greater than zero.");
                invalidFireRateWarningShown = true;
            }

            return;
        }

        if (playerMovement.isSprinting)
        {
            SetShootingAnimations(false, false, false);
            isShooting = false;
            return;
        }

        if (isShootingInput)
        {
            if (Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + (1f / fireRate);
                Shoot();
            }

            if (isWalking)
                SetShootingAnimations(false, true, true);
            else
                SetShootingAnimations(true, false, false);

            isShooting = true;
        }
        else if (isScopeInput)
        {
            SetShootingAnimations(false, true, false);
            isShooting = false;
        }
        else
        {
            SetShootingAnimations(false, false, false);
            isShooting = false;
        }

        if (inputManager.reloadInput && currentAmmo < maxAmmo)
            Reload();
    }

    private void SetShootingAnimations(bool shoot, bool shootingMovement, bool shootWalk)
    {
        animator.SetBool("Shoot", shoot);
        animator.SetBool("ShootingMovement", shootingMovement);
        animator.SetBool("ShootWalk", shootWalk);
    }

    private void Shoot()
    {
        if (currentAmmo <= 0)
        {
            if (totalAmmo > 0)
                Reload();

            return;
        }

        if (firePoint == null)
        {
            Debug.LogError("ShootingController: Fire Point is not assigned.");
            return;
        }

        RaycastHit hit;

        if (Physics.Raycast(
            firePoint.position,
            firePoint.forward,
            out hit,
            fireRange,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            PlayerMovement targetPlayer =
                hit.collider.GetComponentInParent<PlayerMovement>();

            if (targetPlayer != null &&
                targetPlayer.playerTeam != playerTeam)
            {
                targetPlayer.ApplyDamage(fireDamage);

                view.RPC(
                    "RPC_Shoot",
                    RpcTarget.All,
                    hit.point,
                    hit.normal
                );
            }
        }

        if (muzzleFlash != null)
            muzzleFlash.Play();

        if (soundAudioSource != null && shootingSoundClip != null)
            soundAudioSource.PlayOneShot(shootingSoundClip);

        currentAmmo--;

        if (currentAmmo <= 0 && totalAmmo > 0)
            Reload();
    }

    [PunRPC]
    private void RPC_Shoot(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (bloodEffect == null)
            return;

        ParticleSystem blood = Instantiate(
            bloodEffect,
            hitPoint,
            Quaternion.LookRotation(hitNormal)
        );

        Destroy(blood.gameObject, blood.main.duration);
    }

    private void Reload()
    {
        if (isReloading || currentAmmo >= maxAmmo || totalAmmo <= 0)
            return;

        isReloading = true;

        animator.SetTrigger(
            isShootingInput && isWalking ? "ShootReload" : "Reload"
        );

        if (soundAudioSource != null && reloadingSoundClip != null)
            soundAudioSource.PlayOneShot(reloadingSoundClip);

        Invoke(nameof(FinishReloading), reloadTime);
    }

    private void FinishReloading()
    {
        int ammoToReload = Mathf.Min(
            maxAmmo - currentAmmo,
            totalAmmo
        );

        currentAmmo += ammoToReload;
        totalAmmo -= ammoToReload;

        isReloading = false;

        animator.ResetTrigger("ShootReload");
        animator.ResetTrigger("Reload");
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(FinishReloading));
        isReloading = false;
    }
}
