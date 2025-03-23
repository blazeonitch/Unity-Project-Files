using UnityEngine;
using static Container;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    private CharacterController characterController;

    public Transform cameraTransform;
    public Transform feetTransform;
    public PlayerSettings playerSettings;
    private InputManager inputManager;
    private SoundManager soundManager;

    [Header("Movement")]
    public LayerMask playerMask;

    private Vector2 moveInput;
    private Vector2 viewInput;
    private Vector3 newCameraRotation;
    private Vector3 newCharacterRotation;

    [Header("Gravity")]
    public float gravityAmount;

    private float playerGravity;
    public float gravityMin;

    private Vector3 jumpingForce;
    private Vector3 jumpingForceVelocity;

    [Header("Stance")]
    public PlayerStance playerStance;

    public float playerStanceSmoothing;
    public CharacterStance playerStandStance;
    public CharacterStance playerCrouchStance;
    private float stanceCheckErrorMargin = 0.05f;
    private float cameraHeight;
    private float cameraHeightVelocity;
    private Vector3 stanceCapsuleCenterVelocity;
    private float stanceCapsuleHeightVelocity;

    private bool isCrouching;
    private bool isSprinting;
    private Vector3 newMovementSpeed;
    private Vector3 newMovementSpeedVelocity;

    [Header("Footstep Settings")]
    private Transform playerTransform;

    public float raycastDistance;
    public float baseStepInterval = 0.6f;
    public float sprintMultiplier = 0.5f;
    public float crouchMultiplier = 1.5f;

    private float stepTimer;
    private float stepInterval;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        inputManager = InputManager.Instance;
        soundManager = SoundManager.Instance;

        stepInterval = baseStepInterval;
        playerTransform = transform;

        soundManager.PlayMusic();
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        newCameraRotation = transform.localRotation.eulerAngles;
        newCharacterRotation = transform.localRotation.eulerAngles;

        cameraHeight = cameraTransform.localPosition.y;
    }

    private void FixedUpdate()
    {
        CalculateMovement();
    }

    private void Update()
    {
        raycastDistance = characterController.height;
        float speed = characterController.velocity.magnitude;
        DebugX();

        if (Time.timeScale == 0)
        {
            return;
        }
        CalculateView();
        inputManager.JumpAction.performed += ctx => Jump();
        CalculateJump();
        CalculateStance();

        if (isSprinting)
            stepInterval = baseStepInterval * sprintMultiplier;
        else if (isCrouching)
            stepInterval = baseStepInterval * crouchMultiplier;
        else
            stepInterval = baseStepInterval;

        if (characterController.isGrounded && speed > 0.1f)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0)
            {
                string surfaceTag = DetectSurface();
                soundManager.PlayFootstep(surfaceTag);
                stepTimer = stepInterval;
            }
        }
    }

    private string DetectSurface()
    {
        RaycastHit hit;
        if (Physics.Raycast(playerTransform.position, Vector3.down, out hit, raycastDistance))
        {
            return hit.collider.tag;
        }
        return "Default";
    }

    private void CalculateMovement()
    {
        moveInput = inputManager.MoveAction.ReadValue<Vector2>();

        if (inputManager.SprintAction.ReadValue<float>() > 0.1f && playerStance != PlayerStance.Crouch)
        {
            isSprinting = true;
        }
        else
        {
            isSprinting = false;
        }

        var verticalSpeed = isSprinting ? playerSettings.SprintForwardSpeed : playerSettings.WalkingForwardSpeed;
        var horizontalSpeed = isSprinting ? playerSettings.SprintStrafeSpeed : playerSettings.WalkingStrafeSpeed;

        if (!characterController.isGrounded)
        {
            playerSettings.SpeedEffector = playerSettings.FallingSpeedEffector;
        }
        else if (playerStance == PlayerStance.Crouch)
        {
            playerSettings.SpeedEffector = playerSettings.CrouchSpeedEffector;
        }
        else
        {
            playerSettings.SpeedEffector = 1f;
        }

        //Effectors
        verticalSpeed *= playerSettings.SpeedEffector;
        horizontalSpeed *= playerSettings.SpeedEffector;

        newMovementSpeed = Vector3.SmoothDamp(newMovementSpeed, new Vector3(horizontalSpeed * moveInput.x * Time.deltaTime, 0, verticalSpeed * moveInput.y * Time.deltaTime), ref newMovementSpeedVelocity, characterController.isGrounded ? playerSettings.MovementSmoothing : playerSettings.FallingSmoothing);
        var movementSpeed = transform.TransformDirection(newMovementSpeed);

        if (playerGravity > gravityMin)
        {
            playerGravity -= gravityAmount * Time.deltaTime;
        }

        if (playerGravity < -0.1f && characterController.isGrounded)
        {
            playerGravity = -0.1f;
        }

        movementSpeed.y += playerGravity;
        movementSpeed += jumpingForce * Time.deltaTime;
        characterController.Move(movementSpeed);
    }

    private void CalculateView()
    {
        viewInput = inputManager.ViewAction.ReadValue<Vector2>();

        newCharacterRotation.y += viewInput.x * playerSettings.ViewXSensitivity * (playerSettings.ViewXInverted ? -1 : 1) * Time.deltaTime;
        transform.localRotation = Quaternion.Euler(newCharacterRotation);

        newCameraRotation.x += -viewInput.y * playerSettings.ViewYSensitivity * (playerSettings.ViewYInverted ? -1 : 1) * Time.deltaTime;
        newCameraRotation.x = Mathf.Clamp(newCameraRotation.x, playerSettings.ViewMinClamp, playerSettings.ViewMaxClamp);

        cameraTransform.localRotation = Quaternion.Euler(newCameraRotation);
    }

    private void CalculateJump()
    {
        jumpingForce = Vector3.SmoothDamp(jumpingForce, Vector3.zero, ref jumpingForceVelocity, playerSettings.JumpFallOff);
    }

    private void Jump()
    {
        if (!characterController.isGrounded)
        {
            return;
        }

        jumpingForce = Vector3.up * playerSettings.JumpHeight;
        playerGravity = 0f;
    }

    private void CalculateStance()
    {
        var currentStance = playerStandStance;

        if (inputManager.CrouchAction.triggered && !StanceCheck(playerStandStance.StanceCollider.height))
        {
            if (playerStance == PlayerStance.Stand)
            {
                playerStance = PlayerStance.Crouch;
                isCrouching = true;
            }
            else
            {
                playerStance = PlayerStance.Stand;
                isCrouching = false;
            }
        }

        if (inputManager.JumpAction.triggered && playerStance == PlayerStance.Crouch && !StanceCheck(playerStandStance.StanceCollider.height))
        {
            playerStance = PlayerStance.Stand;
            isCrouching = false;
        }

        if (playerStance == PlayerStance.Crouch)
        {
            currentStance = playerCrouchStance;
        }

        cameraHeight = Mathf.SmoothDamp(cameraTransform.localPosition.y, currentStance.CameraHeight, ref cameraHeightVelocity, playerStanceSmoothing);
        cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, cameraHeight, cameraTransform.localPosition.z);

        characterController.height = Mathf.SmoothDamp(characterController.height, currentStance.StanceCollider.height, ref stanceCapsuleHeightVelocity, playerStanceSmoothing);
        characterController.center = Vector3.SmoothDamp(characterController.center, currentStance.StanceCollider.center, ref stanceCapsuleCenterVelocity, playerStanceSmoothing);
    }

    private bool StanceCheck(float stanceCheckHeight)
    {
        var start = new Vector3(feetTransform.position.x, feetTransform.position.y + stanceCheckErrorMargin + characterController.radius, feetTransform.position.z);
        var end = new Vector3(feetTransform.position.x, feetTransform.position.y - stanceCheckErrorMargin - characterController.radius + stanceCheckHeight, feetTransform.position.z);

        return Physics.CheckCapsule(start, end, characterController.radius, playerMask);
    }

    private void DebugX()
    {
        Debug.Log($"Grounded: {characterController.isGrounded}, Speed: {characterController.velocity.magnitude}");
        Debug.Log($"Step Timer: {stepTimer}");
        Debug.Log("Detecting Surface: " + DetectSurface());
    }
}
