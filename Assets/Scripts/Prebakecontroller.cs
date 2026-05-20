using UnityEngine;
using UnityEngine.UI;

public class PrebakeController : MonoBehaviour
{
    [Header("Process Settings")]
    public float targetBakeTime = 60f; // target setup
    private float currentBakeTimer = 0f;
    private bool isBaking = false;

    [Header("UI References")]
    public Slider bakeTimeSlider;
    public Button startBakeButton;
    public Text statusText; // A simple text box to display info

    [Header("Wafer Material Controls")]
    public Renderer waferRenderer;
    public Material bareSiliconMaterial; // Dark grey look
    public Material coatedResistMaterial; // Glossy amber look

    void Start()
    {
        // 1. Initialize the wafer to the bare look at startup
        if (waferRenderer != null && bareSiliconMaterial != null)
        {
            waferRenderer.material = bareSiliconMaterial;
        }

        // 2. Setup button click listener
        if (startBakeButton != null)
        {
            startBakeButton.onClick.AddListener(BeginBakingProcess);
        }
    }

    void Update()
    {
        if (isBaking)
        {
            currentBakeTimer += Time.deltaTime;

            // Update the slider value visually to show a countdown or progress
            if (bakeTimeSlider != null)
            {
                bakeTimeSlider.value = currentBakeTimer;
            }

            if (currentBakeTimer >= targetBakeTime)
            {
                CompleteBakingProcess();
            }
        }
    }

    void BeginBakingProcess()
    {
        // Guard rail: Make sure the slider configuration is locked
        if (bakeTimeSlider != null)
        {
            targetBakeTime = bakeTimeSlider.value;
        }

        isBaking = true;
        if (startBakeButton != null) startBakeButton.interactable = false;
        if (statusText != null) statusText.text = "Status: Wafer is baking...";
    }

    void CompleteBakingProcess()
    {
        isBaking = false;
        currentBakeTimer = 0f;

        // insight checklist: Swap the material properties visually
        if (waferRenderer != null && coatedResistMaterial != null)
        {
            waferRenderer.material = coatedResistMaterial;
        }

        if (statusText != null) 
        {
            statusText.text = "Status: Prebake Complete! Wafer coated in photoresist.";
        }
        
        Debug.Log("Prebake finished successfully.");
    }
}