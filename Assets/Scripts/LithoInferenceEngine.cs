using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.InferenceEngine; 
using TMPro;

public class LithoInferenceEngine : MonoBehaviour
{
    [Header("Model Assets")]
    public ModelAsset onnxModelAsset;
    public ModelAsset chemicalModelAsset; // Stage 2 post-bake tracking slot
    
    private Model runtimeModel;
    private Model runtimeChemicalModel;
    private Worker inferenceWorker; 
    private Worker chemicalInferenceWorker; 

    [Header("UI Input Elements")]
    public RawImage maskPreviewDisplay;   
    public Slider cdSlider; // ADDED: Slider reference to link up the dynamic CD input
    public TextMeshProUGUI cdValueText; // ADDED: Text label to display the current CD value
    public TextMeshProUGUI nmValueText; // ADDED: True nanometer reading

    [Header("Process Parameters")]
    public float currentDose = 30.0f;
    public float currentDefocus = 0.0f;
    public float currentNA = 1.2f;
    
    [Header("Dynamic Mask Parameterization")]
    public int currentCD = 24; // ADDED: Tracks real-time grid cell allocations (equivalent to ~nm sizes)

    [HideInInspector] public bool isSystemExposing = false;
    [HideInInspector] public int maskDropdownIndexOverride = 0;

    // Core data buffers passed to external visualizers
    [HideInInspector] public float[] outputResultsData;
    [HideInInspector] public float[] chemicalResultsData; 

    private float[] inputRawArray;
    private float[] nchwInputArray;
    private Texture2D maskTexture2D;

    private int resolution = 512;
    private int channels = 4; // Mask, Dose, Defocus, NA

    void Start()
    {
        // Programmatically bind the slider so it updates texts instantly on drag
        if (cdSlider != null)
        {
            cdSlider.onValueChanged.RemoveAllListeners();
            cdSlider.onValueChanged.AddListener(delegate { OnCDSliderChanged(); });
            
            // Force an initial update
            OnCDSliderChanged();
        }
    }

    void OnEnable()
    {
        if (onnxModelAsset == null || chemicalModelAsset == null)
        {
            Debug.LogError("[Inference Engine] Missing ONNX assignment references on inspector slots!");
            return;
        }

        // Initialize Stage 1 worker pipeline
        runtimeModel = ModelLoader.Load(onnxModelAsset);
        runtimeChemicalModel = ModelLoader.Load(chemicalModelAsset);

#if UNITY_WEBGL && !UNITY_EDITOR
        inferenceWorker = new Worker(runtimeModel, BackendType.GPUPixel);
        chemicalInferenceWorker = new Worker(runtimeChemicalModel, BackendType.GPUPixel);
#else
        inferenceWorker = new Worker(runtimeModel, BackendType.GPUCompute);
        chemicalInferenceWorker = new Worker(runtimeChemicalModel, BackendType.GPUCompute);
#endif

        inputRawArray = new float[1 * resolution * resolution * channels];
        maskTexture2D = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        maskPreviewDisplay.texture = maskTexture2D;

        // Hook up slider listeners automatically if assigned to prevent inspector misses
        if (cdSlider != null)
        {
            cdSlider.onValueChanged.AddListener(delegate { OnCDSliderChanged(); });
            currentCD = Mathf.RoundToInt(cdSlider.value);
        }

        OnMaskSelectionChanged();
    }

    // ADDED: Listens directly to UI slider events to dynamically alter geometries
    public void OnCDSliderChanged()
    {
        if (cdSlider == null) return;
        currentCD = Mathf.RoundToInt(cdSlider.value);
        if (cdValueText != null) cdValueText.text = "Critical Dimension: " + currentCD.ToString() ;
        if (nmValueText != null) nmValueText.text = (currentCD * 3.91f).ToString("F2") + " nm";
        
        OnMaskSelectionChanged();

        // Refresh the thumbnail gallery UI buttons to reflect the new dynamic sizes
        if (PhotomaskGalleryUI.Instance != null && PhotomaskGalleryUI.Instance.gameObject.activeInHierarchy == false)
        {
            // Even if the root canvas is off, we can call this if the instance is active
            PhotomaskGalleryUI.Instance.RefreshThumbnails();
        }
        else if (PhotomaskGalleryUI.Instance != null)
        {
            PhotomaskGalleryUI.Instance.RefreshThumbnails();
        }
    }

