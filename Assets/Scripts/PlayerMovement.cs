using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;       
    public float turnSpeed = 200f;     

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");
        float turnInput = Input.GetAxis("Horizontal");

        Vector3 move = (transform.forward * vertical + transform.right * horizontal)
                        * moveSpeed * Time.fixedDeltaTime;

        Vector3 turn = new Vector3(0f, turnInput * turnSpeed * Time.fixedDeltaTime, 0f);

        rb.MovePosition(transform.position + move);
        Quaternion turnRotation = Quaternion.Euler(turn);
        rb.MoveRotation(rb.rotation * turnRotation);
    }
}