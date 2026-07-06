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
            tempSlider.minValue = 100f;
            tempSlider.maxValue = 200f;
            tempSlider.value = targetTemperature;
            tempSlider.onValueChanged.AddListener(OnTempChanged);
        }

        if (bakeTimeSlider != null)
        {
            bakeTimeSlider.minValue = 30f;
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
    }

    void OnTimeChanged(float val)
    {
        targetBakeTime = val;
        UpdateSliderReadouts();
    }

    void UpdateSliderReadouts()
    {
        if (tempSlider != null) targetTemperature = tempSlider.value;
        if (bakeTimeSlider != null) targetBakeTime = bakeTimeSlider.value;

        if (tempSliderReadout != null) tempSliderReadout.text = $"Temperature: {targetTemperature:F0} °C";
        if (timeSliderReadout != null) timeSliderReadout.text = $"Bake Time: {targetBakeTime:F0} s";
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
                        "DEHYDRATION OVEN\n" +
                        "─────────────────────────────\n" +
                        "Purpose : Drive off surface moisture before HMDS prime\n" +
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
            float remaining = Mathf.Max(0f, targetBakeTime - elapsed);
            float progress = Mathf.Clamp01(elapsed / targetBakeTime) * 100f;
            statusText.text =
                "DEHYDRATION OVEN  —  Baking\n" +
                "─────────────────────────────\n" +
                $"Temperature   : {targetTemperature:F0} °C (Constant)\n" +
                $"Elapsed Time  : {elapsed:F0} / {targetBakeTime:F0} s\n" +
                $"Dehydration   : {progress:F1} %\n" +
                "─────────────────────────────\n" +
                "Thermal dehydration in progress...";
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
                    "DEHYDRATION OVEN\n" +
                    "─────────────────────────────\n" +
                    "Purpose : Drive off surface moisture before HMDS prime\n" +
                    "─────────────────────────────\n" +
                    "OVEN EMPTY: Please place a wafer inside the dehydration oven before starting bake.";
            }
        }
        else
        {
            if (statusText != null)
            {
                statusText.text =
                    "DEHYDRATION OVEN\n" +
                    "─────────────────────────────\n" +
                    "Purpose : Drive off surface moisture before HMDS prime\n" +
                    "─────────────────────────────\n" +
                    "Wafer loaded in dock. Adjust sliders and press Bake.";
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

        isBaking = true;
        elapsed = 0f;
        if (bakeButton != null) bakeButton.interactable = false;
        if (tempSlider != null) tempSlider.interactable = false;
        if (bakeTimeSlider != null) bakeTimeSlider.interactable = false;

        Debug.Log($"[DehydrationOvenUI] Bake cycle started at {targetTemperature}°C for {targetBakeTime}s.");
    }

    void BakeDone()
    {
        isBaking = false;
        if (statusText != null)
        {
            statusText.text =
                "DEHYDRATION OVEN  —  Complete\n" +
                $"Temperature    : {targetTemperature:F0} °C\n" +
                $"Duration       : {targetBakeTime:F0} s\n" +
                "Surface State  : Hydrophobic / Anhydrous\n" +
                "─────────────────────────────\n" +
                "BAKE COMPLETE: Click oven doors to retrieve wafer.";
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
