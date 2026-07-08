using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LithoVisualizer : MonoBehaviour
{
    [Header("Engine Reference")]
    public LithoInferenceEngine inferenceEngine;

    [Header("UI Display Elements")]
    public RawImage aerialPreviewDisplay;

    private Texture2D aerialTexture2D;
    private int resolution = 512;

    // Inferno colour ramp milestones - same as before
    private static readonly Color32 ColorBlack  = new Color32(3,   2,   12,  255);
    private static readonly Color32 ColorPurple = new Color32(85,  15,  115, 255);
    private static readonly Color32 ColorPink   = new Color32(165, 30,  135, 255);
    private static readonly Color32 ColorOrange = new Color32(245, 90,  30,  255);
    private static readonly Color32 ColorYellow = new Color32(255, 215, 40,  255);
    private static readonly Color32 ColorWhite  = new Color32(255, 255, 255, 255);

    void Start()
    {
        aerialTexture2D = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        aerialTexture2D.filterMode = FilterMode.Bilinear;
        aerialTexture2D.wrapMode   = TextureWrapMode.Clamp;
        aerialPreviewDisplay.texture = aerialTexture2D;

        if (inferenceEngine == null)
            inferenceEngine = GetComponent<LithoInferenceEngine>();
    }

    public void UpdateAerialPlotDisplay()
    {
        if (inferenceEngine == null) return;

        float[] outputArray = inferenceEngine.GetExposureOutputData();
        if (outputArray == null || outputArray.Length == 0) return;

        // 1. Find the true data range so we normalise against real optical values,
        //       not a hardcoded 0-1 assumption. Your data runs ~-0.01  ~0.81.
        float dataMin = float.MaxValue;
        float dataMax = float.MinValue;
        for (int i = 0; i < outputArray.Length; i++)
        {
            if (outputArray[i] < dataMin) dataMin = outputArray[i];
            if (outputArray[i] > dataMax) dataMax = outputArray[i];
        }
        float dataRange = dataMax - dataMin;
        if (dataRange < 1e-6f) dataRange = 1f; // guard against flat output

        Color32[] colorBuffer = new Color32[resolution * resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                // Unity's RawImage has Y=0 at the bottom; tensor row 0 is the top.
                int unityIdx  = (resolution - 1 - y) * resolution + x;
                int tensorIdx = y * resolution + x;

                // 2. Normalise the raw ML float directly - no fake sine wave.
                float t = (outputArray[tensorIdx] - dataMin) / dataRange;
                t = Mathf.Clamp01(t);

                // 3. Drive the same inferno ramp with the REAL gradient value.
                colorBuffer[unityIdx] = InfernoRamp(t);
            }
        }

        aerialTexture2D.SetPixels32(colorBuffer);
        aerialTexture2D.Apply();
    }

    private Color32 InfernoRamp(float t)
    {
        if (t < 0.15f)
            return Color32.Lerp(ColorBlack,  ColorPurple, t / 0.15f);
        else if (t < 0.35f)
            return Color32.Lerp(ColorPurple, ColorPink,   (t - 0.15f) / 0.20f);
        else if (t < 0.55f)
            return Color32.Lerp(ColorPink,   ColorOrange, (t - 0.35f) / 0.20f);
        else if (t < 0.78f)
            return Color32.Lerp(ColorOrange, ColorYellow, (t - 0.55f) / 0.23f);
        else if (t < 0.92f)
            return Color32.Lerp(ColorYellow, ColorWhite,  (t - 0.78f) / 0.14f);
        else
            return Color32.Lerp(ColorWhite,  ColorWhite,  (t - 0.92f) / 0.08f);
    }

    void OnDestroy()
    {
        if (aerialTexture2D != null) Destroy(aerialTexture2D);
    }
}