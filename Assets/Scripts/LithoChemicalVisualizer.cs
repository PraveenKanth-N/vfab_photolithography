using UnityEngine;
using UnityEngine.UI;

public class LithoChemicalVisualizer : MonoBehaviour
{
    [Header("Engine Reference Connection")]
    public LithoInferenceEngine inferenceEngine;

    [Header("UI Render Frame Target")]
    public RawImage deprotectionPreviewDisplay; 

    private Texture2D chemicalTexture2D;
    private int resolution = 512;

    void Start()
    {
        chemicalTexture2D = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        if (deprotectionPreviewDisplay != null) deprotectionPreviewDisplay.texture = chemicalTexture2D;
    }

    public void UpdateDeprotectionPlotDisplay()
    {
        if (inferenceEngine == null || inferenceEngine.chemicalResultsData == null) return;
        
        float[] polymerArray = inferenceEngine.chemicalResultsData;
        Color32[] colorBuffer = new Color32[resolution * resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int unityIdx = (resolution - 1 - y) * resolution + x;
                int tensorIdx = (y * resolution + x);

                float polymerM = Mathf.Clamp01(polymerArray[tensorIdx]);
                colorBuffer[unityIdx] = GetPlasmaColorTrack(polymerM);
            }
        }

        chemicalTexture2D.SetPixels32(colorBuffer);
        chemicalTexture2D.Apply();
    }

    private Color32 GetPlasmaColorTrack(float m)
    {
        if (m < 0.33f)
            return Color32.Lerp(new Color32(13, 8, 135, 255), new Color32(126, 3, 168, 255), m / 0.33f);
        else if (m < 0.66f)
            return Color32.Lerp(new Color32(126, 3, 168, 255), new Color32(204, 71, 120, 255), (m - 0.33f) / 0.33f);
        else
            return Color32.Lerp(new Color32(204, 71, 120, 255), new Color32(240, 249, 33, 255), (m - 0.66f) / 0.34f);
    }

    void OnDestroy()
    {
        if (chemicalTexture2D != null) Destroy(chemicalTexture2D);
    }
}