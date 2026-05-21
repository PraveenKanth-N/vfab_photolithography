using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("Look Settings")]
    public float mouseSensitivity = 2f;
    
    [Header("Orientation Fixes")]
    public bool invertXLook = false; 
    public bool invertYLook = false; 

    [Header("Body Reference")]
    public Transform playerBody;     
    
    private float verticalRotation = 0f;
    private bool isCursorLocked = true;

    void Start()
    {
        // Default to a hidden and locked cursor at startup
        LockCursor();
    }

    void Update()
    {
        // ESCAPE TOGGLE: Tapping Escape now strictly toggles cursor visibility instead of quitting!
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isCursorLocked = !isCursorLocked;
            
            if (isCursorLocked)
            {
                LockCursor();
            }
            else
            {
                UnlockCursor();
            }
        }

        // Only process camera rotations if the cursor is locked (meaning you are actively playing)
        if (isCursorLocked)
        {
            // 1. Get raw, unscaled mouse tracking inputs
            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

            // 2. Handle Reversals / Inversions
            if (invertXLook) mouseX *= -1f;
            if (invertYLook) mouseY *= -1f;

            // 3. VERTICAL LOOK (Up/Down) - Attached to the CAMERA ONLY
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f); 
            transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

            // 4. HORIZONTAL LOOK (Left/Right) - Rotates the ENTIRE PLAYER BODY
            if (playerBody != null)
            {
                playerBody.Rotate(Vector3.up * mouseX); 
            }
        }
        
        // Quick re-lock shortcut: If you click back into the game screen while unlocked, lock it again
        if (!isCursorLocked && Input.GetMouseButtonDown(0))
        {
            // Make sure we didn't just click a wall button before locking
            LockCursor();
        }
    }

    void LockCursor()
    {
        isCursorLocked = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void UnlockCursor()
    {
        isCursorLocked = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}