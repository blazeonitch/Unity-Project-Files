using System;
using Unity.Mathematics;
using UnityEngine;
using static Container;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [HideInInspector] public CharacterController characterController;
    public PlayerSettings playerSettings;
    public Transform cameraTransform;

    [Header("Movement")]
    private Vector2 moveInput;
    private Vector2 viewInput;
    private Vector3 newCameraRotation;
    private Vector3 newCharacterRotation;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    void Awake()
    {
        characterController = GetComponent<CharacterController>();

        newCameraRotation = transform.localRotation.eulerAngles;
        newCharacterRotation = transform.localRotation.eulerAngles;
    }

    void FixedUpdate()
    {
        CalculateMovement();
    }

    void Update()
    {
        CalculateView();
    }

    void CalculateMovement()
    {
        moveInput = InputManager.Instance.MoveAction.ReadValue<Vector2>();

        var verticalSpeed = playerSettings.WalkingForwardSpeed * moveInput.y * Time.deltaTime;
        var horizontalSpeed = playerSettings.WalkingStrafeSpeed * moveInput.x * Time.deltaTime;

        var newMovementSpeed = new Vector3(horizontalSpeed, 0, verticalSpeed);
        newMovementSpeed = transform.TransformDirection(newMovementSpeed);
        characterController.Move(newMovementSpeed);
    }

    void CalculateView()
    {
        viewInput = InputManager.Instance.ViewAction.ReadValue<Vector2>();

        newCharacterRotation.y += viewInput.x * playerSettings.ViewXSensitivity * (playerSettings.ViewXInverted ? -1 : 1) * Time.deltaTime;
        transform.localRotation = Quaternion.Euler(newCharacterRotation);

        newCameraRotation.x += -viewInput.y * playerSettings.ViewYSensitivity * (playerSettings.ViewYInverted ? -1 : 1) * Time.deltaTime;
        newCameraRotation.x = Mathf.Clamp(newCameraRotation.x, playerSettings.ViewMinClamp, playerSettings.ViewMaxClamp);

        cameraTransform.localRotation = Quaternion.Euler(newCameraRotation);
    }
}
