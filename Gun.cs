using System.Collections;
using TMPro;
using UnityEngine;
using static Models;

public abstract class Gun : MonoBehaviour
{
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] public GameObject muzzleFlashPrefab;
    [HideInInspector] public InputMappings inputMappings;
    [HideInInspector] public PlayerMovement playerMovement;
    public WeaponSettingsModel weaponSettings;
    [HideInInspector] public PlayerSettingsModel playerSettings;
    [HideInInspector] public Transform cameraTransform;
    [HideInInspector] public bool isShooting = false;
    [HideInInspector] private RecoilSystem recoil;
    public GunData gunData;
    public Transform gunMuzzle;
    public GameObject bulletHolePrefab;
    private CharacterController characterController;
    public AudioSource audioSource;
    private float timeBetweenShots;
    private float currentAmmo = 0f;
    private float nextTimeToFire = 0f;
    private bool isReloading = false;

    [Header("Sway Settings")]
    private Vector3 newWeaponRotation;

    private Vector3 newWeaponRotationVelocity;
    private Vector3 targetWeaponRotation;
    private Vector3 targetWeaponRotationVelocity;
    private Vector3 newWeaponMovementRotation;
    private Vector3 newWeaponMovementRotationVelocity;
    private Vector3 targetWeaponMovementRotation;
    private Vector3 targetWeaponMovementRotationVelocity;

    [Header("ADS Settings")]
    private Vector3 idlePosition;
    private Quaternion idleRotation;
    [SerializeField] private Vector3 aimPosition;
    [SerializeField] private Quaternion aimRotation;
    [HideInInspector] public bool isAiming { get; private set; }
    public float adsSmoothTime;

    [Header("Bobbing Settings")]
    [SerializeField] private float WeaponBobAmount;

    [SerializeField] private float bobbingSpeed;

    private bool isMoving;

    private void Start()
    {
        currentAmmo = gunData.magazineSize;
        newWeaponRotation = transform.localRotation.eulerAngles;
        playerMovement = transform.root.GetComponent<PlayerMovement>();
        inputMappings = transform.root.GetComponent<InputMappings>();
        cameraTransform = playerMovement.cameraHolder.transform;
        characterController = GameObject.Find("Player").GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        idlePosition = transform.localPosition;
        idleRotation = transform.localRotation;
        recoil = GetComponent<RecoilSystem>();

        weaponSettings.SwayXInverted = playerSettings.ViewXInverted;
        weaponSettings.SwayYInverted = playerSettings.ViewYInverted;

        timeBetweenShots = 60f / gunData.RPM;
        adsSmoothTime = gunData.ADSSpeed;
    }

    public void Update()
    {
        if (Time.timeScale == 0 || pauseMenu.isPaused) return;

        isMoving = characterController.velocity.magnitude > 0.1f;

        bool fire = inputMappings.shootAction.triggered;
        bool reload = inputMappings.reloadAction.triggered;
        bool canFire = Time.time >= nextTimeToFire;

        if (reload && canFire)
        {
            TryReload();
        }

        if (fire && canFire)
        {
            TryShoot();
        }
        else if (gunData.isAutomatic && inputMappings.shootAction.ReadValue<float>() > 0f && canFire)
        {
            TryShoot();
        }
        else if (!gunData.isAutomatic && inputMappings.shootAction.ReadValue<float>() == 0f)
        {
            isShooting = false;
        }

        ADS();
        ApplyBobbing();
        CalculateWeaponSway();
        HandleSway();
    }

    private void ADS()
    {
        isAiming = inputMappings.AimAction.ReadValue<float>() > 0.5f;
        float inverseSmoothTime = 1f / adsSmoothTime;
        transform.localPosition = Vector3.Lerp(transform.localPosition, isAiming ? aimPosition : idlePosition, inverseSmoothTime * Time.deltaTime);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, isAiming ? aimRotation : idleRotation, inverseSmoothTime * Time.deltaTime);
    }


    private void UpdateAmmoText()
    {
        if (HUDManager.Instance == null) return;

        if (isReloading)
        {
            HUDManager.Instance.UpdateAmmoText("Reloading...", Color.gray);
        }
        else
        {
            if (currentAmmo <= 5)
            {
                HUDManager.Instance.UpdateAmmoText($"{currentAmmo} / {gunData.magazineSize}", Color.red);
                if (!isReloading) StartCoroutine(BlinkTextEffect());
            }
            else
            {
                StopCoroutine(BlinkTextEffect());
                ammoText.enabled = true;
                HUDManager.Instance.UpdateAmmoText($"{currentAmmo} / {gunData.magazineSize}", Color.white);
            }
        }
    }

    private IEnumerator BlinkTextEffect()
    {
        while (isReloading || currentAmmo <= 5)
        {
            ammoText.enabled = !ammoText.enabled;
            yield return new WaitForSeconds(0.7f);
        }

        ammoText.enabled = true;
    }

    private IEnumerator FadeTextEffect()
    {
        float fadeDuration = 0.5f;
        float elapsedTime = 0f;

        Color originalColor = ammoText.color;

        while (elapsedTime < fadeDuration)
        {
            ammoText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1 - (elapsedTime / fadeDuration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        ammoText.color = originalColor;
        isReloading = false;
        UpdateAmmoText();
    }

    private void HandleSway()
    {
        float aimMultiplier = isAiming ? 0.1f : 1f;
        // Rotation Sway
        targetWeaponRotation.y += weaponSettings.SwayAmount * aimMultiplier * (weaponSettings.SwayXInverted ? -inputMappings.lookInput.x : inputMappings.lookInput.x) * Time.deltaTime;
        targetWeaponRotation.x += weaponSettings.SwayAmount * aimMultiplier * (weaponSettings.SwayYInverted ? inputMappings.lookInput.y : -inputMappings.lookInput.y) * Time.deltaTime;

        targetWeaponRotation.x = Mathf.Clamp(targetWeaponRotation.x, -weaponSettings.SwayClampX, weaponSettings.SwayClampX);
        targetWeaponRotation.y = Mathf.Clamp(targetWeaponRotation.y, -weaponSettings.SwayClampY, weaponSettings.SwayClampY);
        targetWeaponRotation.z = targetWeaponRotation.y;

        targetWeaponRotation = Vector3.SmoothDamp(targetWeaponRotation, Vector3.zero, ref targetWeaponRotationVelocity, weaponSettings.SwayResetSmoothing);
        newWeaponRotation = Vector3.SmoothDamp(newWeaponRotation, targetWeaponRotation, ref newWeaponRotationVelocity, weaponSettings.SwaySmoothing);

        // Movement Sway
        targetWeaponMovementRotation.z = weaponSettings.MovementSwayX * aimMultiplier * (weaponSettings.MovementSwayXInverted ? -inputMappings.moveInput.x : inputMappings.moveInput.x);
        targetWeaponMovementRotation.x = weaponSettings.MovementSwayY * aimMultiplier * (weaponSettings.MovementSwayYInverted ? inputMappings.moveInput.y : -inputMappings.moveInput.y);

        targetWeaponMovementRotation = Vector3.SmoothDamp(targetWeaponMovementRotation, Vector3.zero, ref targetWeaponMovementRotationVelocity, weaponSettings.MovementSwaySmoothing);
        newWeaponMovementRotation = Vector3.SmoothDamp(newWeaponMovementRotation, targetWeaponMovementRotation, ref newWeaponMovementRotationVelocity, weaponSettings.MovementSwaySmoothing);

        transform.localRotation = Quaternion.Euler(newWeaponRotation + newWeaponMovementRotation);
    }

    private void ApplyBobbing()
    {
        float aimMultiplier = isAiming ? 0.5f : 1f;
        float bobbingAmount = isMoving ? WeaponBobAmount : 0f;

        Vector3 bobOffset = new Vector3(0, Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmount * aimMultiplier, 0);
        transform.localPosition += (bobOffset * Time.deltaTime);
    }

    public void TryReload()
    {
        if (!isReloading && currentAmmo < gunData.magazineSize)
        {
            StartCoroutine(Reload());
        }
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        UpdateAmmoText();
        yield return new WaitForSeconds(gunData.reloadTime - 0.5f);
        StartCoroutine(FadeTextEffect());
        currentAmmo = gunData.magazineSize;
        isReloading = false;
        UpdateAmmoText();
    }

    public void TryShoot()
    {
        if (isReloading || currentAmmo <= 0f)
        {
            return;
        }

        nextTimeToFire = Time.time + timeBetweenShots;
        HandleShoot();
        UpdateAmmoText();
    }

    private void HandleShoot()
    {
        isShooting = true;
        currentAmmo--;
        recoil.ApplyRecoil();
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

    private void BulletHitFX(RaycastHit hit)
    {
        Vector3 hitPosition = hit.point + hit.normal * 0.1f;

        GameObject bulletHole = Instantiate(bulletHolePrefab, hitPosition, Quaternion.LookRotation(hit.normal));

        bulletHole.transform.parent = hit.collider.transform;

        Destroy(bulletHole, 3f);
    }

    private IEnumerator FlashMuzzle()
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

    private void PlayerFireSound()
    {
        if (gunData.fireSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(gunData.fireSound);
        }
    }

    //Weapon Breathing animation
    private void CalculateWeaponSway()
    {
        float aimMultiplier = isAiming ? 0.1f : 1f;

        var targetPosition = LissajousCurve(weaponSettings.swayTime, weaponSettings.swayAmountA, weaponSettings.swayAmountB) / weaponSettings.swayScale * aimMultiplier;
        weaponSettings.swayPosition = Vector3.Lerp(weaponSettings.swayPosition, targetPosition, Time.smoothDeltaTime * weaponSettings.swayLerpSpeed);
        weaponSettings.swayTime += Time.deltaTime;

        if (weaponSettings.swayTime > 6.3f)
        {
            weaponSettings.swayTime = 0f;
        }

        weaponSettings.weaponSwayObject.localPosition = weaponSettings.swayPosition;
    }

    private Vector3 LissajousCurve(float Time, float A, float B)
    {
        return new Vector3(Mathf.Sin(Time), A * Mathf.Sin(B * Time + Mathf.PI));
    }
}