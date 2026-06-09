using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.InferenceEngine; // Unified project wrapper mapping alignment
using TMPro;

public class LithoInferenceEngine : MonoBehaviour
{
    [Header("Model Assets")]
    public ModelAsset onnxModelAsset;
    private Model runtimeModel;
    private Worker inferenceWorker; // Utilizing the modern concrete Worker class

    [Header("UI Input Elements")]
    public TMP_Dropdown maskDropdown;         
    public RawImage maskPreviewDisplay;   
    public Slider doseSlider;
    public Slider defocusSlider;
    public Slider naSlider;

    [HideInInspector] public bool isSystemExposing = false; 

    // Persistent baseline array memory buffer and runtime texture configuration
    private float[] inputRawArray;
    private Texture2D maskTexture2D;

    private int resolution = 512;
    private int channels = 6;
    private float currentThicknessNorm = 1.0f; 
    
    private const float STANDARD_DILL_B_NORM = 0.525f / 0.65f; 

    // Continuous calculation output cache array
    private float[] outputResultsData;

    void Start()
    {
        if (onnxModelAsset == null)
        {
            Debug.LogError("[Inference Engine] ModelAsset missing from slot registry!");
            return;
        }

        // 1. Unified Model Loader Pipeline
        runtimeModel = ModelLoader.Load(onnxModelAsset);
        inferenceWorker = new Worker(runtimeModel, BackendType.GPUCompute);

        // 2. Allocate memory blocks matching our 512x512 matrix resolution
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
            "Isolated Vertical Line",
            "Semi-Dense Vertical Grating",
            "Symmetric 2D Contacts Array",
            "Isolated Contact Square"
        };
        maskDropdown.AddOptions(options);
    }

    public void OnMaskSelectionChanged()
    {
        int selectedIndex = maskDropdown.value;
        for (int i = 0; i < resolution * resolution; i++) inputRawArray[i * channels + 0] = 0.0f;

        Color32[] textureColorBuffer = new Color32[resolution * resolution];

        if (selectedIndex == 0) 
        {
            int cd = 24, pitch = 32;
            int startOffset = (resolution - (7 * cd + 6 * pitch)) / 2;
            for (int col = 0; col < 7; col++) BlitVerticalStripe(startOffset + col * (cd + pitch), cd);
        }
        else if (selectedIndex == 1) 
        {
            int cd = 32;
            BlitVerticalStripe((resolution - cd) / 2, cd);
        }
        else if (selectedIndex == 2) 
        {
            int cd = 24, pitch = 80;
            int startOffset = (resolution - (4 * cd + 3 * pitch)) / 2;
            for (int col = 0; col < 4; col++) BlitVerticalStripe(startOffset + col * (cd + pitch), cd);
        }
        else if (selectedIndex == 3) 
        {
            int cd = 24, pitch = 32;
            int startOffset = (resolution - (8 * cd + 7 * pitch)) / 2;
            for (int r = 0; r < 8; r++) {
                for (int c = 0; c < 8; c++) BlitSquareBlock(startOffset + c * (cd + pitch), startOffset + r * (cd + pitch), cd);
            }
        }
        else if (selectedIndex == 4) 
        {
            int cd = 40;
            BlitSquareBlock((resolution - cd) / 2, (resolution - cd) / 2, cd);
        }

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
        if (!isSystemExposing) return;

        float normDose = doseSlider.value / 38.0f;
        float normDefocus = defocusSlider.value / 45.0f;
        float normNA = naSlider.value / 1.35f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int flatIdx = (y * resolution + x) * channels;
                inputRawArray[flatIdx + 1] = normDose;
                inputRawArray[flatIdx + 2] = normDefocus;
                inputRawArray[flatIdx + 3] = currentThicknessNorm; 
                inputRawArray[flatIdx + 4] = normNA;
                inputRawArray[flatIdx + 5] = STANDARD_DILL_B_NORM;
            }
        }

        // =====================================================================
        // UNIFIED ASYNCHRONOUS INFERENCE PASS EXECUTION
        // =====================================================================
        TensorShape shape = new TensorShape(1, resolution, resolution, channels);
        using Tensor<float> inputTensor = new Tensor<float>(shape, inputRawArray);
        
        // Asynchronously schedule execution on the GPU compute pipeline
        inferenceWorker.Schedule(inputTensor);
        
        Tensor<float> gpuOutputTensor = inferenceWorker.PeekOutput() as Tensor<float>;
        if (gpuOutputTensor != null)
        {
            // Extract and download local memory references cleanly using your asset framework patterns
            using Tensor<float> cpuCopyTensor = gpuOutputTensor.ReadbackAndClone() as Tensor<float>;
            outputResultsData = cpuCopyTensor.DownloadToArray();
        }
        // =====================================================================
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