using System;
using UnityEngine;
using static Container;

public class PlayerController : MonoBehaviour
{
    private Vector2 moveInput;
    private Vector2 viewInput;
    public PlayerSettings playerSettings;
    [HideInInspector] public CharacterController characterController;
    public Transform cameraTransform;
    private Vector3 newCameraRotation;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
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

    }

    void CalculateView()
    {
        viewInput = InputManager.Instance.ViewAction.ReadValue<Vector2>();

        newCameraRotation.x += viewInput.y * playerSettings.ViewYSensitivity * (playerSettings.ViewYInverted ? 1 : -1) * Time.deltaTime;

        newCameraRotation.x = Mathf.Clamp(newCameraRotation.x, playerSettings.ViewMinClamp, playerSettings.ViewMaxClamp);

        cameraTransform.localRotation = Quaternion.Euler(newCameraRotation);
    }
}
