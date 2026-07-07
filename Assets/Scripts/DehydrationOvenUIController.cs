// DehydrationOvenUIController.cs  —  UI Controller for Dehydration Oven
// Hooks up to screen-space Canvas UI for setting temperature, bake time, and starting dehydration bake.
// Attach to: Dehydration Oven UI Panel GameObject
// Assign in Inspector: statusText, tempSlider, bakeTimeSlider, bakeButton, closeButton

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DehydrationOvenUIController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text statusText;
    public TMP_Text tempSliderReadout;
    public TMP_Text timeSliderReadout;
    public Slider tempSlider;
    public Slider bakeTimeSlider;
    public Button bakeButton;
    public Button closeButton;

    [Header("Oven Reference")]
    [Tooltip("Reference to the 3D Dehydration Oven Controller. Auto-discovered if empty.")]
    public DehydrationOvenController ovenController;

    [Header("Bake & Simulation Settings")]
    [Tooltip("Default target temperature in °C.")]
    public float targetTemperature = 150f;
    [Tooltip("Default bake time in seconds.")]
    public float targetBakeTime = 120f;
    [Tooltip("Real time in seconds to simulate the full bake duration in Unity without tedious waiting.")]
    public float realTimeTestDuration = 3.5f;

    private bool isBaking = false;
    private float elapsed = 0f;
    public bool isComplete = false;

    void Start()
    {
        if (bakeButton != null)
        {
            bakeButton.onClick.RemoveAllListeners();
            bakeButton.onClick.AddListener(StartBakeCycle);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseUI);
        }

        if (tempSlider != null)
        {
            tempSlider.minValue = 90f;
            tempSlider.maxValue = 160f;
            tempSlider.value = targetTemperature;
            tempSlider.onValueChanged.AddListener(OnTempChanged);
        }

        if (bakeTimeSlider != null)
        {
            bakeTimeSlider.minValue = 20f;
            bakeTimeSlider.maxValue = 300f;
            bakeTimeSlider.value = targetBakeTime;
            bakeTimeSlider.onValueChanged.AddListener(OnTimeChanged);
        }

        GetOven();
        UpdateSliderReadouts();
        ShowStandby();
    }

    void OnEnable()
    {
        if (!isBaking)
        {
            UpdateSliderReadouts();
            ShowStandby();
        }
    }

    private DehydrationOvenController GetOven()
    {
        if (ovenController != null) return ovenController;
        ovenController = Object.FindAnyObjectByType<DehydrationOvenController>();
        return ovenController;
    }

    void OnTempChanged(float val)
    {
        targetTemperature = val;
        UpdateSliderReadouts();
        if (!isBaking) ShowStandby();
    }

    void OnTimeChanged(float val)
    {
        targetBakeTime = val;
        UpdateSliderReadouts();
        if (!isBaking) ShowStandby();
    }

    void UpdateSliderReadouts()
    {
        if (tempSlider != null) targetTemperature = tempSlider.value;
        if (bakeTimeSlider != null) targetBakeTime = bakeTimeSlider.value;

        if (tempSliderReadout != null) tempSliderReadout.text = $"Temperature: {targetTemperature:F0} °C";
        if (timeSliderReadout != null) timeSliderReadout.text = $"Bake Time: {targetBakeTime:F0} s";
    }

    // UTD DOE HMDS contact angle model
    private float ContactAngle(float temp, float time)
    {
        float flashAngle = 58.0f + (temp - 90.0f) * 0.066f;
        float maxAngle = 78.0f + (temp - 150.0f) * 0.05f;
        float tau = 100.0f;
        return flashAngle + (maxAngle - flashAngle) * (1.0f - Mathf.Exp(-time / tau));
    }

    void Update()
    {
        if (!isBaking)
        {
            DehydrationOvenController oven = GetOven();
            bool hasWafer = (oven != null && oven.hasWafer);

            if (bakeButton != null) bakeButton.interactable = hasWafer;

            if (!hasWafer)
            {
                if (statusText != null && !statusText.text.Contains("OVEN EMPTY"))
                {
                    statusText.text =
                        "HMDS Vapour Prime Oven\n" +
                        "─────────────────────────────\n" +
                        "Purpose : Dehydration bake followed by HMDS reaction to increase hydrophobicity\n" +
                        "─────────────────────────────\n" +
                        "OVEN EMPTY: Please place a wafer inside the dehydration oven before starting bake.";
                }
            }
            else
            {
                if (statusText != null && statusText.text.Contains("OVEN EMPTY"))
                {
                    ShowStandby();
                }
            }
            return;
        }

        // Fast-forward simulation calculation
        float timeScale = (realTimeTestDuration > 0f) ? (targetBakeTime / realTimeTestDuration) : 1f;
        elapsed += Time.deltaTime * timeScale;

        if (statusText != null)
        {
            float progress = Mathf.Clamp01(elapsed / targetBakeTime) * 100f;
            statusText.text =
                "HMDS Vapour Prime Oven -- Baking\n" +
                "─────────────────────────────\n" +
                $"Temperature    : {targetTemperature:F0} °C (Constant)\n" +
                $"Elapsed Time   : {elapsed:F0} / {targetBakeTime:F0} s ({progress:F0} %)\n" +
                "─────────────────────────────\n" +
                "Status: HMDS vapor priming in progress...";
        }

        if (elapsed >= targetBakeTime)
        {
            BakeDone();
        }
    }

    void ShowStandby()
    {
        UpdateSliderReadouts();
        DehydrationOvenController oven = GetOven();
        bool hasWafer = (oven != null && oven.hasWafer);

        if (bakeButton != null) bakeButton.interactable = hasWafer;

        if (!hasWafer)
        {
            if (statusText != null)
            {
                statusText.text =
                    "HMDS Vapour Prime Oven\n" +
                    "─────────────────────────────\n" +
                    "Purpose : Dehydration bake followed by HMDS reaction to increase hydrophobicity\n" +
                    "─────────────────────────────\n" +
                    "OVEN EMPTY: Please place a wafer inside the dehydration oven before starting bake.";
            }
        }
        else
        {
            if (statusText != null)
            {
                statusText.text =
                    "HMDS Vapour Prime Oven\n" +
                    "─────────────────────────────\n" +
                    $"Temperature    : {targetTemperature:F0} °C\n" +
                    $"Prime Time     : {targetBakeTime:F0} s\n" +
                    "─────────────────────────────\n" +
                    "Status: Wafer loaded. Adjust parameters and press Bake to start HMDS vapor prime.";
            }
        }
    }

    void StartBakeCycle()
    {
        DehydrationOvenController oven = GetOven();
        if (oven != null && !oven.hasWafer)
        {
            Debug.LogWarning("[DehydrationOvenUI] Cannot start bake: oven is empty!");
            return;
        }

        isComplete = false;
        isBaking = true;
        elapsed = 0f;
        if (bakeButton != null) bakeButton.interactable = false;
        if (tempSlider != null) tempSlider.interactable = false;
        if (bakeTimeSlider != null) bakeTimeSlider.interactable = false;

        Debug.Log($"[DehydrationOvenUI] Bake cycle started at {targetTemperature}°C for {targetBakeTime}s.");
    }

    void BakeDone()
    {
        isComplete = true;
        isBaking = false;
        if (statusText != null)
        {
            float finalTheta = ContactAngle(targetTemperature, targetBakeTime);
            bool pass = finalTheta >= 65f;
            string resultStatus = pass
                ? $"PASS ({finalTheta:F1} deg >= 65.0 deg)\nBAKE COMPLETE: Click oven doors to retrieve wafer and proceed to Spin Coater."
                : $"FAIL ({finalTheta:F1} deg < 65.0 deg)\nAdjust temperature or time and retry bake.";

            statusText.text =
                "HMDS Vapour Prime Oven -- Complete\n" +
                "─────────────────────────────\n" +
                $"Temperature    : {targetTemperature:F0} °C\n" +
                $"Duration       : {targetBakeTime:F0} s\n" +
                "─────────────────────────────\n" +
                "Contact Angle Stats (UTD DOE):\n" +
                "Pre-Prime Angle: 6.0 deg (Si-OH)\n" +
                $"Post-Prime Angle: {finalTheta:F1} deg (Si-O-Si(CH3)3)\n" +
                "Adhesion Gate  : 65.0 deg\n" +
                "─────────────────────────────\n" +
                $"Status: {resultStatus}";
        }

        if (bakeButton != null) bakeButton.interactable = true;
        if (tempSlider != null) tempSlider.interactable = true;
        if (bakeTimeSlider != null) bakeTimeSlider.interactable = true;

        Debug.Log("[DehydrationOvenUI] Dehydration bake completed successfully.");
    }

    public void CloseUI()
    {
        if (CleanroomUIManager.Instance != null)
        {
            CleanroomUIManager.Instance.CloseActivePanel();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
