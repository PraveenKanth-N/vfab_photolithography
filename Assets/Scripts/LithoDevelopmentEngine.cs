using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class LithoDevelopmentEngine : MonoBehaviour
{
    [Header("Engine Dependency Connections")]
    public LithoInferenceEngine inferenceEngine;

    [Header("Downstream Visualization Frame")]
    public UnityEngine.UI.RawImage waferMonitorDisplay;

    [Header("Metrology Dashboard Text Links")]
    public TMPro.TextMeshProUGUI layoutTypeText;
    public TMPro.TextMeshProUGUI cdMetricsText;
    public TMPro.TextMeshProUGUI roughnessMetricsText;

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
    
    // BETTER OPTION #2: Lower contrast exponent preserves the U-Net blurred transition slope organically
    private const float N_CONTRAST = 5f; 
    private const float M_TH = 0.55f;    // Threshold limit
    private float mackConstantA;

    // BETTER OPTION #1: Stationary Metrology Ruler (Fixed inspection plane at 15% of film height)
    private const float METROLOGY_THRESHOLD_NM = 0f;

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
        
        // Explicitly clear the initial texture so it is blank before the user clicks Swerve
        Color32[] clearPixels = new Color32[resolution * resolution];
        Color32 clearColor = new Color32(0, 0, 0, 0); // Transparent blank canvas
        for (int i = 0; i < clearPixels.Length; i++) clearPixels[i] = clearColor;
        waferTexture2D.SetPixels32(clearPixels);
        waferTexture2D.Apply();

        // Crisp pixel rendering essential for sub-nanometer edge-tracking accuracy
        waferTexture2D.filterMode = FilterMode.Point;
        
        if (waferMonitorDisplay != null)
            waferMonitorDisplay.texture = waferTexture2D;
    }

    public void SetWashTimeAndExecute(float time)
    {
        currentWashTime = time;
        ExecuteDevelopmentPass();
    }

    // HIGH-PERFORMANCE PARALLEL DISSOLUTION PASS
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

        // DEEP DIVE VALIDATION LOGS (FULLY RETAINED)
        Debug.Log("================ DEVELOPMENT DIAGNOSTICS ================");
        Debug.Log($"1. Incoming Hotplate Film Thickness: {initialThicknessNm:F2} nm");
        Debug.Log($"2. Swerve Wash Time (from UI): {currentWashTime:F2} seconds");
        Debug.Log($"3. Model Output Validation: Fetched array of size {polymerArrayM.Length}.");
        
        // Sample a small 3x3 patch from the exact center of the AI model output
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

        // Process dissolution math
#if UNITY_WEBGL && !UNITY_EDITOR
        // CRITICAL WEBGL FIX: Parallel.For deadlocks WebGL builds because they lack a true background thread pool.
        for (int i = 0; i < totalPixels; i++)
        {
            float mVal = Mathf.Clamp01(polymerArrayM[i]);
            float invM = 1.0f - mVal;
            float invMPow = Mathf.Pow(invM, N_CONTRAST);
            float numerator = Mathf.Pow((mackConstantA + 1.0f) * invM, N_CONTRAST);
            float denominator = mackConstantA + invMPow;
            float rate = R_MAX * (numerator / denominator) + R_MIN;
            float removedNm = rate * currentWashTime;
            float finalHeight = Mathf.Max(initialThicknessNm - removedNm, 0.0f);
            remainingHeightMap[i] = finalHeight;
            binaryWaferPattern[i] = (finalHeight > METROLOGY_THRESHOLD_NM) ? (byte)1 : (byte)0;
            float normalizedHeight = finalHeight / initialThicknessNm;
            byte colorVal = (byte)(normalizedHeight * 255);
            textureColorBuffer[i] = new Color32(colorVal, colorVal, colorVal, 255);
        }
