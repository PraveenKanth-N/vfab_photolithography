using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScannerController : MonoBehaviour
{
    [Header("Mask Textures (B/D node)")]
    public Material brightFieldMaterial; // Clear white material asset
    public Material darkFieldMaterial;   // Pitch black material asset
    public Renderer maskRenderer;        // The flat 3D Plane primitive acting as the mask plate

    [Header("Scanner Optical Inputs")]
    public float targetWavelength = 193f; // Default fixed advanced node wavelength (nm)
    private float exposureDose = 0f;      // Alpha variable
    private float lensDefocus = 0f;       // Beta variable
    private bool isAligned = false;       // Alignment marker gate check

    [Header("UI Interaction Controls")]
    public TMP_Dropdown maskFieldDropdown;    // Toggles between Bright Field and Dark Field
    public Slider doseSlider;             // Math variable slider: -12% to +12%
    public Slider defocusSlider;          // Math variable slider: -80nm to +80nm
    public Button alignButton;            // Simple marker adjustment action button
    public Button exposeButton;           // Trigger button to fire payload to Python backend
    public Text scannerStatusText;

    void Start()
    {
        // 1. Enforce process window limits on your screen sliders
        if (doseSlider != null) { doseSlider.minValue = -12f; doseSlider.maxValue = 12f; doseSlider.value = 0f; }
        if (defocusSlider != null) { defocusSlider.minValue = -80f; defocusSlider.maxValue = 80f; defocusSlider.value = 0f; }

        // 2. Map input listeners
        if (maskFieldDropdown != null) maskFieldDropdown.onValueChanged.AddListener(OnMaskFieldChanged);
        if (alignButton != null) alignButton.onClick.AddListener(PerformMarkerAlignment);
        if (exposeButton != null) exposeButton.onClick.AddListener(TriggerOpticalExposure);

        // Exposure button stays locked until wafer is prebaked and aligned!
        if (exposeButton != null) exposeButton.interactable = false;
        
        UpdateStatusDisplay("Scanner Ready. Awaiting baked wafer placement...");
    }

    void OnMaskFieldChanged(int index) {
        if (maskRenderer == null) return;

        // Directly swaps the material property block to change field behavior
        if (index == 0 && brightFieldMaterial != null)
        {
            maskRenderer.material = brightFieldMaterial;
            UpdateStatusDisplay("Switched to Bright-Field Mask configuration.");
        }
        else if (index == 1 && darkFieldMaterial != null)
        {
            maskRenderer.material = darkFieldMaterial;
            UpdateStatusDisplay("Switched to Dark-Field Mask configuration.");
        }
    }

    void PerformMarkerAlignment()
    {
        // Simulates matching the wafer alignment markers to the photomask overlay rules
        isAligned = true;
        UpdateStatusDisplay("Alignment markers matched under viewport! Focus locked.");
        CheckExposureReadiness();
    }

    void CheckExposureReadiness()
    {
        // Gate check constraint logic
        if (isAligned)
        {
            if (exposeButton != null) exposeButton.interactable = true;
            UpdateStatusDisplay("Ready! Set your process variables and click Expose.");
        }
    }

    void TriggerOpticalExposure() {
        // 1. Capture exact slider float positions at runtime
        exposureDose = doseSlider != null ? doseSlider.value : 0f;
        lensDefocus = defocusSlider != null ? defocusSlider.value : 0f;

        // 2. Identify the active mask field index (0 = Bright, 1 = Dark)
        string activeField = maskFieldDropdown != null ? maskFieldDropdown.options[maskFieldDropdown.value].text : "Unknown";

        // 3. Print the finalized telemetry payload to the log console
        string payloadSummary = $"[PAYLOAD DATA LOCKED]\n" +
                                $"- Wavelength: {targetWavelength}nm\n" +
                                $"- Selected Mask: {activeField}\n" +
                                $"- Exposure Dose (Alpha Offset): {exposureDose:F2}%\n" +
                                $"- Lens Defocus (Beta Shift): {lensDefocus:F2}nm";

        UpdateStatusDisplay("Payload broadcasted successfully!");
        Debug.Log(payloadSummary); 
    }

    void UpdateStatusDisplay(string message)
    {
        if (scannerStatusText != null) scannerStatusText.text = "Scanner: " + message;
    }
}