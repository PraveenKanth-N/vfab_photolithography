using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody rb;

    private float moveX;
    private float moveZ;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        
        rb.useGravity = false;
        rb.freezeRotation = true; 

        // CRUCIAL FOR JOINTS: Set collision detection to Continuous so you never skip 
        // through thin door panels or ProBuilder wall seams at high movement speeds.
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void Update()
    {
        // 1. Capture keyboard layout inputs cleanly every rendered frame
        moveX = Input.GetAxis("Horizontal"); // A (-1), D (1)
        moveZ = Input.GetAxis("Vertical");   // S (-1), W (1)
    }

    void FixedUpdate()
    {
        Vector3 moveDirection = (transform.forward * moveZ + transform.right * moveX).normalized;
        moveDirection.y = 0f; 
        rb.linearVelocity = moveDirection * moveSpeed;
    }
}