    public void BakeSelectedMaskMatrix()
    {
        OnMaskSelectionChanged();
    }

    public void OnMaskSelectionChanged()
    {
        int selectedIndex = maskDropdownIndexOverride;
        // Zero out the mask geometry channel cleanly
        for (int i = 0; i < resolution * resolution; i++) inputRawArray[i * channels + 0] = 0.0f;

        // FIXED: Re-derived spatial offsets dynamically using 'currentCD' instead of hardcoded numbers
        int cd = currentCD;
        int pitch = Mathf.RoundToInt(cd * 1.33f); // Keeps pitch proportional to mask line sizes to prevent packing collisions

        if (selectedIndex == 0) 
        {
            int startOffset = (resolution - (7 * cd + 6 * pitch)) / 2;
            for (int col = 0; col < 7; col++) BlitVerticalStripe(startOffset + col * (cd + pitch), cd);
        }
        else if (selectedIndex == 1) 
        {
            int startOffset = (resolution - (7 * cd + 6 * pitch)) / 2;
            for (int row = 0; row < 7; row++) BlitHorizontalStripe(startOffset + row * (cd + pitch), cd);
        }
        else if (selectedIndex == 2) 
        {
            int startOffset = (resolution - (8 * cd + 7 * pitch)) / 2;
            for (int r = 0; r < 8; r++) {
                for (int c = 0; c < 8; c++) BlitSquareBlock(startOffset + c * (cd + pitch), startOffset + r * (cd + pitch), cd);
            }
        }
        else if (selectedIndex == 3) { BlitVerticalStripe((resolution - cd) / 2, cd); }
        else if (selectedIndex == 4) { BlitSquareBlock((resolution - cd) / 2, (resolution - cd) / 2, cd); }

        // Redraw preview panel
        Color32[] textureColorBuffer = new Color32[resolution * resolution];
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int idx = y * resolution + x;
                byte maskVal = (byte)(inputRawArray[idx * channels + 0] * 255);
                textureColorBuffer[idx] = new Color32(maskVal, maskVal, maskVal, 255);
            }
        }
        maskTexture2D.SetPixels32(textureColorBuffer);
        maskTexture2D.Apply();

        OnProcessParameterChanged();
    }

    private void BlitVerticalStripe(int xStart, int width)
    {
        for (int y = 0; y < resolution; y++) {
            for (int x = xStart; x < xStart + width; x++) {
                if (x >= 0 && x < resolution) inputRawArray[(y * resolution + x) * channels + 0] = 1.0f;
            }
        }
    }

    private void BlitHorizontalStripe(int yStart, int height)
    {
        for (int y = yStart; y < yStart + height; y++) {
            for (int x = 0; x < resolution; x++) {
                if (y >= 0 && y < resolution) inputRawArray[(y * resolution + x) * channels + 0] = 1.0f;
            }
        }
    }

    private void BlitSquareBlock(int xStart, int yStart, int size)
    {
        for (int y = yStart; y < yStart + size; y++) {
            for (int x = xStart; x < xStart + size; x++) {
                if (x >= 0 && x < resolution && y >= 0 && y < resolution) inputRawArray[(y * resolution + x) * channels + 0] = 1.0f;
            }
        }
    }

    public void SetProcessParameters(float dose, float defocus, float na)
    {
        currentDose = dose;
        currentDefocus = defocus;
        currentNA = na;
        
        OnProcessParameterChanged();
    }

    public void OnProcessParameterChanged()
    {
        float normDose = currentDose / 38.0f;
        float normDefocus = currentDefocus / 45.0f;
        float normNA = currentNA / 1.35f;

        nchwInputArray = new float[1 * channels * resolution * resolution];
        int planeSize = resolution * resolution;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int pixelIdx = y * resolution + x;
                float maskVal = inputRawArray[pixelIdx * channels + 0];

                nchwInputArray[0 * planeSize + pixelIdx] = maskVal;     
                nchwInputArray[1 * planeSize + pixelIdx] = normDose;    
                nchwInputArray[2 * planeSize + pixelIdx] = normDefocus; 
                nchwInputArray[3 * planeSize + pixelIdx] = normNA;      
            }
        }
    }

    // --- EXECUTES AT THE MASK ALIGNER (Pipes data natively into Stage 2 buffer) ---
    public void ExecuteModelExposurePipeline()
    {
        if (inferenceWorker == null) { Debug.LogError("[Litho AI Engine] inferenceWorker is null!"); return; }
        if (nchwInputArray == null) { Debug.LogError("[Litho AI Engine] nchwInputArray is null! Array never initialized!"); return; }
        if (chemicalInferenceWorker == null) { Debug.LogError("[Litho AI Engine] chemicalInferenceWorker is null! Stage 2 model failed to load."); return; }

        Debug.Log("[Litho AI Engine] Processing Phase 1 Aerial Intensity Pass...");

        TensorShape engineInputShape = new TensorShape(1, channels, resolution, resolution);
        using Tensor<float> engineInputTensor = new Tensor<float>(engineInputShape, nchwInputArray);
        
        inferenceWorker.Schedule(engineInputTensor);
        using Tensor<float> gpuOutputTensor = inferenceWorker.PeekOutput() as Tensor<float>;
        
        if (gpuOutputTensor != null)
        {
            using Tensor<float> cpuCopyTensor = gpuOutputTensor.ReadbackAndClone() as Tensor<float>;
            outputResultsData = cpuCopyTensor.DownloadToArray(); 

            // --- STAGE 2 PRE-COMPUTE: CONSTRUCT 2-CHANNEL PASSTHROUGH FOOTPRINT FOR CHEMICAL SURROGATE ---
            float normDose = currentDose / 38.0f;
            int planeSize = resolution * resolution;
            float[] chemicalInputBuffer = new float[1 * 2 * resolution * resolution];
            
            for (int i = 0; i < planeSize; i++)
            {
                chemicalInputBuffer[0 * planeSize + i] = outputResultsData[i]; 
                chemicalInputBuffer[1 * planeSize + i] = normDose;             
            }

            TensorShape chemShape = new TensorShape(1, 2, resolution, resolution);
            using Tensor<float> chemInputTensor = new Tensor<float>(chemShape, chemicalInputBuffer);

            chemicalInferenceWorker.Schedule(chemInputTensor);
            using Tensor<float> gpuChemOutput = chemicalInferenceWorker.PeekOutput() as Tensor<float>;

            if (gpuChemOutput != null)
            {
                using Tensor<float> cpuChemCopy = gpuChemOutput.ReadbackAndClone() as Tensor<float>;
                chemicalResultsData = cpuChemCopy.DownloadToArray(); // Stashed and ready for Hotplate retrieval
            }

            // ─── NOTIFY EXTERNAL VISUALIZER SCRIPT FOR SCREEN 1 ───
            LithoVisualizer aerialPlot = GetComponent<LithoVisualizer>();
            if (aerialPlot == null) aerialPlot = GetComponentInParent<LithoVisualizer>();
            if (aerialPlot == null) aerialPlot = GetComponentInChildren<LithoVisualizer>();
            if (aerialPlot == null) aerialPlot = Object.FindAnyObjectByType<LithoVisualizer>();

            if (aerialPlot != null)
            {
                aerialPlot.UpdateAerialPlotDisplay();
            }
            
            Debug.Log("[Litho AI Engine] Stage 1 calculations transferred to LithoVisualizer.");
        }
    }

    // --- INTERACTIVE SUB-ROUTINE: INVOKED BY HOTPLATE TERMINAL INTERLOCK BINDINGS ---
    public void ExecutePostBakeChemicalPipeline()
    {
        if (chemicalResultsData == null)
        {
            Debug.LogWarning("[Litho AI Engine] Cannot execute PEB loop: Data cache is empty!");
            return;
        }

        LithoChemicalVisualizer chemVisualizer = Object.FindAnyObjectByType<LithoChemicalVisualizer>();
        if (chemVisualizer != null)
        {
            chemVisualizer.UpdateDeprotectionPlotDisplay();
            Debug.Log("[Litho AI Engine] Stage 2 view refresh call triggered successfully.");
        }

        // Removed forced development execution here.
        // It should ONLY trigger when the user explicitly presses SwerveDev on the UI!

        // --- DATA ANALYSIS DEBUG FOR HORIZONTAL SLICE (EDGE FOCUS) ---
        int midRow = resolution / 2;
        int startIndex = midRow * resolution;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        // We focus on X = 200 to 312 to capture the edges of the central vertical strip
        int startX = 200;
        int endX = 312;
        sb.Append($"[PEB Edge Analysis] Horizontal slice at Y={midRow}, from X={startX} to X={endX} (EVERY pixel):\n");
        for (int x = startX; x <= endX; x++)
        {
            float val = chemicalResultsData[startIndex + x];
            sb.Append(val.ToString("F3")).Append(" | ");
        }
        Debug.Log(sb.ToString());
    }

    // FIXED: Scaled preview thumbnail geometries down using the slider scaling coefficient
    public Texture2D GenerateStaticPreviewTexture(int targetIndex)
    {
        int localRes = 128; 
        Texture2D previewTex = new Texture2D(localRes, localRes, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[localRes * localRes];

        // Maps 512-mesh scale down to thumbnail size ratio cleanly (~0.25)
        float sizeRatio = (float)currentCD / resolution;
        int cd = Mathf.Clamp(Mathf.RoundToInt(localRes * sizeRatio), 4, 40);
        int pitch = Mathf.RoundToInt(cd * 1.33f);

        for (int y = 0; y < localRes; y++)
        {
            for (int x = 0; x < localRes; x++)
            {
                bool isWhite = false;

                if (targetIndex == 0) 
                {
                    int startOffset = (localRes - (7 * cd + 6 * pitch)) / 2;
                    for (int col = 0; col < 7; col++) {
                        int xStart = startOffset + col * (cd + pitch);
                        if (x >= xStart && x < xStart + cd) isWhite = true;
                    }
                }
                else if (targetIndex == 1) 
                {
                    int startOffset = (localRes - (7 * cd + 6 * pitch)) / 2;
                    for (int row = 0; row < 7; row++) {
                        int yStart = startOffset + row * (cd + pitch);
                        if (y >= yStart && y < yStart + cd) isWhite = true;
                    }
                }
                else if (targetIndex == 2) 
                {
                    int startOffset = (localRes - (8 * cd + 7 * pitch)) / 2;
                    for (int r = 0; r < 8; r++) {
                        for (int c = 0; c < 8; c++) {
                            int xStart = startOffset + c * (cd + pitch); int yStart = startOffset + r * (cd + pitch);
                            if (x >= xStart && x < xStart + cd && y >= yStart && y < yStart + cd) isWhite = true;
                        }
                    }
                }
                else if (targetIndex == 3) { if (x >= (localRes - cd) / 2 && x < (localRes - cd) / 2 + cd) isWhite = true; }
                else if (targetIndex == 4) { if (x >= (localRes - cd) / 2 && x < (localRes - cd) / 2 + cd && y >= (localRes - cd) / 2 && y < (localRes - cd) / 2 + cd) isWhite = true; }

                byte val = (byte)(isWhite ? 255 : 0);
                pixels[y * localRes + x] = new Color32(val, val, val, 255);
            }
        }
        
        previewTex.SetPixels32(pixels);
        previewTex.Apply();
        return previewTex;
    }

    public void PaintBakedPatternOntoObject(GameObject physicalMaskObject)
    {
        if (maskTexture2D == null) return;
        MeshRenderer meshRenderer = physicalMaskObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = physicalMaskObject.GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null)
        {
            Material targetMaterial = meshRenderer.material;
            if (targetMaterial.HasProperty("_MainTex")) targetMaterial.SetTexture("_MainTex", maskTexture2D);
            if (targetMaterial.HasProperty("_BaseMap")) targetMaterial.SetTexture("_BaseMap", maskTexture2D);
        }
    }

    public float[] GetExposureOutputData() { return outputResultsData; }

    void OnDestroy()
    {
        inferenceWorker?.Dispose();
        chemicalInferenceWorker?.Dispose(); 
        if (maskTexture2D != null) Destroy(maskTexture2D);
    }
}