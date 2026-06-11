using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.InferenceEngine; 
using TMPro;

public class LithoInferenceEngine : MonoBehaviour
{
    [Header("Model Assets")]
    public ModelAsset onnxModelAsset;
    private Model runtimeModel;
    private Worker inferenceWorker; 

    [Header("UI Input Elements")]
    public RawImage maskPreviewDisplay;   
    public Slider doseSlider;
    public Slider defocusSlider;
    public Slider naSlider;

    [Header("UI Text Displays")]
    public TMPro.TextMeshProUGUI doseText;
    public TMPro.TextMeshProUGUI defocusText;
    public TMPro.TextMeshProUGUI naText;

    [HideInInspector] public bool isSystemExposing = false;
    [HideInInspector] public int maskDropdownIndexOverride = 0;

    private float[] inputRawArray;
    private float[] nchwInputArray;
    
    private Texture2D maskTexture2D;

    private int resolution = 512;
    private int channels = 4; // Mask, Dose, Defocus, NA

    private float[] outputResultsData;

    void Start()
    {
        if (onnxModelAsset == null)
        {
            Debug.LogError("[Inference Engine] Missing UNet ONNX assignment reference!");
            return;
        }

        runtimeModel = ModelLoader.Load(onnxModelAsset);
        inferenceWorker = new Worker(runtimeModel, BackendType.GPUCompute);

        inputRawArray = new float[1 * resolution * resolution * channels];
        maskTexture2D = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        maskPreviewDisplay.texture = maskTexture2D;
        
        doseSlider.onValueChanged.AddListener(delegate { OnProcessParameterChanged(); });
        defocusSlider.onValueChanged.AddListener(delegate { OnProcessParameterChanged(); });
        naSlider.onValueChanged.AddListener(delegate { OnProcessParameterChanged(); });

        OnMaskSelectionChanged();
    }

    public void BakeSelectedMaskMatrix()
    {
        // Use our new override index variable instead of maskDropdown.value
        int selectedIndex = maskDropdownIndexOverride;
        
        for (int i = 0; i < resolution * resolution; i++) inputRawArray[i * channels + 0] = 0.0f;

        if (selectedIndex == 0) 
        {
            int cd = 24, pitch = 32;
            int startOffset = (resolution - (7 * cd + 6 * pitch)) / 2;
            for (int col = 0; col < 7; col++) BlitVerticalStripe(startOffset + col * (cd + pitch), cd);
        }
        else if (selectedIndex == 1) 
        {
            int cd = 24, pitch = 32;
            int startOffset = (resolution - (7 * cd + 6 * pitch)) / 2;
            for (int row = 0; row < 7; row++) BlitHorizontalStripe(startOffset + row * (cd + pitch), cd);
        }
        else if (selectedIndex == 2) 
        {
            int cd = 24, pitch = 32;
            int startOffset = (resolution - (8 * cd + 7 * pitch)) / 2;
            for (int r = 0; r < 8; r++) {
                for (int c = 0; c < 8; c++) BlitSquareBlock(startOffset + c * (cd + pitch), startOffset + r * (cd + pitch), cd);
            }
        }
        else if (selectedIndex == 3) 
        {
            int cd = 32;
            BlitVerticalStripe((resolution - cd) / 2, cd);
        }
        else if (selectedIndex == 4) 
        {
            int cd = 40;
            BlitSquareBlock((resolution - cd) / 2, (resolution - cd) / 2, cd);
        }

        // Generate the texture matrix display on the card
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

        // Regenerate the NCHW arrays behind the scenes
        OnProcessParameterChanged();
    }

