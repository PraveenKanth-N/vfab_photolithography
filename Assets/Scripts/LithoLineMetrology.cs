using UnityEngine;
using System.Collections.Generic;

public class LithoLineMetrology : MonoBehaviour
{
    private int resolution = 512;
    private float dx_nm = 3.9f; // Core physical scaling factor

    public struct LineReport
    {
        public float meanLineCD;
        public float meanTrenchCD;
        public float ler3Sigma;
        public float lwr3Sigma;
    }

    /// <summary>
    /// Scans across line matrices orthogonally to capture continuous CD and Roughness shifts.
    /// </summary>
    public LineReport AnalyzeGratings(byte[] binaryWafer, bool scanVertically)
    {
        LineReport report = new LineReport();
        List<float> lineWidths = new List<float>();
        List<float> trenchWidths = new List<float>();
        List<List<float>> allEdgeProfiles = new List<List<float>>();

        int primaryAxisLimit = resolution;
        int orthogonalAxisLimit = resolution;

        // Loop over the wafer array row-by-row or column-by-column
        for (int i = 0; i < primaryAxisLimit; i++)
        {
            List<int> transitions = new List<int>();
            byte currentState = scanVertically ? binaryWafer[0 + i] : binaryWafer[i * resolution + 0];

            for (int j = 1; j < orthogonalAxisLimit; j++)
            {
                int idx = scanVertically ? (j * resolution + i) : (i * resolution + j);
                byte pixelState = binaryWafer[idx];

                if (pixelState != currentState)
                {
                    transitions.Add(j);
                    currentState = pixelState;
                }
            }

            // If the slice hits a feature, parse width vectors and edge arrays
            if (transitions.Count >= 2)
            {
                while (allEdgeProfiles.Count < transitions.Count)
                {
                    allEdgeProfiles.Add(new List<float>());
                }

                for (int t = 0; t < transitions.Count; t++)
                {
                    allEdgeProfiles[t].Add(transitions[t]);
                }

                for (int t = 0; t < transitions.Count - 1; t++)
                {
                    float width = (transitions[t + 1] - transitions[t]) * dx_nm;
                    int mid = (transitions[t] + transitions[t + 1]) / 2;
                    int midIdx = scanVertically ? (mid * resolution + i) : (i * resolution + mid);

                    if (binaryWafer[midIdx] == 1) lineWidths.Add(width);
                    else trenchWidths.Add(width);
                }
            }
        }

        // Compute statistical variables from your true spatial dataset arrays
        report.meanLineCD = CalculateMean(lineWidths);
        report.meanTrenchCD = CalculateMean(trenchWidths);

        List<float> edgeVariances = new List<float>();
        foreach (var profile in allEdgeProfiles)
        {
            if (profile.Count < 2) continue;
            float profileMean = CalculateMean(profile);
            foreach (float edgePos in profile)
            {
                edgeVariances.Add(Mathf.Pow(edgePos - profileMean, 2));
            }
        }

        float globalStandardDeviation = Mathf.Sqrt(CalculateMean(edgeVariances));
        report.ler3Sigma = globalStandardDeviation * 3.0f * dx_nm;
        report.lwr3Sigma = CalculateStandardDeviation(lineWidths) * 3.0f;

        return report;
    }

    private float CalculateMean(List<float> list)
    {
        if (list.Count == 0) return 0.0f;
        float sum = 0f;
        for (int i = 0; i < list.Count; i++) sum += list[i];
        return sum / list.Count;
    }

    private float CalculateStandardDeviation(List<float> list)
    {
        if (list.Count < 2) return 0.0f;
        float avg = CalculateMean(list);
        float sumSq = 0f;
        for (int i = 0; i < list.Count; i++) sumSq += Mathf.Pow(list[i] - avg, 2);
        return Mathf.Sqrt(sumSq / (list.Count - 1));
    }
}
