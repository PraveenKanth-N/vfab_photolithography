using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using Unity.InferenceEngine; // Using your modernized Inference Engine API

public class HotplateController : MonoBehaviour
{
    public enum HotplateState { Idle, WaferLoaded, Baking, Complete }

    [Header("Process Tracking")]
    public HotplateState currentState = HotplateState.Idle;
    private float bakeTimer = 0f;

    [Header("ML Feature Parameters (Inputs)")]
    public float targetTemperature = 90f; 
    public float targetBakeTime = 30f;     

    [Header("UI Control Inputs")]
    public Slider tempSlider;              
    public Slider bakeTimeSlider;          
    public Button startPrebakeButton;      

    [Header("UI Display Outputs (TMPro)")]
    public TMP_Text hotplateStatusText;   
    public TMP_Text temperatureDisplay;   
    public TMP_Text tempSliderReadout;     
    public TMP_Text timeSliderReadout;     

    [Header("Material References")]
    public Material bakedAmberMaterial;   

    [Header("GLB Target Transform References")]
    public Transform topHeatingSurfaceElement; 

    [Header("Inference Engine Outlets")]
    public ModelAsset onnxModelAsset; // Assign your mate's newly updated .onnx file here!
    public EllipsometryMonitor metrologyMonitorScreen; 

    [Header("Debug Adjustments")]
    [Tooltip("Check this box to bypass the bake timer constraints instantly for swift testing passes.")]
    public bool debugSpeedUp = false;

    private Worker worker; 
    private const int Resolution = 512;

    // Local variable cache holding the physical baseline passed directly from the spin coater
    private float cachedSpinCenterThickness = 0f;
    private float currentWaferRPM = 3000f; 

    void Start()
    {
        if (onnxModelAsset != null)
        {
            Model runtimeModel = ModelLoader.Load(onnxModelAsset);
            worker = new Worker(runtimeModel, BackendType.GPUCompute);
        }
        else
        {
            Debug.LogError("[Hotplate] Inference Engine Error: Missing Model Asset assignment container reference.");
        }

        if (startPrebakeButton != null)
        {
            startPrebakeButton.onClick.RemoveAllListeners();
            startPrebakeButton.onClick.AddListener(BeginThermalBakeCycle);
        }

        InitializeSliders();
        UpdateHotplateUI("Stable at baseline room temperature.");

        if (topHeatingSurfaceElement == null)
        {
            Transform foundTop = transform.Find("Top") ?? transform.Find("gltf_embedded_0/Top");
            if (foundTop != null) topHeatingSurfaceElement = foundTop;
        }
    }

    void Update()
    {
        if (currentState == HotplateState.Idle || currentState == HotplateState.WaferLoaded)
        {
            CapturePanelInputs();
        }

        if (currentState == HotplateState.Baking)
        {
            // Debug speed up multiplier handles real-time vs instant testing skip
            bakeTimer += Time.deltaTime * (debugSpeedUp ? 25f : 1f);
            
            if (temperatureDisplay != null)
            {
                float currentTemp = targetTemperature + UnityEngine.Random.Range(-0.3f, 0.3f);
                temperatureDisplay.text = $"{currentTemp:F1}°C";
            }

            if (bakeTimer >= targetBakeTime) 
            {
                currentState = HotplateState.Complete; // Lock out loop immediately
                CompleteSoftBake();
            }
        }
    }

    void InitializeSliders()
    {
        if (tempSlider != null) { tempSlider.minValue = 40f; tempSlider.maxValue = 120f; tempSlider.value = 90f; }
        if (bakeTimeSlider != null) { bakeTimeSlider.minValue = 30f; bakeTimeSlider.maxValue = 90f; bakeTimeSlider.value = 45f; }
    }

    void CapturePanelInputs()
    {
        if (tempSlider != null) targetTemperature = tempSlider.value;
        if (bakeTimeSlider != null) targetBakeTime = bakeTimeSlider.value;

        if (tempSliderReadout != null) tempSliderReadout.text = $"Set Temp: {targetTemperature:F0}°C";
        if (timeSliderReadout != null) timeSliderReadout.text = $"Set Time: {targetBakeTime:F0} Seconds";
    }

    public void ReceiveWaferStateFromSpinCoater(float trackingHCenter, float appliedRPM)
    {
        cachedSpinCenterThickness = trackingHCenter;
        currentWaferRPM = appliedRPM;
        Debug.Log($"[Hotplate Cache Sync] Received genuine coater metrics: {cachedSpinCenterThickness:F1} nm at {currentWaferRPM} RPM.");
    }

