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

    // Triggers automatically when the player's mouse cursor moves over the Box Collider
    void OnMouseEnter()
    {
        if (meshRenderer != null)
        {
            // Inject the HDR color into the shader, turning visibility on
            meshRenderer.material.SetColor("_EmissionColor", hoverGlowColor);
        }
    }

    // Triggers automatically when the player's mouse cursor leaves the Box Collider bounds
    void OnMouseExit()
    {
        if (meshRenderer != null)
        {
            // Reset back to black/zero emission, making the shader invisible again
            meshRenderer.material.SetColor("_EmissionColor", originalColor);
        }
    }

    // Triggers automatically when clicking directly on the cube anchor zone
    void OnMouseDown()
    {
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