using UnityEngine;
using System.Collections;

public class DehydrationOvenController : MonoBehaviour
{
    [Header("Hinge Pivot References")]
    [Tooltip("Empty GameObject placed at the left door's hinge edge.")]
    public Transform door1Hinge;
    
    [Tooltip("Empty GameObject placed at the right door's hinge edge.")]
    public Transform door2Hinge;

    [Header("Angle Constraints (Z Axis Rotation)")]
    [Tooltip("Closed Z angle for Door 1 (left hinge).")]
    public float door1ClosedAngleZ = -90f;
    [Tooltip("Opened Z angle for Door 1.")]
    public float door1OpenedAngleZ = 0f;

    [Tooltip("Closed Z angle for Door 2 (right hinge).")]
    public float door2ClosedAngleZ = 90f;
    [Tooltip("Opened Z angle for Door 2.")]
    public float door2OpenedAngleZ = 0f;

    [Header("Animation Settings")]
    public float animationSpeed = 4.0f;
    public bool isOpen = false;
    public bool isAnimating = false;
    public bool hasWafer = false;

    [Header("Interior Dock")]
    [Tooltip("Anchor point inside the oven where wafers can be placed.")]
    public Transform ovenWaferDock;

    private GameObject storedWafer;

    void Start()
    {
        // Auto-discover hinges if not explicitly assigned in the Inspector
        if (door1Hinge == null || door2Hinge == null)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                string tName = t.name.ToLower();
                if (door1Hinge == null && (tName.Contains("door1") || tName.Contains("left")) && tName.Contains("hinge"))
                {
                    door1Hinge = t;
                }
                else if (door2Hinge == null && (tName.Contains("door2") || tName.Contains("right")) && tName.Contains("hinge"))
                {
                    door2Hinge = t;
                }
            }
        }

        // Initialize physical rotation to match isOpen state on start
        float startZ1 = isOpen ? door1OpenedAngleZ : door1ClosedAngleZ;
        float startZ2 = isOpen ? door2OpenedAngleZ : door2ClosedAngleZ;

        if (door1Hinge != null) door1Hinge.localRotation = Quaternion.Euler(0f, 0f, startZ1);
        if (door2Hinge != null) door2Hinge.localRotation = Quaternion.Euler(0f, 0f, startZ2);
    }

    public void ToggleOvenDoors()
    {
        if (isAnimating) return;
        StartCoroutine(AnimateDoorsRoutine(!isOpen));
    }

    public void InsertWaferSequence(GameObject wafer)
    {
        if (isAnimating || hasWafer || ovenWaferDock == null) return;
        StartCoroutine(InsertWaferRoutine(wafer));
    }

    private IEnumerator InsertWaferRoutine(GameObject wafer)
    {
        isAnimating = true;
        storedWafer = wafer;
        hasWafer = true;

        // 1. Immediately teleport wafer to the dock center so it NEVER gets stuck mid-air!
        wafer.transform.SetParent(ovenWaferDock, false);
        wafer.transform.localPosition = Vector3.zero;
        wafer.transform.localRotation = Quaternion.identity;

        Rigidbody rb = wafer.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.detectCollisions = false; }

        // 2. If doors are closed, swing them open so player sees inside!
        if (!isOpen)
        {
            yield return StartCoroutine(AnimateDoorsRoutine(true));
            yield return new WaitForSeconds(0.2f);
        }

        // 3. Swing doors shut over the wafer
        yield return StartCoroutine(AnimateDoorsRoutine(false));

        isAnimating = false;
        Debug.Log("[Dehydration Oven] Wafer inserted and doors closed.");
    }

    public void ExtractWaferSequence(System.Action<GameObject> onExtracted)
    {
        if (isAnimating || !hasWafer || storedWafer == null) return;
        StartCoroutine(ExtractWaferRoutine(onExtracted));
    }

    private IEnumerator ExtractWaferRoutine(System.Action<GameObject> onExtracted)
    {
        isAnimating = true;

        // 1. Force the doors to open FIRST!
        if (!isOpen)
        {
            yield return StartCoroutine(AnimateDoorsRoutine(true));
            yield return new WaitForSeconds(0.2f);
        }

        GameObject extracted = storedWafer;
        storedWafer = null;
        hasWafer = false;

        // 2. Immediately teleport wafer directly to player's hand!
        onExtracted?.Invoke(extracted);

        isAnimating = false;
        Debug.Log("[Dehydration Oven] Wafer extracted to player hand.");
    }

    private IEnumerator AnimateDoorsRoutine(bool openTarget)
    {
        isOpen = openTarget;
        float targetZ1 = openTarget ? door1OpenedAngleZ : door1ClosedAngleZ;
        float targetZ2 = openTarget ? door2OpenedAngleZ : door2ClosedAngleZ;

        Quaternion startRot1 = door1Hinge != null ? door1Hinge.localRotation : Quaternion.identity;
        Quaternion startRot2 = door2Hinge != null ? door2Hinge.localRotation : Quaternion.identity;

        Quaternion targetRot1 = Quaternion.Euler(0f, 0f, targetZ1);
        Quaternion targetRot2 = Quaternion.Euler(0f, 0f, targetZ2);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * animationSpeed;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            if (door1Hinge != null) door1Hinge.localRotation = Quaternion.Lerp(startRot1, targetRot1, smoothT);
            if (door2Hinge != null) door2Hinge.localRotation = Quaternion.Lerp(startRot2, targetRot2, smoothT);
            yield return null;
        }

        if (door1Hinge != null) door1Hinge.localRotation = targetRot1;
        if (door2Hinge != null) door2Hinge.localRotation = targetRot2;
    }
}
