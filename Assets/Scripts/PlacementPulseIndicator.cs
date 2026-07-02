using UnityEngine;

public class PlacementPulseIndicator : MonoBehaviour
{
    [Tooltip("The keyword in the held object's name that triggers this dock (e.g., 'toplid' or 'carrier')")]
    public string requiredKeyword = "carrier";

    [Tooltip("The color of the pulsing indicator")]
    public Color pulseColor = new Color(0f, 1f, 1f, 0.6f); // Cyan default

    [Tooltip("Size of the indicator pad")]
    public float indicatorScale = 0.8f;

    private GameObject pulseVisual;
    private Material pulseMaterial;
    private bool isPulsing = false;
    private float pulseSpeed = 3f;

    private void Awake()
    {
        // Dynamically inject wafer compatibility into any existing carrier dock!
        // This ensures the table docks automatically pulse when the player holds a wafer, guiding them to the carrier.
        if (requiredKeyword.ToLower().Contains("carrier") && !requiredKeyword.ToLower().Contains("wafer"))
        {
            requiredKeyword += ",wafer,substrate";
        }
    }

    private void Start()
    {
        // Programmatically generate the Quad primitive
        pulseVisual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        pulseVisual.name = "PulseVisualIndicator";
        
        // Strip colliders so it doesn't interfere with raycasts or physics
        Destroy(pulseVisual.GetComponent<Collider>());

        pulseVisual.transform.SetParent(this.transform, false);
        
        // Lay it flat on the ground/surface (X rotated 90 degrees)
        pulseVisual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        
        // Apply custom size and slight Y offset to prevent Z-fighting with the table
        pulseVisual.transform.localScale = new Vector3(indicatorScale, indicatorScale, indicatorScale);
        pulseVisual.transform.localPosition = new Vector3(0f, 0.005f, 0f);

        // Create WebGL-safe transparent unlit material
        Renderer rend = pulseVisual.GetComponent<Renderer>();
        Shader spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader != null)
        {
            pulseMaterial = new Material(spriteShader);
            rend.material = pulseMaterial;
            pulseMaterial.color = new Color(pulseColor.r, pulseColor.g, pulseColor.b, 0f); // start invisible
        }

        pulseVisual.SetActive(false);
    }

    private void Update()
    {
        // For wafer carrier docks, during active workflow, pulsing is continuously evaluated against workflow steps!
        WorkflowManager wm = Object.FindAnyObjectByType<WorkflowManager>();
        bool isCarrierDock = gameObject.name.ToLower().Contains("dock") || requiredKeyword.ToLower().Contains("carrier");
        
        if (wm != null && wm.workflowActive && isCarrierDock)
        {
            bool targetPulse = false;
            string dockName = gameObject.name.ToLower();
            int s = wm.currentStep;

            if (dockName.Contains("dock1"))
            {
                targetPulse = (s == 21 || s == 22 || s == 52 || s == 53 || s == 60 || s == 62);
            }
            else if (dockName.Contains("dock2"))
            {
                targetPulse = (s == 23 || s == 39 || s == 41 || s == 50);
            }
            else if (dockName.Contains("dock3"))
            {
                targetPulse = (s == 63 || s == 70);
            }

            if (isPulsing != targetPulse)
            {
                SetPulseActive(targetPulse);
            }
        }

        if (isPulsing && pulseMaterial != null)
        {
            // Smoothly pulse the alpha channel
            float alpha = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f * pulseColor.a;
            pulseMaterial.color = new Color(pulseColor.r, pulseColor.g, pulseColor.b, alpha);
        }
    }

    public void CheckAndTogglePulse(string heldItemName)
    {
        WorkflowManager wm = Object.FindAnyObjectByType<WorkflowManager>();
        bool isCarrierDock = gameObject.name.ToLower().Contains("dock") || requiredKeyword.ToLower().Contains("carrier");
        
        // During active workflow, carrier dock pulsing is handled completely by Update() matching workflow steps!
        if (wm != null && wm.workflowActive && isCarrierDock)
        {
            return;
        }

        if (string.IsNullOrEmpty(heldItemName))
        {
            SetPulseActive(false);
            return;
        }

        string nameLower = heldItemName.ToLower();
        bool shouldPulse = false;
        
        string[] keywords = requiredKeyword.ToLower().Split(',');
        foreach (var keyword in keywords)
        {
            if (nameLower.Contains(keyword.Trim()))
            {
                shouldPulse = true;
                break;
            }
        }

        SetPulseActive(shouldPulse);
    }

    private void SetPulseActive(bool active)
    {
        isPulsing = active;
        if (pulseVisual != null)
        {
            pulseVisual.SetActive(active);
            if (!active && pulseMaterial != null)
            {
                pulseMaterial.color = new Color(pulseColor.r, pulseColor.g, pulseColor.b, 0f);
            }
        }
    }
}
