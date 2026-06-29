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
        if (isPulsing && pulseMaterial != null)
        {
            // Smoothly pulse the alpha channel
            float alpha = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f * pulseColor.a;
            pulseMaterial.color = new Color(pulseColor.r, pulseColor.g, pulseColor.b, alpha);
        }
    }

    public void CheckAndTogglePulse(string heldItemName)
    {
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

        // WORKFLOW RESTRICTIONS:
        WorkflowManager wm = Object.FindAnyObjectByType<WorkflowManager>();
        if (wm != null && nameLower.Contains("carrier"))
        {
            // Only docks 1 and 2 should highlight during the first transport (Step 21)
            if (wm.currentStep <= 22 && gameObject.name.ToLower().Contains("dock3"))
            {
                shouldPulse = false;
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