    public void RegisterWaferPlacement(GameObject wafer)
    {
        currentState = HotplateState.WaferLoaded;
        
        if (topHeatingSurfaceElement != null)
        {
            wafer.transform.SetParent(topHeatingSurfaceElement); 
            wafer.transform.position = topHeatingSurfaceElement.position + new Vector3(0f, 0.25f, -0.1f);
            wafer.transform.rotation = topHeatingSurfaceElement.rotation; 
        }
        else
        {
            wafer.transform.position = transform.position + new Vector3(0f, 0.5f, 0f);
        }

        UpdateHotplateUI("Substrate registered on heating element. Press Start.");
    }

    public void BeginThermalBakeCycle()
    {
        if (currentState != HotplateState.WaferLoaded) return;

        // SEQUENCE COMPLIANCE LOCKOUT: Substrate must be coated first
        if (cachedSpinCenterThickness <= 0.01f)
        {
            UpdateHotplateUI("ERROR: Substrate must undergo Spin Coating before thermal baking.");
            return;
        }

        currentState = HotplateState.Baking;
        bakeTimer = 0f;
        UpdateHotplateUI("Bake cycle running. Thermally processing substrate matrix...");

        if (MicroscopeInterfaceManager.Instance != null)
        {
            AtomicChamberController chamber = MicroscopeInterfaceManager.Instance.atomicChamberRoot.GetComponent<AtomicChamberController>();
            if (chamber != null)
            {
                float targetThicknessRatio = (targetTemperature < 90f || targetBakeTime < 45f) ? 0.75f : 0.55f;
                StartCoroutine(chamber.AnimateSoftBake(targetThicknessRatio, targetBakeTime * 0.1f));
            }
        }
    }

    void CompleteSoftBake()
    {
        UpdateHotplateUI("Process complete! Thermal profile sequence executed successfully.");

        // Physical look and color swap logic for wafer surface transformation
        Vector3 scanCenter = topHeatingSurfaceElement != null ? topHeatingSurfaceElement.position : transform.position;
        Collider[] hits = Physics.OverlapSphere(scanCenter + new Vector3(0f, 0.1f, 0f), 0.4f);
        foreach (Collider hit in hits)
        {
            if (hit.gameObject.name.ToLower().Contains("wafer"))
            {
                Renderer waferRen = hit.gameObject.GetComponent<Renderer>();
                if (waferRen != null && bakedAmberMaterial != null) waferRen.material = bakedAmberMaterial;
            }
        }

        // --- INFERENCE EXTRACTION USING EXPLICIT NAMED OUTPUT PORTS ---
        if (worker != null)
        {
            int totalPixels = Resolution * Resolution;
            float[] inputs = new float[totalPixels * 3];

            float[] means = { 472.3705139160156f, 100.18104553222656f, 65.00565338134766f };
            float[] stds  = { 129.4272003173828f, 11.57119369506836f, 31.54351806640625f };

            int arrayIdx = 0;
            for (int y = 0; y < Resolution; y++)
            {
                for (int x = 0; x < Resolution; x++)
                {
                    float nx = (x / (float)Resolution) * 2.0f - 1.0f;
                    float ny = (y / (float)Resolution) * 2.0f - 1.0f;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);

                    
                    float hSpinPixel = cachedSpinCenterThickness;
                    float tempPixel = targetTemperature - (1.5f * (r * r));

                    inputs[arrayIdx]     = (hSpinPixel - means[0]) / stds[0];
                    inputs[arrayIdx + 1] = (tempPixel - means[1]) / stds[1];
                    inputs[arrayIdx + 2] = (targetBakeTime - means[2]) / stds[2];
                    arrayIdx += 3;
                }
            }

            TensorShape shape = new TensorShape(totalPixels, 3);
            using Tensor<float> inputTensor = new Tensor<float>(shape, inputs);
            
            worker.Schedule(inputTensor);

            // Fetch the independent layer outputs cleanly by their explicit string descriptors
            Tensor<float> thickTensor = worker.PeekOutput("predicted_thickness") as Tensor<float>;
            Tensor<float> solventTensor = worker.PeekOutput("predicted_solvent") as Tensor<float>;

            if (thickTensor != null && solventTensor != null)
            {
                thickTensor.CompleteAllPendingOperations();
                solventTensor.CompleteAllPendingOperations();

                using Tensor<float> cpuThick = thickTensor.ReadbackAndClone() as Tensor<float>;
                using Tensor<float> cpuSolvent = solventTensor.ReadbackAndClone() as Tensor<float>;

                float[] rawThicknessArray = cpuThick.DownloadToArray();
                float[] rawSolventArray = cpuSolvent.DownloadToArray();

                if (metrologyMonitorScreen != null)
                {
                    metrologyMonitorScreen.ProcessAndDisplayWafers(rawThicknessArray, rawSolventArray);
                }
            }
            else
            {
                Debug.LogError("[Hotplate Engine] Error finding distinct output nodes named 'predicted_thickness' and 'predicted_solvent'!");
            }
        }
    }

    void UpdateHotplateUI(string message) 
    {
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

    void OnDestroy()
    {
        worker?.Dispose();
    }
}