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
    private Worker inferenceWorker; // Fixed to use concrete Worker class matching SpinCoater

    [Header("UI Input Elements")]
    public TMP_Dropdown maskDropdown;         
    public RawImage maskPreviewDisplay;   
    public Slider doseSlider;
    public Slider defocusSlider;
    public Slider naSlider;

    [HideInInspector] public bool isSystemExposing = false;

    private float[] inputRawArray;
    
    // Persistent wrapper data blocks matching custom project API
    private Texture2D maskTexture2D;

    private int resolution = 512;
    private int channels = 4; // Updated: 4 Channels (Mask, Dose, Defocus, NA)

    // Cached outputs for consumption by ExposureStationController
    private float[] outputResultsData;

    void Start()
    {
        if (onnxModelAsset == null)
        {
            Debug.LogError("[Inference Engine] Missing UNet ONNX assignment reference!");
            return;
        }

        // Initialize matching your exact working SpinCoater structural format
        runtimeModel = ModelLoader.Load(onnxModelAsset);
        inferenceWorker = new Worker(runtimeModel, BackendType.GPUCompute);

        inputRawArray = new float[1 * resolution * resolution * channels];
        maskTexture2D = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        maskPreviewDisplay.texture = maskTexture2D;

        PopulateMaskDropdownMenu();

        maskDropdown.onValueChanged.AddListener(delegate { OnMaskSelectionChanged(); });
        doseSlider.onValueChanged.AddListener(delegate { OnProcessParameterChanged(); });
        defocusSlider.onValueChanged.AddListener(delegate { OnProcessParameterChanged(); });
        naSlider.onValueChanged.AddListener(delegate { OnProcessParameterChanged(); });

        OnMaskSelectionChanged();
    }

    private void PopulateMaskDropdownMenu()
    {
        maskDropdown.ClearOptions();
        List<string> options = new List<string>
        {
            "Dense Vertical Grating",
            "Dense Horizontal Grating", 
            "Symmetric 2D Contacts Array",
            "Isolated Vertical Line",
            "Isolated Contact Square"
        };
        maskDropdown.AddOptions(options);
        maskDropdown.RefreshShownValue();
    }

    public void OnMaskSelectionChanged()
    {
        int selectedIndex = maskDropdown.value;
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

    public void OnProcessParameterChanged()
    {
        float normDose = doseSlider.value / 38.0f;
        float normDefocus = defocusSlider.value / 45.0f;
        float normNA = naSlider.value / 1.35f;

        // 1. ALLOCATE NEW TEMP BUFFER FOR NCHW LAYOUT: [1, 4, 512, 512]
        // Instead of grouping pixels side-by-side, we stack entire 512x512 grids back-to-back.
        float[] nchwInputArray = new float[1 * channels * resolution * resolution];
        int planeSize = resolution * resolution;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int pixelIdx = y * resolution + x;

                // Grab the mask structural binary bit we blitted earlier in channel 0
                float maskVal = inputRawArray[pixelIdx * channels + 0];

                // Map across distinct spatial planes:
                nchwInputArray[0 * planeSize + pixelIdx] = maskVal;     // Channel 0: Mask
                nchwInputArray[1 * planeSize + pixelIdx] = normDose;    // Channel 1: Dose
                nchwInputArray[2 * planeSize + pixelIdx] = normDefocus; // Channel 2: Defocus
                nchwInputArray[3 * planeSize + pixelIdx] = normNA;      // Channel 3: NA
            }
        }

        // 2. CONSTRUCT TENSOR MATCHING MODEL EXPECTED EXPECTED SHAPE: (1, 4, 512, 512)
        TensorShape engineInputShape = new TensorShape(1, channels, resolution, resolution);
        using Tensor<float> engineInputTensor = new Tensor<float>(engineInputShape, nchwInputArray);
        
        // 3. Schedule the fresh layout onto the GPU pipeline
        inferenceWorker.Schedule(engineInputTensor);
        
        Tensor<float> gpuOutputTensor = inferenceWorker.PeekOutput() as Tensor<float>;
        if (gpuOutputTensor != null)
        {
            using Tensor<float> cpuCopyTensor = gpuOutputTensor.ReadbackAndClone() as Tensor<float>;
            outputResultsData = cpuCopyTensor.DownloadToArray();

            // Fire your new physics continuous sine-wave visualizer
            if (TryGetComponent<LithoVisualizer>(out var visualizer))
            {
                visualizer.UpdateAerialPlotDisplay();
            }
        }
    }

    // Clean data hook for external controllers
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