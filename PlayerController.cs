using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Vector2 moveInput;
    public Vector2 viewInput;

    void Update()
    {
        moveInput = InputManager.Instance.MoveAction.ReadValue<Vector2>();
        viewInput = InputManager.Instance.ViewAction.ReadValue<Vector2>();

        // Check if jump is triggered
        if (InputManager.Instance.JumpAction.WasPressedThisFrame())
        {
            Debug.Log("Jump Pressed!");
        }

        // Check if sprint is held
        if (InputManager.Instance.SprintAction.IsPressed())
        {
            Debug.Log("Sprinting...");
        }
    }
}
