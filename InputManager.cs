using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    private InputSystem_Actions inputSystem;
    public InputAction MoveAction { get; private set; }
    public InputAction ViewAction { get; private set; }
    public InputAction JumpAction { get; private set; }
    public InputAction CrouchAction { get; private set; }
    public InputAction SprintAction { get; private set; }

    private void Awake()
    {

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        inputSystem = new InputSystem_Actions();

        // Player actions
        MoveAction = inputSystem.Player.Move;
        ViewAction = inputSystem.Player.View;
        JumpAction = inputSystem.Player.Jump;
        CrouchAction = inputSystem.Player.Crouch;
        SprintAction = inputSystem.Player.Sprint;

        inputSystem.Enable();
    }
}
