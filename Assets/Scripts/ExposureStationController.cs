using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ExposureStationController : MonoBehaviour
{
    [Header("Process Tracking")]
    public bool isExposingNow = false;

    [Header("ML Input Parameters")]
    public float exposureTimeSetting = 5.0f;     
    public float uvIntensitySetting = 15.0f;     

    [Header("Equipment Variables")]
    public bool isVacuumSealed = false;        // Default: Unlocked
    public bool isMaskTrayInserted = true;     // Default: Inserted

    [Header("UI Control Inputs")]
    public Slider timeSlider;
    public Slider intensitySlider;
    public Button vacuumButton;
    public Button maskTrayButton;
    public Button executeExposureButton;

    [Header("UI Display Outputs (TMPro)")]
    public TMP_Text stationStatusText;
    public TMP_Text timeReadoutText;
    public TMP_Text intensityReadoutText;

    [Header("Simulation Visual References")]
    public GameObject uvChamberLightSource;       
    public Transform maskTrayTransform;          
    public Transform waferDockPosition;          

    [Header("Materials")]
    public Material acidIonMaterial;

    [HideInInspector] public bool wasWaferExposedSuccessfully = false;
    [HideInInspector] public float loggedTimeValue = 0f;
    [HideInInspector] public float loggedIntensityValue = 0f;

    void Start()
    {
        // Bind UI triggers cleanly
        if (vacuumButton != null) { vacuumButton.onClick.RemoveAllListeners(); vacuumButton.onClick.AddListener(ToggleVacuumClamp); }
        if (maskTrayButton != null) { maskTrayButton.onClick.RemoveAllListeners(); maskTrayButton.onClick.AddListener(ToggleMaskTrayPosition); }
        if (executeExposureButton != null) { executeExposureButton.onClick.RemoveAllListeners(); executeExposureButton.onClick.AddListener(TriggerUVLampIgnition); }

        if (timeSlider != null) timeSlider.onValueChanged.AddListener(delegate { CapturePanelInputs(); });
        if (intensitySlider != null) intensitySlider.onValueChanged.AddListener(delegate { CapturePanelInputs(); });

        InitializeSliders();
        if (uvChamberLightSource != null) uvChamberLightSource.SetActive(false);

        UpdateExposureStationUI(isVacuumSealed ? "System sealed. Ready for exposure." : "System vented. Substrate accessible.");
    }

    void Update()
    {
        CapturePanelInputs();
    }

    void InitializeSliders()
    {
        if (timeSlider != null) { timeSlider.minValue = 2f; timeSlider.maxValue = 15f; timeSlider.value = 5f; }
        if (intensitySlider != null) { intensitySlider.minValue = 10f; intensitySlider.maxValue = 25f; intensitySlider.value = 15f; }
    }

    void CapturePanelInputs()
    {
        if (timeSlider != null) exposureTimeSetting = timeSlider.value;
        if (intensitySlider != null) uvIntensitySetting = intensitySlider.value;

        if (timeReadoutText != null) timeReadoutText.text = $"Exposure Duration: {exposureTimeSetting:F1} s";
        if (intensityReadoutText != null) intensityReadoutText.text = $"UV Intensity: {uvIntensitySetting:F1} mW/cm²";

        // LOGIC RULE: Execute Exposure button ONLY works when vacuum seal is locked!
        if (executeExposureButton != null)
        {
            executeExposureButton.interactable = isVacuumSealed && !isExposingNow;
        }
    }

    void ToggleVacuumClamp()
    {
        if (isExposingNow) return;
        
        isVacuumSealed = !isVacuumSealed;
        UpdateExposureStationUI(isVacuumSealed ? "Vacuum seal LOCKED. Execute sequence enabled." : "Vacuum seal UNLOCKED. Substrate pickup active.");
    }

    void ToggleMaskTrayPosition()
    {
        if (isExposingNow) return;
        
        isMaskTrayInserted = !isMaskTrayInserted;

        if (maskTrayTransform != null)
        {
            StopAllCoroutines();
            // FIXED: Keeping Y perfectly locked at 0f so the tray never disappears up or down!
            StartCoroutine(AnimateMaskTray(isMaskTrayInserted ? new Vector3(0.4f, 0f, 0.2f) : new Vector3(0.4f, 0f, 0.8f)));
        }
    }

    IEnumerator AnimateMaskTray(Vector3 targetPos)
    {
        while (Vector3.Distance(maskTrayTransform.localPosition, targetPos) > 0.002f)
        {
            maskTrayTransform.localPosition = Vector3.Lerp(maskTrayTransform.localPosition, targetPos, Time.deltaTime * 8f);
            yield return null;
        }
        maskTrayTransform.localPosition = targetPos;
    }

    void TriggerUVLampIgnition()
    {
        if (!isVacuumSealed || isExposingNow) return;
        StartCoroutine(UVExposureCycleRoutine());
    }

    IEnumerator UVExposureCycleRoutine()
    {
        isExposingNow = true;
        float exposureTimer = 0f;

        if (uvChamberLightSource != null) uvChamberLightSource.SetActive(true);

        while (exposureTimer < exposureTimeSetting)
        {
            exposureTimer += Time.deltaTime;
            UpdateExposureStationUI($"EXPOSING... Lamp Stable | Time: {exposureTimer:F1}s / {exposureTimeSetting:F1}s");
            yield return null;
        }

        if (uvChamberLightSource != null) uvChamberLightSource.SetActive(false);
        CompleteExposureSequence();
    }

    void CompleteExposureSequence()
    {
        isExposingNow = false;
        wasWaferExposedSuccessfully = true;
        
        loggedTimeValue = exposureTimeSetting;
        loggedIntensityValue = uvIntensitySetting;

        // --- INTEGRATION TRIGGER: STEP 3 (Photolytic Energy Conversion) ---
        if (MicroscopeInterfaceManager.Instance != null)
        {
            AtomicChamberController chamber = MicroscopeInterfaceManager.Instance.atomicChamberRoot.GetComponent<AtomicChamberController>();
            if (chamber != null)
            {
                // Math: Energy Dose = Intensity * Duration
                float totalEnergyDose = loggedIntensityValue * loggedTimeValue;

                // Physics Mapping: If dosage is optimized, trigger a high-percentage molecular flip.
                // If user leaves sliders at absolute lowest values, conversion efficiency stalls at 25%.
                float targetConversionEfficiency = (totalEnergyDose > 120f) ? 0.85f : 0.25f;
                chamber.AnimateExposure(targetConversionEfficiency, acidIonMaterial);
            }
        }

        Debug.Log($"ML DATA POINT LOGGED (EXPOSURE): Time={loggedTimeValue}s, Intensity={loggedIntensityValue}mW/cm²");
        UpdateExposureStationUI("Exposure complete! Latent image stabilized.");
    }

    void UpdateExposureStationUI(string message)
    {
        if (stationStatusText != null) stationStatusText.text = "SUSS Aligner: " + message;
    }
}