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

    private bool isHovered = false;

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
        if (Camera.main != null)
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit[] hits = Physics.RaycastAll(ray, 7.0f);
            bool aimedAtCube = false;
            foreach (var h in hits)
            {
                if (h.collider != null && (h.collider.gameObject == gameObject || h.collider.transform.IsChildOf(transform)))
                {
                    aimedAtCube = true;
                    break;
                }
            }
            if (aimedAtCube) isHovered = true;
            else if (!aimedAtCube && isHovered && !Input.GetMouseButton(0)) isHovered = false;

            if (aimedAtCube && Input.GetMouseButtonDown(0))
            {
                TriggerPanelOpen();
            }
        }

        if (meshRenderer != null)
        {
            bool activeStep = ShouldGlow();
            if (!activeStep && !isHovered)
            {
                meshRenderer.material.SetColor("_EmissionColor", originalColor);
                return;
            }

            if (activeStep)
            {
                // Calculate a pulsing multiplier between 0.2 and 1.0 for a continuous breathing effect
                float pulseMultiplier = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; 
                pulseMultiplier = Mathf.Clamp(pulseMultiplier, 0.2f, 1.0f);
                Color currentPulse = hoverGlowColor * pulseMultiplier;
                meshRenderer.material.SetColor("_EmissionColor", currentPulse);
            }
            else if (isHovered)
            {
                // Steady glow when hovered by default!
                meshRenderer.material.SetColor("_EmissionColor", hoverGlowColor * 0.85f);
            }
        }
    }

    bool ShouldGlow()
    {
        WorkflowManager wm = Object.FindAnyObjectByType<WorkflowManager>();
        if (wm == null) return true; // Sandbox / free-play mode without tutorial
        if (!wm.workflowActive) return false;

        int step = wm.currentStep;
        if (step < 2) return false; // Do not pulse during startup or welcome screen (steps 0 and 1)

        string lowerName = (machineName + " " + gameObject.name).ToLower();

        if (lowerName.Contains("clean") || lowerName.Contains("sc1") || lowerName.Contains("rca"))
        {
            return step == 103 || step == 104;
        }
        if (lowerName.Contains("oven") || lowerName.Contains("dehydrat"))
        {
            return step == 113 || step == 114;
        }
        if (lowerName.Contains("spin"))
        {
            return step == 2 || step == 3 || step == 7 || step == 12;
        }
        if (lowerName.Contains("hot"))
        {
            return step == 16 || step == 55;
        }
        if (lowerName.Contains("control") || lowerName.Contains("hardware")) // Aligner Hardware Controls
        {
            return step == 28 || step == 29 || step == 36 || step == 37 || step == 43 || step == 47;
        }
        if (lowerName.Contains("exposure") || lowerName.Contains("aligner") || lowerName.Contains("monitor") || lowerName.Contains("machine"))
        {
            return step == 31 || step == 45;
        }
        if (lowerName.Contains("dev") || lowerName.Contains("dispenser")) // Developer Station controls (interaction_dispenser)
        {
            return step == 72 || step == 78;
        }

        return false;
    }

    // Triggers automatically when the player's mouse cursor moves over the Box Collider
    void OnMouseEnter()
    {
        isHovered = true;
    }

    // Triggers automatically when the player's mouse cursor leaves the Box Collider bounds
    void OnMouseExit()
    {
        isHovered = false;
    }

    void OnMouseDown()
    {
        TriggerPanelOpen();
    }

    void TriggerPanelOpen()
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