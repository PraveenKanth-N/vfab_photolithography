using UnityEngine;

public class MachineInteractionAnchor : MonoBehaviour
{
    [Header("Target Configuration")]
    [Tooltip("Type the exact name of the machine this anchor controls: Hotplate, SpinCoater, Dispenser, or Exposure")]
    public string machineName; 

    private Renderer meshRenderer;
    private Color originalColor;
    
    [Header("Glow Customization")]
    [ColorUsage(true, true)] // This enables the HDR color picker for intensive glowing looks
    public Color hoverGlowColor = new Color(1.0f, 0.6f, 0.0f, 1.0f); // Default Amber
    
    [Tooltip("Speed of the pulse animation")]
    public float pulseSpeed = 4f;

    void Start()
    {
        meshRenderer = GetComponent<Renderer>();
        if (meshRenderer != null)
        {
            // Cache the starting emission color (which is zero/black, making it invisible in our shader)
            originalColor = meshRenderer.material.GetColor("_EmissionColor");
        }
        else
        {
            Debug.LogError($"[Interaction Anchor] Missing Mesh Renderer on {gameObject.name}. Hover highlights will not display.");
        }
    }

    void Update()
    {
        if (meshRenderer != null)
        {
            // Calculate a pulsing multiplier between 0.2 and 1.0 for a continuous breathing effect
            float pulseMultiplier = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; 
            pulseMultiplier = Mathf.Clamp(pulseMultiplier, 0.2f, 1.0f);
            
            Color currentPulse = hoverGlowColor * pulseMultiplier;
            meshRenderer.material.SetColor("_EmissionColor", currentPulse);
        }
    }

    // Triggers automatically when the player's mouse cursor moves over the Box Collider
    void OnMouseEnter()
    {
        // Removed: Pulse is now infinite
    }

    // Triggers automatically when the player's mouse cursor leaves the Box Collider bounds
    void OnMouseExit()
    {
        // Removed: Pulse is now infinite
    }

    // Triggers automatically when clicking directly on the cube anchor zone
    void OnMouseDown()
    {
        var mainWindow = Object.FindAnyObjectByType<MainWindowController>();
        if (mainWindow != null && mainWindow.IsPanelCurrentlyOpen()) return;

        if (CleanroomUIManager.Instance != null)
        {
            // Open up the screen space cockpit overlay panel
            CleanroomUIManager.Instance.OpenMachinePanel(machineName);
        }
        else
        {
            Debug.LogWarning("[Interaction Anchor] Cannot open panel. CleanroomUIManager Instance is missing in the scene.");
        }
    }
}