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

    [Header("GLB Target Transform References")]
    [Tooltip("Assign the 'Top' child GameObject from your GLB hierarchy container here.")]
    public Transform topHeatingSurfaceElement; 

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

        // Automatically locate the "Top" mesh if not manually assigned in the inspector
        if (topHeatingSurfaceElement == null)
        {
            Transform foundTop = transform.Find("Top") ?? transform.Find("gltf_embedded_0/Top");
            if (foundTop != null)
            {
                topHeatingSurfaceElement = foundTop;
            }
            else
            {
                Debug.LogWarning("[Hotplate] 'Top' surface element child reference not found! Defaulting to parent position offsets.");
            }
        }
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
        
        // --- POSITION SYNCHRONIZATION ---
        // Snap wafer perfectly flush onto the top face of the 'Top' GLB sub-mesh element
        if (topHeatingSurfaceElement != null)
        {
            topHeatingSurfaceElement = transform.Find("Top") ?? 
                               transform.Find("gltf_embedded_0/Top") ?? 
                               transform.GetComponentInChildren<MeshRenderer>().transform; 
            wafer.transform.position = topHeatingSurfaceElement.position + new Vector3(0f, 0.41f, 0f);
            wafer.transform.rotation = topHeatingSurfaceElement.rotation;
            wafer.transform.SetParent(topHeatingSurfaceElement); // Parent it so it stays locked with the hotplate
        }
        else
        {
            wafer.transform.position = transform.position + new Vector3(0f, 0.5f, 0f);
        }

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

        // --- INTEGRATION TRIGGER: STEP 2 (Thermal Matrix Compaction) ---
        if (MicroscopeInterfaceManager.Instance != null)
        {
            AtomicChamberController chamber = MicroscopeInterfaceManager.Instance.atomicChamberRoot.GetComponent<AtomicChamberController>();
            if (chamber != null)
            {
                float targetThicknessRatio = (targetTemperature < 90f || targetBakeTime < 45f) ? 0.75f : 0.55f;
                float animationDuration = targetBakeTime * 0.1f;
                StartCoroutine(chamber.AnimateSoftBake(targetThicknessRatio, animationDuration));
            }
        }
    }

    void CompleteSoftBake()
    {
        currentState = HotplateState.Complete;
        UpdateHotplateUI("Process complete! Thermal profile sequence executed successfully.");

        // Physical Overlap Scan centered directly around the heating plate element bounds
        Vector3 scanCenter = topHeatingSurfaceElement != null ? topHeatingSurfaceElement.position : transform.position;
        Collider[] hits = Physics.OverlapSphere(scanCenter + new Vector3(0f, 0.1f, 0f), 0.4f);
        
        foreach (Collider hit in hits)
        {
            if (hit.gameObject.name.ToLower().Contains("wafer"))
            {
                SpinCoaterController coater = Object.FindAnyObjectByType<SpinCoaterController>();
                
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