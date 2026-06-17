using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class LithoDevelopmentEngine : MonoBehaviour
{
    [Header("Engine Dependency Connections")]
    public LithoInferenceEngine inferenceEngine;

    [Header("Downstream Visualization Frame")]
    public UnityEngine.UI.RawImage waferMonitorDisplay;

    // Upstream state trackers (Populated from prior processing modules)
    [HideInInspector] public float initialThicknessNm = 300.0f; // Soft-bake tracking target
    [HideInInspector] public float currentWashTime = 30.0f;
    
    // Output structural arrays exposed to your metrology panels
    [HideInInspector] public byte[] binaryWaferPattern; 
    [HideInInspector] public float[] remainingHeightMap;

    private Texture2D waferTexture2D;
    private int resolution = 512;

    // Fixed Mack Dissolution Kinetic Constants for 193nm CAR
    private const float R_MAX = 120.0f;  // Max etch speed (nm/s)
    private const float R_MIN = 0.05f;   // Leakage rate (nm/s)
    private const float N_CONTRAST = 6.5f; 
    private const float M_TH = 0.55f;    // Threshold limit
    private float mackConstantA;

    void Awake()
    {
        // Pre-calculate Mack parameter constant 'a' to save operational cycles
        mackConstantA = ((N_CONTRAST + 1.0f) / (N_CONTRAST - 1.0f)) * Mathf.Pow(1.0f - M_TH, N_CONTRAST);
    }

    void Start()
    {
        binaryWaferPattern = new byte[resolution * resolution];
        remainingHeightMap = new float[resolution * resolution];

        waferTexture2D = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        
        if (waferMonitorDisplay != null)
            waferMonitorDisplay.texture = waferTexture2D;
    }

    public void SetWashTimeAndExecute(float time)
    {
        currentWashTime = time;
        ExecuteDevelopmentPass();
    }

    // =====================================================================
    // HIGH-PERFORMANCE PARALLEL DISSOLUTION PASS
    // =====================================================================
    public void ExecuteDevelopmentPass()
    {
        if (inferenceEngine == null || inferenceEngine.chemicalResultsData == null) 
        {
            Debug.LogError("[Litho Dev Engine] Aborting: AI Model Chemical Results are missing! Was exposure executed?");
            return;
        }
        if (waferTexture2D == null) return;

        float[] polymerArrayM = inferenceEngine.chemicalResultsData;
        int totalPixels = resolution * resolution;

        if (polymerArrayM.Length != totalPixels) return;

        // --- DEEP DIVE VALIDATION LOGS ---
        Debug.Log("================ DEVELOPMENT DIAGNOSTICS ================");
        Debug.Log($"1. Incoming Hotplate Film Thickness: {initialThicknessNm:F2} nm");
        Debug.Log($"2. Swerve Wash Time (from UI): {currentWashTime:F2} seconds");
        Debug.Log($"3. Model Output Validation: Fetched array of size {polymerArrayM.Length}.");
        
        // Let's sample a small 3x3 patch from the exact center of the AI model output
        int center = resolution / 2;
        string sampleRaw = "";
        for (int y = -1; y <= 1; y++) {
            for (int x = -1; x <= 1; x++) {
                sampleRaw += polymerArrayM[(center+y)*resolution + (center+x)].ToString("F3") + " | ";
            }
            sampleRaw += "\n";
        }
        Debug.Log($"4. Center 3x3 Raw Inference Patch (0 to 1 chemical bonds):\n{sampleRaw}");
        Debug.Log("=========================================================");

        Color32[] textureColorBuffer = new Color32[totalPixels];

        // Process a high-speed multi-threaded parallel block across all CPU cores
        Parallel.For(0, totalPixels, i =>
        {
            float mVal = Mathf.Clamp01(polymerArrayM[i]);

            // Compute Mack dissolution velocity equations
            float invM = 1.0f - mVal;
            float invMPow = Mathf.Pow(invM, N_CONTRAST);
            
            float numerator = Mathf.Pow((mackConstantA + 1.0f) * invM, N_CONTRAST);
            float denominator = mackConstantA + invMPow;
            
            float rate = R_MAX * (numerator / denominator) + R_MIN;

            // Fluid erosion calculation
            float removedNm = rate * currentWashTime;
            float finalHeight = Mathf.Max(initialThicknessNm - removedNm, 0.0f);
            remainingHeightMap[i] = finalHeight;

            // Binary Wafer Masking: 0 = Cleared Silicon (Black), 1 = Intact Resist (White)
            byte binaryState = (finalHeight > 0.0f) ? (byte)1 : (byte)0;
            binaryWaferPattern[i] = binaryState;

            // Map binary matrix array states to top-down preview colors
            byte colorVal = (byte)(binaryState * 255);
            textureColorBuffer[i] = new Color32(colorVal, colorVal, colorVal, 255);
        });

        // Blit color arrays onto target GPU texture textures
        waferTexture2D.SetPixels32(textureColorBuffer);
        waferTexture2D.Apply();
        
        Debug.Log("[Litho Dev Engine] Development calculation and visualization arrays synchronized successfully.");
    }

    void OnDestroy()
    {
        if (waferTexture2D != null) Destroy(waferTexture2D);
    }
}
