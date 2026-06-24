using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("Look Settings")]
    public float mouseSensitivity = 2f;
    public bool useSmoothing = true;
    public float smoothTime = 0.05f;
    
    [Header("Orientation Fixes")]
    public bool invertXLook = false; 
    public bool invertYLook = false; 

    [Header("Body Reference")]
    public Transform playerBody;     
    
    private float verticalRotation = 0f;
    private bool isCursorLocked = true;

    // Smoothing variables
    private float currentMouseX;
    private float currentMouseY;
    private float mouseXVelocity;
    private float mouseYVelocity;

    void Start()
    {
        LockCursor();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isCursorLocked = !isCursorLocked;
            if (isCursorLocked) LockCursor();
            else UnlockCursor();
        }

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            // NEW UI PROTECTION SENSORS: If our custom photomask collage gallery layout is open, DO NOT lock the cursor!
            if (PhotomaskGalleryUI.Instance != null && PhotomaskGalleryUI.Instance.isGalleryOpen)
            {
                return; 
            }

            if (HelpManualController.Instance != null && HelpManualController.Instance.isOpen)
            {
                return;
            }

            if (CleanroomUIManager.Instance != null && !CleanroomUIManager.Instance.IsAnyPanelOpen())
            {
                LockCursor();
            }
            else if (CleanroomUIManager.Instance == null)
            {
                LockCursor();
            }
        }
    }

    void LateUpdate()
    {
        if (isCursorLocked)
        {
            float currentSensitivity = mouseSensitivity;
#if UNITY_WEBGL && !UNITY_EDITOR
            currentSensitivity *= 0.15f; // WebGL raw delta scaling fix
#endif

            float targetMouseX = Input.GetAxisRaw("Mouse X") * currentSensitivity;
            float targetMouseY = Input.GetAxisRaw("Mouse Y") * currentSensitivity;

            if (invertXLook) targetMouseX *= -1f;
            if (invertYLook) targetMouseY *= -1f;

            if (useSmoothing)
            {
                currentMouseX = Mathf.SmoothDamp(currentMouseX, targetMouseX, ref mouseXVelocity, smoothTime);
                currentMouseY = Mathf.SmoothDamp(currentMouseY, targetMouseY, ref mouseYVelocity, smoothTime);
            }
            else
            {
                currentMouseX = targetMouseX;
                currentMouseY = targetMouseY;
            }

            verticalRotation -= currentMouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f); 
            transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

            if (playerBody != null)
            {
                playerBody.Rotate(Vector3.up * currentMouseX); 
            }
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

    public void ForceUnlockForUI()
    {
        isCursorLocked = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ForceLockFromUI()
    {
        isCursorLocked = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
