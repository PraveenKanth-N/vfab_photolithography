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
        // Use GetAxisRaw instead of GetAxis to eliminate Unity's built-in input smoothing/acceleration.
        // This makes WASD respond instantly with zero lag on startup and zero momentum when keys are released!
        moveX = Input.GetAxisRaw("Horizontal"); // A (-1), D (1), or 0
        moveZ = Input.GetAxisRaw("Vertical");   // S (-1), W (1), or 0
    }

    void FixedUpdate()
    {
        // Calculate forward and right vectors purely on the horizontal XZ plane
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 moveDirection = (forward * moveZ + right * moveX).normalized;
        
        // Apply crisp velocity directly without vertical drift
        rb.linearVelocity = moveDirection * moveSpeed;
        rb.angularVelocity = Vector3.zero;
    }
}