using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float turnSpeed = 200f;
    [SerializeField] private float sprintMultiplier = 2f;

    private Rigidbody rb;
    private PlayerInput input;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        input = GetComponent<PlayerInput>();
    }

    void FixedUpdate()
    {
        if (input == null) return;

        Vector2 moveInput = input.MovementInput;
        if (moveInput == Vector2.zero) return;

        float currentSpeed = moveSpeed;
        if (input.IsSprinting)
            currentSpeed *= sprintMultiplier;

        Vector3 move = transform.forward * moveInput.y + transform.right * moveInput.x;
        move *= currentSpeed * Time.fixedDeltaTime;

        rb.MovePosition(rb.position + move);

        if (moveInput.x != 0)
        {
            float turn = moveInput.x * turnSpeed * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn, 0f));
        }
    }
}


