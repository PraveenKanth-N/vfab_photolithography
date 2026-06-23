using UnityEngine;
using System.Collections;

public class WaferCarrierController : MonoBehaviour
{
    [Header("Carrier Mechanics")]
    public Transform hinge;
    public Transform waferDock;
    
    [Header("Animation Settings")]
    public Vector3 openRotation = new Vector3(0f, 0f, 100f);
    public Vector3 closedRotation = Vector3.zero;
    public float animationSpeed = 1.0f;

    [Header("State Tracking")]
    public bool hasWafer = false;
    public bool isOpen = false; // Default state is closed
    public bool isAnimating = false;
    public bool isReadyForExtraction = false; // Sequence tracking

    private GameObject storedWafer;

    void Start()
    {
        // Force the initial physical state to match the boolean state
        if (hinge != null)
        {
            hinge.localRotation = Quaternion.Euler(isOpen ? openRotation : closedRotation);
        }
    }

    public void InsertWaferSequence(GameObject wafer)
    {
        if (isAnimating || hasWafer) return;
        StartCoroutine(InsertWaferRoutine(wafer));
    }

    private IEnumerator InsertWaferRoutine(GameObject wafer)
    {
        isAnimating = true;
        storedWafer = wafer;
        hasWafer = true;
        isReadyForExtraction = false; // Sequence: Needs to be moved before extraction

        // Instantly secure the wafer so it doesn't fall, but float it above
        wafer.transform.SetParent(waferDock, false);
        wafer.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        wafer.transform.localRotation = Quaternion.identity;

        // 1. Open Lid
        if (!isOpen && hinge != null) yield return StartCoroutine(AnimateLidRoutine(true));

        // 2. Drop wafer in smoothly
        Vector3 startPos = wafer.transform.localPosition;
        Vector3 endPos = new Vector3(0f, 0.02f, 0f);
        float t = 0;
        while(t < 1f) {
            t += Time.deltaTime * animationSpeed;
            wafer.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        // 3. Close Lid
        if (hinge != null) yield return StartCoroutine(AnimateLidRoutine(false));

        isAnimating = false;
    }

    public void ExtractWaferSequence(System.Action<GameObject> onExtracted)
    {
        if (isAnimating || !hasWafer) return;
        StartCoroutine(ExtractWaferRoutine(onExtracted));
    }

    private IEnumerator ExtractWaferRoutine(System.Action<GameObject> onExtracted)
    {
        isAnimating = true;

        // 1. Open Lid
        if (!isOpen && hinge != null) yield return StartCoroutine(AnimateLidRoutine(true));

        // 2. Float wafer up
        Vector3 startPos = storedWafer.transform.localPosition;
        Vector3 endPos = new Vector3(0f, 0.2f, 0f);
        float t = 0;
        while(t < 1f) {
            t += Time.deltaTime * animationSpeed;
            storedWafer.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        GameObject extracted = storedWafer;
        storedWafer = null;
        hasWafer = false;

        // 3. Pass to player hand
        onExtracted?.Invoke(extracted);

        // Keep lid open so player knows it's empty
        isAnimating = false;
    }

    private IEnumerator AnimateLidRoutine(bool openTarget)
    {
        isOpen = openTarget;
        Vector3 targetEuler = openTarget ? openRotation : closedRotation;
        Quaternion startRot = hinge.localRotation;
        Quaternion targetRot = Quaternion.Euler(targetEuler);
        
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * animationSpeed;
            hinge.localRotation = Quaternion.Lerp(startRot, targetRot, t);
            yield return null;
        }
        hinge.localRotation = targetRot;
    }
}
