using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.InferenceEngine; // Modernized Inference Engine API

public class SpinCoaterController : MonoBehaviour
{
    public enum CoaterState { Empty, WaferLoaded, PRDispensing, PRDispensed, Spinning, Complete }
    
    [Header("Process Tracking")]
    public CoaterState currentCoaterState = CoaterState.Empty;
    private float recipeTimer = 0f;
    private string currentPhaseName = "Idle";

    [Header("UI Controls (Inputs)")]
    public Button startSpinButton;              
    public Button dispenseButton;
    public Slider speedSlider;                  // Min: 2000, Max: 5000
    public Slider timeSlider;                   // Min: 15, Max: 60
    public TMP_Dropdown prDropdown;             // Options: PR-1000, PR-1200, PR-1500

    [Header("UI Display Outputs (TMPro Text)")]
    public TMP_Text coaterStatusText;     
    public TMP_Text speedReadoutText;
    public TMP_Text timeReadoutText;
    public TMP_Text thicknessTextDisplay;       // Live ML thickness value monitor

    [Header("Physical Equipment References")]
    public Transform waferObject; 
    public Transform rotorSpindleChuck;
    public Renderer waferRenderer;              // Wafer mesh rendering target
    private Vector3 originalWaferScale;  
    private Material wetCoatingMaterialInstance;

    [Header("Dynamic Fluid Animations")]
    public float fluidSpreadSpeed = 2.5f;
    public ParticleSystem dropEmitterParticles;  // Tight, dense nozzle stream emitter
    public ParticleSystem fluidSplatterParticles; // Centrifugal waste spray emitter

    [Header("ML Inference Engine Integration")]
    public ModelAsset modelAsset;               // Target: PR_Coating_Model.onnx asset container
    private Worker engine;                      // FIXED: Replaced IWorker with concrete Worker class
    private Texture2D mlDataLookupTexture;
    private const int RADIAL_SAMPLES = 256;

    private ClamshellLidController lidController;
    private GameObject hostedWaferObjectRef = null;

    void Start()
    {
        if (waferObject != null) originalWaferScale = waferObject.localScale;
        
        // Find safety systems in scene context
        lidController = Object.FindAnyObjectByType<ClamshellLidController>();

        // Setup UI Interactive Listener Hooks safely
        if (dispenseButton != null)
        {
            dispenseButton.onClick.RemoveAllListeners(); 
            dispenseButton.onClick.AddListener(TriggerFluidDispenser); 
        }
        
        if (startSpinButton != null)
        {
            startSpinButton.onClick.RemoveAllListeners(); 
            startSpinButton.onClick.AddListener(BeginAutomatedRecipe); 
        }
        
        // Initialize the modern Inference Engine Engine Architecture
        if (modelAsset != null)
        {
            Model model = ModelLoader.Load(modelAsset);
            
            // FIXED: Replaced WorkerFactory.CreateWorker with a direct constructor call
            engine = new Worker(model, BackendType.GPUCompute);
        }
        else
        {
            Debug.LogError("Inference Engine Error: Missing PR_Coating_Model.onnx assignment reference!");
        }

        // Allocate the 1D RGFloat profile texture buffer
        mlDataLookupTexture = new Texture2D(RADIAL_SAMPLES, 1, TextureFormat.RGFloat, false);
        mlDataLookupTexture.wrapMode = TextureWrapMode.Clamp; 
        mlDataLookupTexture.filterMode = FilterMode.Bilinear;

        Color[] clearPixels = new Color[RADIAL_SAMPLES];
        for(int i = 0; i < RADIAL_SAMPLES; i++) clearPixels[i] = new Color(0f, 0f, 0f, 1f); 
        mlDataLookupTexture.SetPixels(clearPixels);
        mlDataLookupTexture.Apply();

        if (waferRenderer != null) waferRenderer.material.SetTexture("_MLDataTex", mlDataLookupTexture);

        InitializeEquipmentUI();
        UpdateCoaterUI("Awaiting substrate insertion...");
    }

    void Update()
    {
        // Only allow sliders to overwrite target profiles when stationary
        if (currentCoaterState != CoaterState.Spinning) 
        {
            CapturePanelInputs();
        }
    }