    public void OnMaskSelectionChanged()
    {
        int selectedIndex = maskDropdownIndexOverride;
        for (int i = 0; i < resolution * resolution; i++) inputRawArray[i * channels + 0] = 0.0f;

        if (selectedIndex == 0) 
        {
            int cd = 24, pitch = 32;
            int startOffset = (resolution - (7 * cd + 6 * pitch)) / 2;
            for (int col = 0; col < 7; col++) BlitVerticalStripe(startOffset + col * (cd + pitch), cd);
        }
        else if (selectedIndex == 1) 
        {
            int cd = 24, pitch = 32;
            int startOffset = (resolution - (7 * cd + 6 * pitch)) / 2;
            for (int row = 0; row < 7; row++) BlitHorizontalStripe(startOffset + row * (cd + pitch), cd);
        }
        else if (selectedIndex == 2) 
        {
            int cd = 24, pitch = 32;
            int startOffset = (resolution - (8 * cd + 7 * pitch)) / 2;
            for (int r = 0; r < 8; r++) {
                for (int c = 0; c < 8; c++) BlitSquareBlock(startOffset + c * (cd + pitch), startOffset + r * (cd + pitch), cd);
            }
        }
        else if (selectedIndex == 3) 
        {
            int cd = 32;
            BlitVerticalStripe((resolution - cd) / 2, cd);
        }
        else if (selectedIndex == 4) 
        {
            int cd = 40;
            BlitSquareBlock((resolution - cd) / 2, (resolution - cd) / 2, cd);
        }

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

    // Cache inputs only (Fires dynamically when sliders move)
    public void OnProcessParameterChanged()
    {
        if (doseText != null) doseText.text = $"Dose: {doseSlider.value:F2} mJ";
        if (defocusText != null) defocusText.text = $"Defocus: {defocusSlider.value:F2} um";
        if (naText != null) naText.text = $"Numerical Aperture: {naSlider.value:F2}";
        
        float normDose = doseSlider.value / 38.0f;
        float normDefocus = defocusSlider.value / 45.0f;
        float normNA = naSlider.value / 1.35f;

        nchwInputArray = new float[1 * channels * resolution * resolution];
        int planeSize = resolution * resolution;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int pixelIdx = y * resolution + x;
                float maskVal = inputRawArray[pixelIdx * channels + 0];

                // Pack data into the array buffer, but DO NOT run the inference engine yet
                nchwInputArray[0 * planeSize + pixelIdx] = maskVal;     
                nchwInputArray[1 * planeSize + pixelIdx] = normDose;    
                nchwInputArray[2 * planeSize + pixelIdx] = normDefocus; 
                nchwInputArray[3 * planeSize + pixelIdx] = normNA;      
            }
        }
        
