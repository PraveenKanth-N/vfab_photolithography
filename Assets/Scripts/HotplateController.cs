using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class HotplateController : MonoBehaviour
{
    public enum HotplateState { Idle, WaferLoaded, Baking, Complete }

    [Header("Process Tracking")]
    public HotplateState currentState = HotplateState.Idle;
    private float bakeTimer = 0f;

    [Header("ML Feature Parameters (Inputs)")]
    public float targetTemperature = 90f; // Configured via Slider
    public float targetBakeTime = 30f;     // Configured via Slider

    [Header("UI Control Inputs")]
    public Slider tempSlider;              // Your temperature adjustment slider
    public Slider bakeTimeSlider;          // Your bake duration adjustment slider
    public Button startPrebakeButton;      // The button to turn on the heat

    [Header("UI Display Outputs (TMPro)")]
    public TMP_Text hotplateStatusText;   
    public TMP_Text temperatureDisplay;   // Digital numeric read-out
    public TMP_Text tempSliderReadout;     // Text above Temp Slider
    public TMP_Text timeSliderReadout;     // Text above Time Slider

    [Header("Material References")]
    public Material bakedAmberMaterial;   

    void Start()
    {
        // Bind the button to our activation function via code
        if (startPrebakeButton != null)
        {
            startPrebakeButton.onClick.RemoveAllListeners();
            startPrebakeButton.onClick.AddListener(BeginThermalBakeCycle);
        }

        InitializeSliders();
        UpdateHotplateUI("Stable at baseline room temperature.");
    }

    void Update()
    {
        // Track panel modifications while idle or when a wafer is just sitting there
        if (currentState == HotplateState.Idle || currentState == HotplateState.WaferLoaded)
        {
            CapturePanelInputs();
        }

        if (currentState == HotplateState.Baking)
        {
            bakeTimer += Time.deltaTime;
            
            // Display simulated high-temperature fluctuations right around target setpoint
            if (temperatureDisplay != null)
            {
                float currentTemp = targetTemperature + Random.Range(-0.3f, 0.3f);
                temperatureDisplay.text = $"{currentTemp:F1}°C";
            }

            if (bakeTimer >= targetBakeTime * 0.1f) // Scaled down time purely for simulation pacing
            {
                CompleteSoftBake();
            }
        }
    }

    void InitializeSliders()
    {
        if (tempSlider != null) { tempSlider.minValue = 80f; tempSlider.maxValue = 120f; tempSlider.value = 90f; }
        if (bakeTimeSlider != null) { bakeTimeSlider.minValue = 30f; bakeTimeSlider.maxValue = 90f; bakeTimeSlider.value = 45f; }
    }

    void CapturePanelInputs()
    {
        if (tempSlider != null) targetTemperature = tempSlider.value;
        if (bakeTimeSlider != null) targetBakeTime = bakeTimeSlider.value;

        if (tempSliderReadout != null) tempSliderReadout.text = $"Set Temp: {targetTemperature:F0}°C";
        if (timeSliderReadout != null) timeSliderReadout.text = $"Set Time: {targetBakeTime:F0} Seconds";
    }

    // Automatically triggered by PlayerInteraction.cs when dropped on the hotplate element surface
    public void RegisterWaferPlacement(GameObject wafer)
    {
        currentState = HotplateState.WaferLoaded;
        UpdateHotplateUI("Substrate registered on heating element. Configure recipe parameters and press Start.");
    }

    // Triggered when clicking the 'Start Prebake' Button
    public void BeginThermalBakeCycle()
    {
        if (currentState != HotplateState.WaferLoaded)
        {
            UpdateHotplateUI("Operation Denied: No target substrate detected in heating zone.");
            return;
        }

        currentState = HotplateState.Baking;
        bakeTimer = 0f;
        UpdateHotplateUI("Bake cycle running. Thermally processing substrate matrix...");
    }

    void CompleteSoftBake()
    {
        currentState = HotplateState.Complete;
        UpdateHotplateUI("Process complete! Thermal profile sequence executed successfully.");

        // Physical Overlap Scan to handle material transformations
        Collider[] hits = Physics.OverlapSphere(transform.position + new Vector3(0f, 0.5f, 0f), 0.6f);
        foreach (Collider hit in hits)
        {
            if (hit.gameObject.name.ToLower().Contains("wafer"))
            {
                // Grab the spin coater state dynamically right at the millisecond of completion
                SpinCoaterController coater = Object.FindAnyObjectByType<SpinCoaterController>();
                
                // ONLY morph the material to dry matte if it was actually spun-coated first
                if (coater != null && coater.currentCoaterState == SpinCoaterController.CoaterState.Complete)
                {
                    Renderer waferRen = hit.gameObject.GetComponent<Renderer>();
                    if (waferRen != null && bakedAmberMaterial != null)
                    {
                        waferRen.material = bakedAmberMaterial;
                    }
                }
            }
        }
        Debug.Log($"ML DATA POINT LOGGED (HOTPLATE): Temp={targetTemperature}°C, Time={targetBakeTime}s");
    }

    void UpdateHotplateUI(string message) {
        if (hotplateStatusText != null) hotplateStatusText.text = "POLOS Hotplate: " + message;
        
        // Unlocked whenever a wafer is present, locked down ONLY while a bake is currently running
        bool canOperate = (currentState == HotplateState.WaferLoaded);
        
        if (startPrebakeButton != null) startPrebakeButton.interactable = canOperate;
        if (tempSlider != null) tempSlider.interactable = (currentState != HotplateState.Baking);
        if (bakeTimeSlider != null) bakeTimeSlider.interactable = (currentState != HotplateState.Baking);

        if (temperatureDisplay != null && currentState != HotplateState.Baking) 
        {
            temperatureDisplay.text = currentState == HotplateState.Idle ? "23.5°C" : $"{targetTemperature:F1}°C";
        }
    }
}