    void InitializeEquipmentUI()
    {
        if (speedSlider != null) { speedSlider.minValue = 1000f; speedSlider.maxValue = 7000f; speedSlider.value = 3000f; }
        if (timeSlider != null) { timeSlider.minValue = 15f; timeSlider.maxValue = 60f; timeSlider.value = 30f; }
        
        if (prDropdown != null)
        {
            prDropdown.ClearOptions();
            var optionsList = new List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData("PR-1000"),
                new TMP_Dropdown.OptionData("PR-1200"),
                new TMP_Dropdown.OptionData("PR-1500")
            };
            prDropdown.AddOptions(optionsList);
            prDropdown.RefreshShownValue();
        }
        
        if (thicknessTextDisplay != null) thicknessTextDisplay.text = "Thickness: 0.0 nm";
    }

    void CapturePanelInputs()
    {
        float speedVal = speedSlider != null ? speedSlider.value : 3000f;
        float timeVal = timeSlider != null ? timeSlider.value : 30f;

        if (speedReadoutText != null) speedReadoutText.text = $"Set Speed: {speedVal:F0} RPM";
        if (timeReadoutText != null) timeReadoutText.text = $"Set Time: {timeVal:F0} Seconds";
    }

    public void TriggerFluidDispenser()
    {
        if (currentCoaterState != CoaterState.WaferLoaded) return;
        StartCoroutine(DispenseFluidSequence());
    }

    IEnumerator DispenseFluidSequence()
    {
        currentCoaterState = CoaterState.PRDispensing;
        string prName = prDropdown != null ? prDropdown.options[prDropdown.value].text : "PR";
        UpdateCoaterUI($"Dispensing chemical matrix: {prName}...");

        // --- INTEGRATION TRIGGER: STEP 1 (Post-Dispense / Wet Matrix Generation) ---
        if (MicroscopeInterfaceManager.Instance != null)
        {
            // 1. Awaken the hidden microscope chamber subsystem
            MicroscopeInterfaceManager.Instance.SetChamberState(true);
            
            AtomicChamberController chamber = MicroscopeInterfaceManager.Instance.atomicChamberRoot.GetComponent<AtomicChamberController>();
            if (chamber != null)
            {
                // Dynamic Variant Mapping: Adjust initial chemistry bounds based on selected photoresist viscosity profile
                if (prName == "PR-1500") // High Viscosity option
                {
                    chamber.solventCount = 500;
                    chamber.pacCount = 150;
                }
                else if (prName == "PR-1000") // Low Viscosity option
                {
                    chamber.solventCount = 250;
                    chamber.pacCount = 70;
                }
                else // Default standard configuration (PR-1200)
                {
                    chamber.solventCount = 380;
                    chamber.pacCount = 110;
                }

                // Initialize the live, vibrating, fluid molecular matrix simulation frame
                chamber.InitializeWetMatrix();
            }
        }

        if (dropEmitterParticles != null) dropEmitterParticles.Play();

        yield return new WaitForSeconds(0.2f);

        float progressMultiplier = 0f;
        while (progressMultiplier < 1.0f)
        {
            progressMultiplier += Time.deltaTime * (fluidSpreadSpeed / 2f);
            
            // Dynamically paint a soft expanding boundary into our texture array data
            for (int i = 0; i < RADIAL_SAMPLES; i++)
            {
                float radiusNormalized = (float)i / (RADIAL_SAMPLES - 1);
                // If the sample radius is within our current distribution wave front, set coverage to 1
                float coverage = (radiusNormalized <= progressMultiplier) ? 1f : 0f;
                
                // Baseline unspun thickness profile setup (approx 1500nm puddle)
                float initialThickness = (radiusNormalized <= progressMultiplier) ? 1500f : 0f;
                
                mlDataLookupTexture.SetPixel(i, 0, new Color(coverage, initialThickness, 0f, 1f));
            }
            mlDataLookupTexture.Apply();
            
            if (waferRenderer != null) waferRenderer.material.SetTexture("_MLDataTex", mlDataLookupTexture);
            
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);
        currentCoaterState = CoaterState.PRDispensed;
        UpdateCoaterUI("Dispense complete. Chemical wet-film uniform. Secure glass lid to spin.");
    }

    public void BeginAutomatedRecipe()
    {
        if (currentCoaterState != CoaterState.PRDispensed) return;

        if (lidController != null && lidController.isOpen)
        {
            UpdateCoaterUI("ERROR: Safety interlock violation! Chamber must be sealed to spin.");
            return;
        }

        StartCoroutine(AutomatedSpinRecipeRoutine());
    }

    IEnumerator AutomatedSpinRecipeRoutine()
    {
        currentCoaterState = CoaterState.Spinning;
        
        float targetSpinSpeed = speedSlider != null ? speedSlider.value : 3000f;
        float targetSpinTime = timeSlider != null ? timeSlider.value : 30f;

        // --- STAGE 1: AUTOMATED SPREADING PHASE ---
        currentPhaseName = "SPREADING";
        recipeTimer = 0f;
        float spreadDuration = 5f;
        float spreadRPM = 500f;
        
        while (recipeTimer < spreadDuration)
        {
            recipeTimer += Time.deltaTime;
            
            UpdateCoaterUI($"[{currentPhaseName}] Speed: {spreadRPM:F0} RPM | Time Left: {(spreadDuration - recipeTimer):F1}s");
            yield return null;
        }

        // --- STAGE 2: AUTOMATED CASTING PHASE (Inference Engine Driven) ---
        currentPhaseName = "CASTING";
        recipeTimer = 0f;
        if (fluidSplatterParticles != null) fluidSplatterParticles.Play();
        
        if (MicroscopeInterfaceManager.Instance != null)
        {
            AtomicChamberController chamber = MicroscopeInterfaceManager.Instance.atomicChamberRoot.GetComponent<AtomicChamberController>();
            if (chamber != null)
            {
                // Run the molecular shedding animation over the duration of the spin cycle
                StartCoroutine(chamber.AnimateCentrifugalThinning(targetSpinTime));
            }
        }

        while (recipeTimer < targetSpinTime)
        {
            recipeTimer += Time.deltaTime;
            ExecuteMLInferenceLoop(targetSpinSpeed, recipeTimer);

            UpdateCoaterUI($"[{currentPhaseName}] Speed: {targetSpinSpeed:F0} RPM | Time Left: {(targetSpinTime - recipeTimer):F1}s");
            yield return null;
        }

        if (fluidSplatterParticles != null) fluidSplatterParticles.Stop();
        CompleteSpinCycle();
    }

    void ExecuteMLInferenceLoop(float currentRPM, float currentTime) {
        if (engine == null || waferRenderer == null) return;

        // 1. Construct input data array [256 steps, 3 coordinates each]
        float[] inputs = new float[RADIAL_SAMPLES * 3];
        for (int i = 0; i < RADIAL_SAMPLES; i++)
        {
            float radiusNormalized = (float)i / (RADIAL_SAMPLES - 1); 
            inputs[i * 3 + 0] = currentRPM;
            inputs[i * 3 + 1] = currentTime;
            inputs[i * 3 + 2] = radiusNormalized;
        }

        TensorShape shape = new TensorShape(RADIAL_SAMPLES, 3);
        
        // 2. Wrap inputs and schedule execution asynchronously on GPU
        using Tensor<float> inputTensor = new Tensor<float>(shape, inputs);
        engine.Schedule(inputTensor);

        // 3. Peek the raw output tensor reference from the engine
        Tensor<float> gpuOutputTensor = engine.PeekOutput() as Tensor<float>;
        
        if (gpuOutputTensor != null)
        {
            // 4. CRITICAL MODERN STEP: Download the GPU memory back to a CPU-readable copy
            using Tensor<float> cpuCopyTensor = gpuOutputTensor.ReadbackAndClone() as Tensor<float>;
            
            // 5. MODERN API CONVERSION: Safely dump structural data to standard array
            float[] mlPredictions = cpuCopyTensor.DownloadToArray();

            float centerThicknessSample = mlPredictions[0];

            // 6. Parse network predictions straight into your custom dynamic shader texture
            for (int i = 0; i < RADIAL_SAMPLES; i++)
            {
                float predictedThickness = mlPredictions[i * 2 + 0]; 
                float predictedCoverage = mlPredictions[i * 2 + 1];  

                if (i == 0) {
                    centerThicknessSample = predictedThickness;
                    Debug.Log($"Live ML Output - Time: {currentTime:F2}s | Thickness: {centerThicknessSample:F1}nm");
                }

                float normalizedThicknessForShader = Mathf.Clamp01(predictedThickness);
                mlDataLookupTexture.SetPixel(i, 0, new Color(predictedCoverage, predictedThickness, 0f, 1f));
            }
            mlDataLookupTexture.Apply();

            // Feed the live lookup map to your wafer material instance properties
            waferRenderer.material.SetTexture("_MLDataTex", mlDataLookupTexture);

            if (thicknessTextDisplay != null)
            {
                thicknessTextDisplay.text = centerThicknessSample > 0 
                    ? $"Thickness: {centerThicknessSample:F1} nm" 
                    : "Thickness: 0.0 nm";
            }
        }
    }

    void CompleteSpinCycle()
    {
        currentCoaterState = CoaterState.Complete;
        if (waferObject != null) waferObject.localRotation = Quaternion.identity;
        UpdateCoaterUI("Spin cycle complete. Uniform film thickness achieved via neural inference model.");
    }

    // --- RESTORED ORIGINAL PLACEMENT METHOD ---
    public bool RegisterWaferPlacement(GameObject wafer)
    {
        if (currentCoaterState != CoaterState.Empty)
        {
            Debug.LogWarning($"[{gameObject.name}] Placement Rejected: Spin coater state is {currentCoaterState}, not Empty.");
            return false;
        }

        if (lidController != null && !lidController.isOpen)
        {
            Debug.LogWarning($"[{gameObject.name}] Placement Rejected: Cannot insert substrate while safety cover is sealed.");
            return false;
        }

        waferObject = wafer.transform;

        // PARENT COUPLING WITH MATRIX MAINTENANCE
        if (rotorSpindleChuck != null)
        {
            wafer.transform.SetParent(rotorSpindleChuck, true);
            
            // Apply your custom alignment tweaks cleanly
            wafer.transform.localPosition = new Vector3(0.006f, 0.02f, 0.015f); 
            wafer.transform.localRotation = Quaternion.identity;
        }
        else
        {
            wafer.transform.SetParent(transform);
            wafer.transform.position = transform.position + new Vector3(0f, 0.2f, 0f);
            wafer.transform.rotation = Quaternion.identity;
        }

        wafer.transform.localScale = Vector3.one; // Reset local multipliers to baseline unity unity units
        
        // Force the absolute physical proportions across the rendering matrix pipeline
        Vector3 currentWorldScale = wafer.transform.lossyScale;
        wafer.transform.localScale = new Vector3(
            0.65f / (currentWorldScale.x != 0 ? currentWorldScale.x : 1f),
            0.013f / (currentWorldScale.y != 0 ? currentWorldScale.y : 1f),
            0.65f / (currentWorldScale.z != 0 ? currentWorldScale.z : 1f)
        );

        Rigidbody rb = wafer.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        currentCoaterState = CoaterState.WaferLoaded;
        UpdateCoaterUI("Substrate loaded. Ready for chemical dispensing.");
        
        return true;
    }

    public GameObject RemoveWafer()
    {
        if (currentCoaterState == CoaterState.Spinning || currentCoaterState == CoaterState.PRDispensing)
        {
            Debug.LogWarning($"[{gameObject.name}] Extraction Denied: Process active.");
            return null;
        }

        GameObject waferToReturn = null;
        if (rotorSpindleChuck != null && rotorSpindleChuck.childCount > 0)
        {
            foreach (Transform child in rotorSpindleChuck)
            {
                if (child.name.ToLower().Contains("wafer"))
                {
                    waferToReturn = child.gameObject;
                    break;
                }
            }
        }

        if (waferToReturn != null)
        {
            // Reset proportions back to standard world space scale values right before unbinding
            waferToReturn.transform.localScale = new Vector3(0.65f, 0.013f, 0.65f);
        }

        waferObject = null;
        currentCoaterState = CoaterState.Empty;
        UpdateCoaterUI("Awaiting substrate insertion...");

        return waferToReturn;
    }

    void UpdateCoaterUI(string message = "")
    {
        bool canSelectRecipe = (currentCoaterState == CoaterState.WaferLoaded || currentCoaterState == CoaterState.PRDispensed);
        bool canSpinNow = (currentCoaterState == CoaterState.PRDispensed);
        
        if (dispenseButton != null) dispenseButton.interactable = (currentCoaterState == CoaterState.WaferLoaded);
        if (startSpinButton != null) startSpinButton.interactable = canSpinNow;

        if (speedSlider != null) speedSlider.interactable = canSelectRecipe;
        if (timeSlider != null) timeSlider.interactable = canSelectRecipe;
        if (prDropdown != null) prDropdown.interactable = canSelectRecipe;
        
        if (coaterStatusText != null && !string.IsNullOrEmpty(message)) coaterStatusText.text = "POLOS Coater: " + message;
    }

    private void OnDestroy()
    {
        engine?.Dispose();
    }
}