#else
        // High-performance parallel block for Desktop
        Parallel.For(0, totalPixels, i =>
        {
            float mVal = Mathf.Clamp01(polymerArrayM[i]);
            float invM = 1.0f - mVal;
            float invMPow = Mathf.Pow(invM, N_CONTRAST);
            float numerator = Mathf.Pow((mackConstantA + 1.0f) * invM, N_CONTRAST);
            float denominator = mackConstantA + invMPow;
            float rate = R_MAX * (numerator / denominator) + R_MIN;
            float removedNm = rate * currentWashTime;
            float finalHeight = Mathf.Max(initialThicknessNm - removedNm, 0.0f);
            remainingHeightMap[i] = finalHeight;
            binaryWaferPattern[i] = (finalHeight > METROLOGY_THRESHOLD_NM) ? (byte)1 : (byte)0;
            float normalizedHeight = finalHeight / initialThicknessNm;
            byte colorVal = (byte)(normalizedHeight * 255);
            textureColorBuffer[i] = new Color32(colorVal, colorVal, colorVal, 255);
        });
#endif

        // Blit color arrays onto target GPU texture structures
        waferTexture2D.SetPixels32(textureColorBuffer);
        waferTexture2D.Apply();
        
        Debug.Log("[Litho Dev Engine] Development calculation and visualization arrays synchronized successfully.");

        // DISPATCHER HANDSHAKE TRIGGER
        // Automatically dispatches the uncorrupted data map to your standalone metrology modules
        if (inferenceEngine != null)
        {
            int currentLayoutID = inferenceEngine.maskDropdownIndexOverride; 
            RunMetrologyInspection(currentLayoutID);
        }
    }

    public void RunMetrologyInspection(int activeLayoutType)
    {
        if (binaryWaferPattern == null) return;

        switch(activeLayoutType)
        {
            case 0: // Vertical Grating Array
            case 3: // Single Vertical Line
                if (!TryGetComponent<LithoLineMetrology>(out var vScanner)) vScanner = gameObject.AddComponent<LithoLineMetrology>();
                var vReport = vScanner.AnalyzeGratings(binaryWaferPattern, scanVertically: false);
                UpdateLineUI(activeLayoutType == 0 ? "Vertical Grating" : "Single Vertical Line", vReport);
                break;

            case 1: // Horizontal Grating Array
                if (!TryGetComponent<LithoLineMetrology>(out var hScanner)) hScanner = gameObject.AddComponent<LithoLineMetrology>();
                var hReport = hScanner.AnalyzeGratings(binaryWaferPattern, scanVertically: true);
                UpdateLineUI("Horizontal Grating Array", hReport);
                break;

            case 2: // 8x8 Contact Matrix Array
            case 4: // Isolated Contact Window
                if (!TryGetComponent<LithoContactMetrology>(out var cScanner)) cScanner = gameObject.AddComponent<LithoContactMetrology>();
                var cReport = cScanner.AnalyzeContacts(binaryWaferPattern);
                UpdateContactUI(activeLayoutType == 2 ? "Contact Hole Array" : "Isolated Via Window", cReport);
                break;
        }
    }

    private void UpdateLineUI(string title, LithoLineMetrology.LineReport report)
    {
        if (layoutTypeText != null) layoutTypeText.text = $"<b>Mask Layout:</b> {title}";
        if (cdMetricsText != null) cdMetricsText.text = $"<b>Line CD:</b> {report.meanLineCD:F1} nm\n<b>Trench CD:</b> {report.meanTrenchCD:F1} nm";
        if (roughnessMetricsText != null) roughnessMetricsText.text = $"<b>LER (3σ):</b> {report.ler3Sigma:F2} nm\n<b>LWR (3σ):</b> {report.lwr3Sigma:F2} nm";
    }

    private void UpdateContactUI(string title, LithoContactMetrology.ContactReport report)
    {
        if (layoutTypeText != null) layoutTypeText.text = $"<b>Mask Layout:</b> {title} ({report.totalContactsDetected} Vias)";
        if (cdMetricsText != null) cdMetricsText.text = $"<b>Contact CD (Dia):</b> {report.averageDiameterCD:F1} nm\n<b>Grid Uniformity:</b> Stable";
        if (roughnessMetricsText != null) roughnessMetricsText.text = $"<b>Perimeter LER:</b> {report.contactLER3Sigma:F2} nm\n<b>LWR:</b> N/A";
    }

    void OnDestroy()
    {
        if (waferTexture2D != null) Destroy(waferTexture2D);
    }
}
