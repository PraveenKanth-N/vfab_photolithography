using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody rb;

    void Start()
    {
        // Add a Rigidbody component automatically if missing to handle wall collisions
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;

        // Lock rotations so the player capsule doesn't tip over like a bowling pin
        rb.freezeRotation = true; 
    }

    void Update()
    {
        // 1. Capture natural keyboard layout inputs (-1 to 1)
        float moveX = Input.GetAxis("Horizontal"); // A (-1), D (1)
        float moveZ = Input.GetAxis("Vertical");   // S (-1), W (1)

        // 2. Calculate direction vectors directly relative to where your camera is looking
        Vector3 moveDirection = (transform.forward * moveZ + transform.right * moveX).normalized;
        
        // 3. Keep movement locked to a level plane (stops you from flying into the air)
        moveDirection.y = 0f; 

        // 4. Move the player's physical workspace position smoothly
        rb.MovePosition(transform.position + moveDirection * moveSpeed * Time.deltaTime);
    }
}