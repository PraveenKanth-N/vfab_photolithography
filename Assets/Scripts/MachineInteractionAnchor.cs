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
            if (!ShouldGlow())
            {
                meshRenderer.material.SetColor("_EmissionColor", originalColor);
                return;
            }

            // Calculate a pulsing multiplier between 0.2 and 1.0 for a continuous breathing effect
            float pulseMultiplier = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; 
            pulseMultiplier = Mathf.Clamp(pulseMultiplier, 0.2f, 1.0f);
            
            Color currentPulse = hoverGlowColor * pulseMultiplier;
            meshRenderer.material.SetColor("_EmissionColor", currentPulse);
        }
    }

    bool ShouldGlow()
    {
        WorkflowManager wm = Object.FindAnyObjectByType<WorkflowManager>();
        if (wm == null) return true; // Sandbox / free-play mode without tutorial
        if (!wm.workflowActive) return false;

        int step = wm.currentStep;
        string lowerName = (machineName + " " + gameObject.name).ToLower();

        if (lowerName.Contains("spin"))
        {
            return step == 1 || step == 2 || step == 7;
        }
        if (lowerName.Contains("hot"))
        {
            return step == 15 || step == 50;
        }
        if (lowerName.Contains("control") || lowerName.Contains("hardware")) // Aligner Hardware Controls
        {
            return step == 26 || step == 27 || step == 34 || step == 35 || step == 40 || step == 43;
        }
        if (lowerName.Contains("exposure") || lowerName.Contains("aligner") || lowerName.Contains("monitor") || lowerName.Contains("machine"))
        {
            return step == 29 || step == 41;
        }
        if (lowerName.Contains("dev") || lowerName.Contains("dispenser")) // Developer Station controls (interaction_dispenser)
        {
            return step == 67 || step == 73;
        }

        return false;
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
        if (CleanroomUIManager.Instance != null && CleanroomUIManager.Instance.IsAnyPanelOpen()) return;
        if (PhotomaskGalleryUI.Instance != null && PhotomaskGalleryUI.Instance.isGalleryOpen) return;
        if (HelpManualController.Instance != null && HelpManualController.Instance.isOpen) return;

        var mainWindow = Object.FindAnyObjectByType<MainWindowController>();
        if (mainWindow != null && mainWindow.IsPanelCurrentlyOpen()) return;

        WelcomeScreenController wsc = Object.FindAnyObjectByType<WelcomeScreenController>();
        if (wsc != null && wsc.gameObject.activeInHierarchy) return;

        WorkflowManager wm = Object.FindAnyObjectByType<WorkflowManager>();
        if (wm != null && wm.contextImgObj != null && wm.contextImgObj.activeInHierarchy) return;

        DeveloperUIManager devUI = Object.FindAnyObjectByType<DeveloperUIManager>();
        if (devUI != null && devUI.gameObject.activeInHierarchy) return;

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