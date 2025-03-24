using System.Collections;
using TMPro;
using UnityEngine;
using static Container;

public abstract class Gun : MonoBehaviour
{
    [SerializeField] public GameObject muzzleFlashPrefab;
    private InputManager inputManager;
    private SoundManager soundManager;
    [HideInInspector] public PlayerController playerController;
    public WeaponSettings weaponSettings;
    [HideInInspector] public PlayerSettings playerSettings;
    [HideInInspector] public Transform cameraTransform;
    [HideInInspector] public bool isShooting = false;
    public GunData gunData;
    public Transform gunMuzzle;
    public GameObject bulletHolePrefab;
    private CharacterController characterController;
    private float timeBetweenShots;
    private float nextTimeToFire = 0f;
    private bool isReloading = false;

    [Header("Ammo Settings")]
    public float currentAmmo;
    public float reserveAmmo;
    public float maxReserveAmmo;


    [Header("ADS Settings")]
    private Vector3 idlePosition;
    [SerializeField] private Vector3 aimPosition;
    public float adsSmoothTime;

    private Vector3 newWeaponRotation = Vector3.zero;
    private Vector3 newWeaponRotationVelocity = Vector3.zero;
    private Vector3 targetWeaponRotation = Vector3.zero;
    private Vector3 targetWeaponRotationVelocity = Vector3.zero;

    void Start()
    {
        currentAmmo = gunData.magazineSize;
        playerController = transform.root.GetComponent<PlayerController>();
        inputManager = InputManager.Instance;
        soundManager = SoundManager.Instance;
        cameraTransform = playerController.cameraTransform.transform;
        characterController = GameObject.Find("Player").GetComponent<CharacterController>();
        idlePosition = transform.localPosition;

        newWeaponRotation = transform.localRotation.eulerAngles;

        timeBetweenShots = 60f / gunData.RPM;
        adsSmoothTime = gunData.ADSSpeed;
    }

    void Update()
    {
        if (Time.timeScale == 0) return;

        bool fire = inputManager.ShootAction.triggered;
        bool reload = inputManager.ReloadAction.triggered;
        bool canFire = Time.time >= nextTimeToFire;

        if (reload && canFire)
        {
            TryReload();
        }

        if (fire && canFire)
        {
            TryShoot();
        }
        else if (gunData.isAutomatic && inputManager.ShootAction.ReadValue<float>() > 0f && canFire)
        {
            TryShoot();
        }
        else if (!gunData.isAutomatic && inputManager.ShootAction.ReadValue<float>() == 0f)
        {
            isShooting = false;
        }

        ADS();
        WeaponSway();

    }

    void WeaponSway()
    {
        targetWeaponRotation.y += inputManager.viewInput.x * weaponSettings.SwayAmount * (weaponSettings.SwayXInverted ? -1 : 1) * Time.deltaTime;
        targetWeaponRotation.x += -inputManager.viewInput.y * weaponSettings.SwayAmount * (weaponSettings.SwayYInverted ? -1 : 1) * Time.deltaTime;

        targetWeaponRotation.x = Mathf.Clamp(targetWeaponRotation.x, -weaponSettings.SwayClampX, weaponSettings.SwayClampX);
        targetWeaponRotation.y = Mathf.Clamp(targetWeaponRotation.y, -weaponSettings.SwayClampY, weaponSettings.SwayClampY);

        targetWeaponRotation = Vector3.SmoothDamp(targetWeaponRotation, Vector3.zero, ref targetWeaponRotationVelocity, weaponSettings.SwaySmoothing);
        newWeaponRotation = Vector3.SmoothDamp(newWeaponRotation, targetWeaponRotation, ref newWeaponRotationVelocity, weaponSettings.SwaySmoothing);

        transform.localRotation = Quaternion.Euler(newWeaponRotation);
    }

    void ADS()
    {
        inputManager.isAiming = inputManager.AimAction.ReadValue<float>() > 0.5f;
        float inverseSmoothTime = 1f / adsSmoothTime;
        transform.localPosition = Vector3.Lerp(transform.localPosition, inputManager.isAiming ? aimPosition : idlePosition, inverseSmoothTime * Time.deltaTime);
    }

    public void TryReload()
    {
        if (!isReloading && currentAmmo < gunData.magazineSize)
        {
            StartCoroutine(Reload());
        }
    }

    IEnumerator Reload()
    {
        if (reserveAmmo <= 0 || currentAmmo == gunData.magazineSize)
        {
            yield break;
        }

        isReloading = true;
        // UpdateAmmoText(); 
        yield return new WaitForSeconds(gunData.reloadTime - 0.5f);


        float ammoNeeded = gunData.magazineSize - currentAmmo;
        float ammoToTransfer = Mathf.Min(ammoNeeded, reserveAmmo);

        currentAmmo += ammoToTransfer;
        reserveAmmo -= ammoToTransfer;

        isReloading = false;
        // UpdateAmmoText(); 
    }

    public void TryShoot()
    {
        if (isReloading || currentAmmo <= 0f)
        {
            return;
        }

        if (currentAmmo > 0)
        {
            nextTimeToFire = Time.time + timeBetweenShots;
            HandleShoot();
            // UpdateAmmoText(); 
        }
        else if (reserveAmmo > 0)
        {
            TryReload();
        }
    }

    void HandleShoot()
    {
        isShooting = true;
        currentAmmo--;
        StartCoroutine(FlashMuzzle());
        Shoot();
        PlayerFireSound();
    }

    public abstract void Shoot();

    public IEnumerator BulletFire(Vector3 target, RaycastHit hit)
    {
        if (hit.collider != null)
        {
            yield return null;
            BulletHitFX(hit);
        }
        else
        {
            target = gunMuzzle.position + cameraTransform.forward * 100f;
        }
    }

    void BulletHitFX(RaycastHit hit)
    {
        Vector3 hitPosition = hit.point + hit.normal * 0.1f;

        GameObject bulletHole = Instantiate(bulletHolePrefab, hitPosition, Quaternion.LookRotation(hit.normal));

        bulletHole.transform.parent = hit.collider.transform;

        Destroy(bulletHole, 3f);
    }

    IEnumerator FlashMuzzle()
    {
        GameObject flashInstance = Instantiate(muzzleFlashPrefab, gunMuzzle.position, gunMuzzle.rotation);
        flashInstance.transform.SetParent(gunMuzzle);
        if (flashInstance.TryGetComponent(out ParticleSystem ps))
        {
            Debug.Log("Particle System found, playing...");
            ps.Play();
            yield return new WaitForSeconds(0.1f);
        }
        else Debug.LogWarning("No Particle System found on muzzle flash prefab!");

        Destroy(flashInstance);
    }

    void PlayerFireSound()
    {
        if (gunData.fireSound != null && soundManager.weaponSFX != null)
        {
            soundManager.weaponSFX.PlayOneShot(gunData.fireSound);
        }
    }

    public void AddAmmo(float amount)
    {
        reserveAmmo = Mathf.Min(reserveAmmo + amount, maxReserveAmmo);
        // UpdateAmmoText(); // Optional: Update HUD with new ammo counts
    }

    public void SubtractAmmo(float amount)
    {
        reserveAmmo = Mathf.Max(reserveAmmo - amount, 0);
        // UpdateAmmoText(); // Optional: Update HUD with new ammo counts
    }
}
