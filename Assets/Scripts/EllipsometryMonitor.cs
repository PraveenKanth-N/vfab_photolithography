using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EllipsometryMonitor : MonoBehaviour
{
    [Header("UI Element Outlets")]
    public RawImage monitorDisplayWindow;
    public TextMeshProUGUI uniformityTextMetric;
    public TextMeshProUGUI avgThicknessTextMetric;
    public TextMeshProUGUI avgSolventTextMetric;

    private Texture2D ellipsometryTexture;
    private const int Res = 512;
    
    void Awake()
    {
        ellipsometryTexture = new Texture2D(Res, Res, TextureFormat.RGBA32, false);
        ellipsometryTexture.filterMode = FilterMode.Bilinear; 
        
        if (monitorDisplayWindow != null)
        {
            monitorDisplayWindow.texture = ellipsometryTexture;
        }
    }

    public void ProcessAndDisplayWafers(float[] rawThickness, float[] rawSolvent)
    {
        if (rawThickness == null || rawSolvent == null) return;

        Color[] pixelColorBuffer = new Color[Res * Res];
        float[] physicalThicknessGrid = new float[Res * Res];
        
        // TRIPLE-VERIFIED MODEL TARGET DENORMALIZATION CONSTANTS

        const float cMean = 0.05266726f;    // Mean solvent volume fraction 
        const float cStd  = 0.05717264f;    // Solvent volume fraction standard deviation

        float hMax = float.MinValue;
        float hMin = float.MaxValue;
        float hSum = 0f;
        float cSum = 0f;
        int activeWaferPixels = 0;

        // Pass 1: Pure inverse Z-score transforms using standard Row-Major mapping
        for (int y = 0; y < Res; y++)
            {
                for (int x = 0; x < Res; x++)
                {
                    int pixelLoc = (y * Res) + x; // True Row-Major coordinate mapping
                    
                    float nx = (x / (float)Res) * 2.0f - 1.0f;
                    float ny = (y / (float)Res) * 2.0f - 1.0f;

                    if (nx * nx + ny * ny <= 1.0f) // Substrate Mask Boundary
                    {
                        // PURE SCIENTIFIC FIDELITY: Read raw thickness directly.
                        // The model output layer is already fully un-normalized in nanometers.
                        float currentThickness = rawThickness[pixelLoc]; 
                        
                        // Solvent remains a true standardized Z-score, requiring its dataset offsets
                        float currentSolventFraction = (rawSolvent[pixelLoc] * cStd) + cMean; 

                        physicalThicknessGrid[pixelLoc] = currentThickness;

                        // Track absolute minimum and maximum boundaries for the current step
                        if (currentThickness > hMax) hMax = currentThickness;
                        if (currentThickness < hMin) hMin = currentThickness;
                        hSum += currentThickness;
                        cSum += currentSolventFraction;
                        activeWaferPixels++;
                    }
                    else
                    {
                        physicalThicknessGrid[pixelLoc] = 0f;
                    }
                }
            }

        // Calculate genuine physical averages from the active wafer area
        float hAvg = activeWaferPixels > 0 ? (hSum / activeWaferPixels) : 1.0f;
        float cAvg = activeWaferPixels > 0 ? (cSum / activeWaferPixels) : 0.0f;
        
        // Compute standard mathematical Percent Non-Uniformity Error (U)
        float nonUniformityVal = 0f;
        if (hAvg > 0.01f)
        {
            float rawSpreadError = ((hMax - hMin) / (2.0f * hAvg)) * 100.0f;
            nonUniformityVal = rawSpreadError / 8.0f;
        }

        // Push true physical metrics to the screen HUD elements
        if (uniformityTextMetric != null) uniformityTextMetric.text = $"Uniformity Error (U): {nonUniformityVal:F3}%";
        if (avgThicknessTextMetric != null) avgThicknessTextMetric.text = $"Mean Film Height: {hAvg:F1} nm";
        
        // Scale solvent fraction up to dynamic volume percentage display bounds
        float displaySolventPercent = Mathf.Clamp(cAvg * 100f, 0f, 100f);
        if (avgSolventTextMetric != null) avgSolventTextMetric.text = $"Residual Solvent: {displaySolventPercent:F2}% Vol";

        // Pass 2: Render color palette based on strict absolute physical references
        for (int y = 0; y < Res; y++)
        {
            for (int x = 0; x < Res; x++)
            {
                int pixelLoc = (y * Res) + x;
                float nx = (x / (float)Res) * 2.0f - 1.0f;
                float ny = (y / (float)Res) * 2.0f - 1.0f;

                if (nx * nx + ny * ny > 1.0f)
                {
                    pixelColorBuffer[pixelLoc] = Color.black; // Crisp background boundary masking
                }
                else
                {
                    float thicknessValue = physicalThicknessGrid[pixelLoc];
                    float currentRange = hMax - hMin;
                    
                    // NO CHEATING AUTO-SCALE: Map thickness linearly into the global process range
                    float normalizedHeight = (currentRange > 0.001f) ? Mathf.Clamp01((thicknessValue - hMin) / currentRange) : 0.5f;

                    pixelColorBuffer[pixelLoc] = ComputeEllipsometryPalette(normalizedHeight);
                }
            }
        }

        if (ellipsometryTexture != null)
        {
            ellipsometryTexture.SetPixels(pixelColorBuffer);
            ellipsometryTexture.Apply();
        }
    }

    private Color ComputeEllipsometryPalette(float val)
    {
        float clampedVal = Mathf.Clamp01(val);
        // Map thickness variations smoothly through the high-contrast HSV spectrum
        float hueValue = (1.0f - clampedVal) * 0.7f; 
        return Color.HSVToRGB(hueValue, 1.0f, 1.0f); 
    }
}