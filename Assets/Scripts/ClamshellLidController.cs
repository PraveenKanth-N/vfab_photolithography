using UnityEngine;

public class ClamshellLidController : MonoBehaviour
{
    [Header("Angle Constraints")]
    public float closedXAngle = 0f;   // Completely flat/shut position
    public float openedXAngle = -75f; // Swung back open angle offset
    public float transitionSpeed = 3f;

    public bool isOpen = false;
    private float targetXRotation = 0f;

    void Start()
    {
        // Default to locked shut state at setup
        targetXRotation = closedXAngle;
        transform.localRotation = Quaternion.Euler(targetXRotation, 0f, 0f);
    }

    void Update()
    {
        // Smoothly rotate the hinge toward our target angle configuration using Lerp
        float currentX = Mathf.MoveTowardsAngle(transform.localRotation.eulerAngles.x, targetXRotation, transitionSpeed * 100f * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(currentX, 0f, 0f);
    }

    // Call this function via a UI Button on your wall control panel
    public void ToggleLidState()
    {
        isOpen = !isOpen;
        targetXRotation = isOpen ? openedXAngle : closedXAngle;
        
        Debug.Log(isOpen ? "POLOS Coater: Shield lid opened. Vacuum chuck exposed." : "POLOS Coater: Shield lid sealed for execution cycle.");
    }
}