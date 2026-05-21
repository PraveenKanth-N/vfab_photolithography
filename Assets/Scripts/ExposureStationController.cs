using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ExposureStationController : MonoBehaviour
{
    public enum ExposureState { Empty, SubstrateLoaded, Sealed, Exposing, Complete }

    [Header("Process Tracking")]
    public ExposureState currentStationState = ExposureState.Empty;
    private float exposureTimer = 0f;

    [Header("ML Input Parameters")]
    public float exposureTimeSetting = 5.0f;     
    public float uvIntensitySetting = 15.0f;     

    [Header("Equipment Interlock Variables")]
    public bool isVacuumSealed = false;
    public bool isMaskTrayInserted = false;

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

    [HideInInspector] public bool wasWaferExposedSuccessfully = false;
    [HideInInspector] public float loggedTimeValue = 0f;
    [HideInInspector] public float loggedIntensityValue = 0f;

    void Start()
    {
        // 1. FORCE PROGRAMMATIC BUTTON HOOKS - Bypasses Unity Inspector canvas wiring entirely!
        if (vacuumButton != null) { vacuumButton.onClick.RemoveAllListeners(); vacuumButton.onClick.AddListener(ToggleVacuumClamp); }
        if (maskTrayButton != null) { maskTrayButton.onClick.RemoveAllListeners(); maskTrayButton.onClick.AddListener(ToggleMaskTrayPosition); }
        if (executeExposureButton != null) { executeExposureButton.onClick.RemoveAllListeners(); executeExposureButton.onClick.AddListener(TriggerUVLampIgnition); }

        // 2. FORCE AUTOMATED SLIDER LISTENERS - Resolves frozen sliders instantly!
        if (timeSlider != null) timeSlider.onValueChanged.AddListener(delegate { CapturePanelInputs(); });
        if (intensitySlider != null) intensitySlider.onValueChanged.AddListener(delegate { CapturePanelInputs(); });

        InitializeSliders();
        if (uvChamberLightSource != null) uvChamberLightSource.SetActive(false);

        UpdateExposureStationUI("System initialized. Drop wafer onto dock.");
    }

    void Update()
    {
        // Keep readouts up to date continuously
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

        // Keep button states responsive dynamically
        ManageButtonInteractability();
    }

    public void RegisterWaferInsertion(GameObject wafer)
    {
        currentStationState = ExposureState.SubstrateLoaded;
        UpdateExposureStationUI("Substrate logged onto vacuum chuck assembly. Secure interlocks.");
    }

    void ToggleVacuumClamp()
    {
        isVacuumSealed = !isVacuumSealed;
        if (waferDockPosition != null)
        {
            waferDockPosition.localPosition += new Vector3(0f, isVacuumSealed ? 0.008f : -0.008f, 0f);
        }
        UpdateExposureStationUI(isVacuumSealed ? "Vacuum matrix active. Contact proximity locked." : "Vacuum matrix vented. Proximity released.");
    }

    void ToggleMaskTrayPosition()
    {
        if (currentStationState == ExposureState.Exposing) return;

        if (currentStationState == ExposureState.SubstrateLoaded || currentStationState == ExposureState.Complete)
        {
            UpdateExposureStationUI("ERROR: Interlock Active. Cannot move photomask tray while substrate is docked.");
            Debug.LogWarning("SUSS Aligner: Tray movement blocked to prevent physical contact damage to mask reticle.");
            return; 
        }

        isMaskTrayInserted = !isMaskTrayInserted;

        if (maskTrayTransform != null)
        {
            StopAllCoroutines();
            StartCoroutine(AnimateMaskTray(isMaskTrayInserted ? new Vector3(0.4f, 0f, 0.2f) : new Vector3(0.4f, 0f, 0.8f)));
        }
    }

    IEnumerator AnimateMaskTray(Vector3 targetPos)
    {
        while (Vector3.Distance(maskTrayTransform.localPosition, targetPos) > 0.005f)
        {
            maskTrayTransform.localPosition = Vector3.Lerp(maskTrayTransform.localPosition, targetPos, Time.deltaTime * 8f);
            yield return null;
        }
        maskTrayTransform.localPosition = targetPos;
    }

    void TriggerUVLampIgnition()
    {
        if (!isVacuumSealed)
        {
            UpdateExposureStationUI("ERROR: Safety lock failure! Vacuum check unsealed.");
            return;
        }
        if (!isMaskTrayInserted)
        {
            UpdateExposureStationUI("ERROR: Safety lock failure! Photomask carrier tray missing.");
            return;
        }

        StartCoroutine(UVExposureCycleRoutine());
    }

    IEnumerator PlayerForcedStateOverrideCheck()
    {
        // Fallback helper to confirm wafer state detection
        yield return null;
    }

    IEnumerator UVExposureCycleRoutine()
    {
        currentStationState = ExposureState.Exposing;
        exposureTimer = 0f;

        if (uvChamberLightSource != null) uvChamberLightSource.SetActive(true);

        while (exposureTimer < exposureTimeSetting)
        {
            exposureTimer += Time.deltaTime;
            UpdateExposureStationUI($"EXPOSING MATRIX... Lamp Stable | Elapsed: {exposureTimer:F1}s / {exposureTimeSetting:F1}s");
            yield return null;
        }

        if (uvChamberLightSource != null) uvChamberLightSource.SetActive(false);
        CompleteExposureSequence();
    }

    void CompleteExposureSequence()
    {
        currentStationState = ExposureState.Complete;
        wasWaferExposedSuccessfully = true;
        
        loggedTimeValue = exposureTimeSetting;
        loggedIntensityValue = uvIntensitySetting;

        Debug.Log($"ML DATA POINT LOGGED (EXPOSURE): Time={loggedTimeValue}s, Intensity={loggedIntensityValue}mW/cm²");
        UpdateExposureStationUI("Exposure finished! Latent structural bonds transformed.");
    }

    void ManageButtonInteractability()
    {
        if (executeExposureButton != null)
        {
            executeExposureButton.interactable = (currentStationState != ExposureState.Empty && currentStationState != ExposureState.Exposing);
        }
    }

    void UpdateExposureStationUI(string message)
    {
        if (stationStatusText != null) stationStatusText.text = "SUSS Aligner: " + message;
    }
}