        // --- LIVE INFERENCE RUN TIME REMOVED FROM HERE TO EMBED ENGINE LOCK ---
    }

    // MASTER EXPOSURE PIPELINE (Fires strictly when UV Lamp Ignites)
    public void ExecuteModelExposurePipeline()
    {
        if (inferenceWorker == null || nchwInputArray == null) return;

        Debug.Log("[Litho AI Engine] Execution gate unlocked. Processing raw data arrays into model worker...");

        // Construct tensor from our background cached data matrix
        TensorShape engineInputShape = new TensorShape(1, channels, resolution, resolution);
        using Tensor<float> engineInputTensor = new Tensor<float>(engineInputShape, nchwInputArray);
        
        // Push payload execution directly to the GPU
        inferenceWorker.Schedule(engineInputTensor);
        
        Tensor<float> gpuOutputTensor = inferenceWorker.PeekOutput() as Tensor<float>;
        if (gpuOutputTensor != null)
        {
            using Tensor<float> cpuCopyTensor = gpuOutputTensor.ReadbackAndClone() as Tensor<float>;
            outputResultsData = cpuCopyTensor.DownloadToArray();

            // Paint the screen only when the execution cycle officially finishes!
            if (TryGetComponent<LithoVisualizer>(out var visualizer))
            {
                visualizer.UpdateAerialPlotDisplay();
            }
            
            Debug.Log("[Litho AI Engine] Aerial plot layout updated successfully.");
        }
    }

    // Called instantly by your PhotomaskGalleryUI script buttons!
    public void FinalizeAndBakeSingleMask(int selectedIndex, GameObject physicalMaskPlate)
    {
        maskDropdownIndexOverride = selectedIndex;
        
        // 1. Wipe old matrix channels clean
        for (int i = 0; i < resolution * resolution; i++) inputRawArray[i * channels + 0] = 0.0f;

        // 2. Blit your mathematical design vectors into Channel 0
        if (selectedIndex == 0) {
            int cd = 24, pitch = 32; int startOffset = (resolution - (7 * cd + 6 * pitch)) / 2;
            for (int col = 0; col < 7; col++) BlitVerticalStripe(startOffset + col * (cd + pitch), cd);
        }
        else if (selectedIndex == 1) {
            int cd = 24, pitch = 32; int startOffset = (resolution - (7 * cd + 6 * pitch)) / 2;
            for (int row = 0; row < 7; row++) BlitHorizontalStripe(startOffset + row * (cd + pitch), cd);
        }
        else if (selectedIndex == 2) {
            int cd = 24, pitch = 32; int startOffset = (resolution - (8 * cd + 7 * pitch)) / 2;
            for (int r = 0; r < 8; r++) {
                for (int c = 0; c < 8; c++) BlitSquareBlock(startOffset + c * (cd + pitch), startOffset + r * (cd + pitch), cd);
            }
        }
        else if (selectedIndex == 3) { int cd = 32; BlitVerticalStripe((resolution - cd) / 2, cd); }
        else if (selectedIndex == 4) { int cd = 40; BlitSquareBlock((resolution - cd) / 2, (resolution - cd) / 2, cd); }

        // 3. Render the preview onto the physical plate's mesh sub-material if you have one!
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

        // 4. Stash parameters in NCHW matrix arrays for the subsequent Sentis UV run
        OnProcessParameterChanged();
        
        Debug.Log($"[Litho System] Single functional photomask baked with Profile [{selectedIndex}]. Ready for transport.");
    }

    // Generates a crisp runtime texture thumbnail for the 2x3 grid gallery buttons
    // FIXED: Generates crisp, perfectly scaled runtime texture thumbnails for ALL 2x3 gallery buttons
    public Texture2D GenerateStaticPreviewTexture(int targetIndex)
    {
        int localRes = 128; // Completely isolate coordinate math from global 512 rules
        Texture2D previewTex = new Texture2D(localRes, localRes, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[localRes * localRes];

        for (int y = 0; y < localRes; y++)
        {
            for (int x = 0; x < localRes; x++)
            {
                bool isWhite = false;

                if (targetIndex == 0) // 1. Dense Vert Grating
                {
                    int cd = 6, pitch = 8; // Scaled down exactly 4x (24/4, 32/4)
                    int startOffset = (localRes - (7 * cd + 6 * pitch)) / 2;
                    for (int col = 0; col < 7; col++) {
                        int xStart = startOffset + col * (cd + pitch);
                        if (x >= xStart && x < xStart + cd) isWhite = true;
                    }
                }
                else if (targetIndex == 1) // 2. Dense Horiz Grating
                {
                    int cd = 6, pitch = 8; // Scaled down exactly 4x
                    int startOffset = (localRes - (7 * cd + 6 * pitch)) / 2;
                    for (int row = 0; row < 7; row++) {
                        int yStart = startOffset + row * (cd + pitch);
                        if (y >= yStart && y < yStart + cd) isWhite = true;
                    }
                }
                else if (targetIndex == 2) // 3. Symmetric 2D Matrix
                {
                    int cd = 6, pitch = 8; // Scaled down exactly 4x
                    int startOffset = (localRes - (8 * cd + 7 * pitch)) / 2;
                    for (int r = 0; r < 8; r++) {
                        for (int c = 0; c < 8; c++) {
                            int xStart = startOffset + c * (cd + pitch);
                            int yStart = startOffset + r * (cd + pitch);
                            if (x >= xStart && x < xStart + cd && y >= yStart && y < yStart + cd) isWhite = true;
                        }
                    }
                }
                else if (targetIndex == 3) // 4. Isolated Vertical Line
                { 
                    int cd = 8; // Scaled down 4x (32/4)
                    if (x >= (localRes - cd) / 2 && x < (localRes - cd) / 2 + cd) isWhite = true; 
                }
                else if (targetIndex == 4) // 5. Isolated Contact Square
                { 
                    int cd = 10; // Scaled down 4x (40/4)
                    if (x >= (localRes - cd) / 2 && x < (localRes - cd) / 2 + cd && 
                        y >= (localRes - cd) / 2 && y < (localRes - cd) / 2 + cd) isWhite = true; 
                }

                byte val = (byte)(isWhite ? 255 : 0);
                pixels[y * localRes + x] = new Color32(val, val, val, 255);
            }
        }
        
        previewTex.SetPixels32(pixels);
        previewTex.Apply();
        return previewTex;
    }

    // --- NEW METHOD: PAINTS THE GRAPHIC MATRIX DIRECTLY ONTO THE PHYSICAL MESH ---
    public void PaintBakedPatternOntoObject(GameObject physicalMaskObject)
    {
        if (maskTexture2D == null)
        {
            Debug.LogError("[Litho Engine] Cannot paint photomask: maskTexture2D pixel data buffer is empty!");
            return;
        }

        MeshRenderer meshRenderer = physicalMaskObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = physicalMaskObject.GetComponentInChildren<MeshRenderer>();

        if (meshRenderer != null)
        {
            // Forces an isolated runtime instance copy of your new standard material
            Material targetMaterial = meshRenderer.material;

            if (targetMaterial.HasProperty("_MainTex"))
            {
                targetMaterial.SetTexture("_MainTex", maskTexture2D);
            }
            if (targetMaterial.HasProperty("_BaseMap"))
            {
                targetMaterial.SetTexture("_BaseMap", maskTexture2D);
            }

            Debug.Log($"[Litho Engine] Photomask asset texture channel mapped successfully onto standard material!");
        }
    }

    public float[] GetExposureOutputData()
    {
        return outputResultsData;
    }

    void OnDestroy()
    {
        inferenceWorker?.Dispose();
        Destroy(maskTexture2D);
    }
}