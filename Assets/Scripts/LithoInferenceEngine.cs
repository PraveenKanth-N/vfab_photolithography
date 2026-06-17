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

    [Header("Process Parameters")]
    public float currentDose = 20.0f;
    public float currentDefocus = 0.0f;
    public float currentNA = 0.8f;

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
        if (onnxModelAsset == null || chemicalModelAsset == null)
        {
            Debug.LogError("[Inference Engine] Missing ONNX assignment references on inspector slots!");
            return;
        }

        // Initialize Stage 1 worker pipeline
        runtimeModel = ModelLoader.Load(onnxModelAsset);
        inferenceWorker = new Worker(runtimeModel, BackendType.GPUCompute);

        // Initialize Stage 2 chemical compute core on GPU
        runtimeChemicalModel = ModelLoader.Load(chemicalModelAsset);
        chemicalInferenceWorker = new Worker(runtimeChemicalModel, BackendType.GPUCompute);

        inputRawArray = new float[1 * resolution * resolution * channels];
        maskTexture2D = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        maskPreviewDisplay.texture = maskTexture2D;

        OnMaskSelectionChanged();
    }

    public void BakeSelectedMaskMatrix()
    {
        int selectedIndex = maskDropdownIndexOverride;
        for (int i = 0; i < resolution * resolution; i++) inputRawArray[i * channels + 0] = 0.0f;

        if (selectedIndex == 0) 
        {
            int cd = 24, pitch = 32; int startOffset = (resolution - (7 * cd + 6 * pitch)) / 2;
            for (int col = 0; col < 7; col++) BlitVerticalStripe(startOffset + col * (cd + pitch), cd);
        }
        else if (selectedIndex == 1) 
        {
            int cd = 24, pitch = 32; int startOffset = (resolution - (7 * cd + 6 * pitch)) / 2;
            for (int row = 0; row < 7; row++) BlitHorizontalStripe(startOffset + row * (cd + pitch), cd);
        }
        else if (selectedIndex == 2) 
        {
            int cd = 24, pitch = 32; int startOffset = (resolution - (8 * cd + 7 * pitch)) / 2;
            for (int r = 0; r < 8; r++) {
                for (int c = 0; c < 8; c++) BlitSquareBlock(startOffset + c * (cd + pitch), startOffset + r * (cd + pitch), cd);
            }
        }
        else if (selectedIndex == 3) { int cd = 32; BlitVerticalStripe((resolution - cd) / 2, cd); }
        else if (selectedIndex == 4) { int cd = 40; BlitSquareBlock((resolution - cd) / 2, (resolution - cd) / 2, cd); }

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

    public void OnMaskSelectionChanged()
    {
        int selectedIndex = maskDropdownIndexOverride;
        for (int i = 0; i < resolution * resolution; i++) inputRawArray[i * channels + 0] = 0.0f;

        if (selectedIndex == 0) 
        {
            int cd = 24, pitch = 32; int startOffset = (resolution - (7 * cd + 6 * pitch)) / 2;
            for (int col = 0; col < 7; col++) BlitVerticalStripe(startOffset + col * (cd + pitch), cd);
        }
        else if (selectedIndex == 1) 
        {
            int cd = 24, pitch = 32; int startOffset = (resolution - (7 * cd + 6 * pitch)) / 2;
            for (int row = 0; row < 7; row++) BlitHorizontalStripe(startOffset + row * (cd + pitch), cd);
        }
        else if (selectedIndex == 2) 
        {
            int cd = 24, pitch = 32; int startOffset = (resolution - (8 * cd + 7 * pitch)) / 2;
            for (int r = 0; r < 8; r++) {
                for (int c = 0; c < 8; c++) BlitSquareBlock(startOffset + c * (cd + pitch), startOffset + r * (cd + pitch), cd);
            }
        }
        else if (selectedIndex == 3) { int cd = 32; BlitVerticalStripe((resolution - cd) / 2, cd); }
        else if (selectedIndex == 4) { int cd = 40; BlitSquareBlock((resolution - cd) / 2, (resolution - cd) / 2, cd); }

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

        // NEW CONNECTION LINK: Force development calculations to run down the chain instantly
        if (TryGetComponent<LithoDevelopmentEngine>(out var devEngine))
        {
            devEngine.ExecuteDevelopmentPass();
        }
    }

    public Texture2D GenerateStaticPreviewTexture(int targetIndex)
    {
        int localRes = 128; 
        Texture2D previewTex = new Texture2D(localRes, localRes, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[localRes * localRes];

        for (int y = 0; y < localRes; y++)
        {
            for (int x = 0; x < localRes; x++)
            {
                bool isWhite = false;

                if (targetIndex == 0) 
                {
                    int cd = 6, pitch = 8; int startOffset = (localRes - (7 * cd + 6 * pitch)) / 2;
                    for (int col = 0; col < 7; col++) {
                        int xStart = startOffset + col * (cd + pitch);
                        if (x >= xStart && x < xStart + cd) isWhite = true;
                    }
                }
                else if (targetIndex == 1) 
                {
                    int cd = 6, pitch = 8; int startOffset = (localRes - (7 * cd + 6 * pitch)) / 2;
                    for (int row = 0; row < 7; row++) {
                        int yStart = startOffset + row * (cd + pitch);
                        if (y >= yStart && y < yStart + cd) isWhite = true;
                    }
                }
                else if (targetIndex == 2) 
                {
                    int cd = 6, pitch = 8; int startOffset = (localRes - (8 * cd + 7 * pitch)) / 2;
                    for (int r = 0; r < 8; r++) {
                        for (int c = 0; c < 8; c++) {
                            int xStart = startOffset + c * (cd + pitch); int yStart = startOffset + r * (cd + pitch);
                            if (x >= xStart && x < xStart + cd && y >= yStart && y < yStart + cd) isWhite = true;
                        }
                    }
                }
                else if (targetIndex == 3) { int cd = 8; if (x >= (localRes - cd) / 2 && x < (localRes - cd) / 2 + cd) isWhite = true; }
                else if (targetIndex == 4) { int cd = 10; if (x >= (localRes - cd) / 2 && x < (localRes - cd) / 2 + cd && y >= (localRes - cd) / 2 && y < (localRes - cd) / 2 + cd) isWhite = true; }

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