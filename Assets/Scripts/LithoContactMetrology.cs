using UnityEngine;
using System.Collections.Generic;

public class LithoContactMetrology : MonoBehaviour
{
    private int resolution = 512;
    private float dx_nm = 3.9f;

    public struct ContactReport
    {
        public int totalContactsDetected;
        public float averageDiameterCD;
        public float contactLER3Sigma;
    }

    /// <summary>
    /// Segments individual contact holes using a 2D flood-fill to capture true areas and perimeters.
    /// </summary>
    public ContactReport AnalyzeContacts(byte[] binaryWafer)
    {
        ContactReport report = new ContactReport();
        bool[] visited = new bool[resolution * resolution];
        List<float> distinctContactDiameters = new List<float>();
        List<float> accumulatedPerimeterRoughness = new List<float>();

        // Scan the active interior field of the wafer canvas
        for (int y = 16; y < resolution - 16; y++)
        {
            for (int x = 16; x < resolution - 16; x++)
            {
                int idx = y * resolution + x;
                
                // If an unvisited open space (0) is located, a component is targeted
                if (binaryWafer[idx] == 0 && !visited[idx])
                {
                    List<int> componentPixels = new List<int>();
                    List<Vector2> perimeterCoordinates = new List<Vector2>();
                    
                    ExecuteConnectedComponentFill(binaryWafer, x, y, visited, componentPixels, perimeterCoordinates);

                    if (componentPixels.Count > 10) // Filter out noise fragments
                    {
                        float physicalAreaNm2 = componentPixels.Count * (dx_nm * dx_nm);
                        
                        // Circle Equivalent Area CD formula: d = 2 * sqrt(Area / pi)
                        float diameterCD = 2.0f * Mathf.Sqrt(physicalAreaNm2 / Mathf.PI);
                        distinctContactDiameters.Add(diameterCD);

                        if (perimeterCoordinates.Count > 4)
                        {
                            Vector2 centroid = CalculateCentroid(perimeterCoordinates);
                            List<float> radii = new List<float>();
                            foreach (var edgePoint in perimeterCoordinates)
                            {
                                radii.Add(Vector2.Distance(edgePoint, centroid) * dx_nm);
                            }
                            accumulatedPerimeterRoughness.Add(Calculate3Sigma(radii));
                        }
                    }
                }
            }
        }

        report.totalContactsDetected = distinctContactDiameters.Count;
        report.averageDiameterCD = CalculateMean(distinctContactDiameters);
        report.contactLER3Sigma = CalculateMean(accumulatedPerimeterRoughness);

        return report;
    }

    private void ExecuteConnectedComponentFill(byte[] wafer, int startX, int startY, bool[] visited, List<int> component, List<Vector2> perimeter)
    {
        Queue<int> queue = new Queue<int>();
        int startIdx = startY * resolution + startX;
        queue.Enqueue(startIdx);
        visited[startIdx] = true;

        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };

        while (queue.Count > 0)
        {
            int currIdx = queue.Dequeue();
            component.Add(currIdx);

            int cx = currIdx % resolution;
            int cy = currIdx / resolution;
            bool isPerimeter = false;

            for (int i = 0; i < 4; i++)
            {
                int nx = cx + dx[i];
                int ny = cy + dy[i];
                int nIdx = ny * resolution + nx;

                if (nx >= 0 && nx < resolution && ny >= 0 && ny < resolution)
                {
                    if (wafer[nIdx] == 1) isPerimeter = true;
                    else if (!visited[nIdx])
                    {
                        visited[nIdx] = true;
                        queue.Enqueue(nIdx);
                    }
                }
            }

            if (isPerimeter) perimeter.Add(new Vector2(cx, cy));
        }
    }

    private Vector2 CalculateCentroid(List<Vector2> points)
    {
        Vector2 sum = Vector2.zero;
        foreach (var p in points) sum += p;
        return sum / points.Count;
    }

    private float CalculateMean(List<float> list)
    {
        if (list.Count == 0) return 0.0f;
        float sum = 0f;
        for (int i = 0; i < list.Count; i++) sum += list[i];
        return sum / list.Count;
    }

    private float Calculate3Sigma(List<float> list)
    {
        if (list.Count < 2) return 0f;
        float avg = CalculateMean(list);
        float sumSq = 0f;
        foreach (float val in list) sumSq += Mathf.Pow(val - avg, 2);
        return 3.0f * Mathf.Sqrt(sumSq / (list.Count - 1));
    }
}
