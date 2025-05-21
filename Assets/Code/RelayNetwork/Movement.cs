using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : NetworkBehaviour
{
    InputSystem_Actions controls;
    [SerializeField] Rigidbody2D rb;
    [SerializeField] float speed;
    [SerializeField] Vector2 movementInput;

    private void Awake()
    {
        controls = new InputSystem_Actions();

        controls.Player.Move.performed += OnMoveInput;
        controls.Player.Move.canceled += OnMoveInput;

        controls.Disable();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            controls.Enable();
        }
        base.OnNetworkSpawn();
    }

    private void OnEnable()
    {
        if (IsOwner)
            controls.Enable();
    }
    private void OnDisable()
    {
        if (IsOwner)
            controls.Disable();

        controls.Player.Move.performed -= OnMoveInput;
        controls.Player.Move.canceled -= OnMoveInput;
    }

    private void OnMoveInput(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;
        rb.linearVelocityX = movementInput.x * speed;
    }
}