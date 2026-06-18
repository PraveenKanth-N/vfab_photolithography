using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ExposureStationController : MonoBehaviour
{
    [Header("Engine & Sequencer Links")]
    public LithoInferenceEngine lithoAIModelEngine;
    public AlignerAnimationSequencer hardwareSequencer;

    [Header("Process Tracking")]
    public bool isExposingNow = false;
    public bool isVacuumSealed = false;        // Default: Unlocked
    public bool isMaskTrayInserted = false;    // Default: Open/Out

    [Header("UI Control Buttons")]
    public Button maskTrayToggleButton;
    public Button vacuumButton;
    public Button executeExposureButton;

    [Header("UI Display Output")]
    public TMP_Text stationStatusText;

    [Header("Simulation Visual References")]
    public GameObject uvChamberLightSource;    
    public Transform waferDockPosition;   
    public Transform maskTrayTransform;
    public Transform topDeckAnchor;

    void Start()
    {
        // Bind UI triggers directly to the new workflow setup
        if (maskTrayToggleButton != null)
        {
            maskTrayToggleButton.onClick.RemoveAllListeners();
            maskTrayToggleButton.onClick.AddListener(HandleTrayToggleClick);
        }
        if (vacuumButton != null)
        {
            vacuumButton.onClick.RemoveAllListeners();
            vacuumButton.onClick.AddListener(ToggleVacuumClamp);
        }
        if (executeExposureButton != null)
        {
            executeExposureButton.onClick.RemoveAllListeners();
            executeExposureButton.onClick.AddListener(TriggerUVLampIgnition);
        }

        if (uvChamberLightSource != null) uvChamberLightSource.SetActive(false);

        // Run an initialization check to grey out buttons correctly on boot
        UpdateSystemWorkflowState("System initialized. Substrate accessible.");
    }

    // STEP 1: TOGGLE MASK TRAY (Runs the 5-step animation chain back or forth)
    public void HandleTrayToggleClick()
    {
        if (isExposingNow || hardwareSequencer == null || hardwareSequencer.isRunning) return;

        // Toggle state tracking flag
        isMaskTrayInserted = !isMaskTrayInserted;

        // Fire the sequence animation deck
        hardwareSequencer.ToggleMaskTray();

        if (isMaskTrayInserted)
        {
            UpdateSystemWorkflowState("Loading Mask Tray & Aligning Optics...");
            StartCoroutine(TransferMaskLogic(true, hardwareSequencer.trayRotateDuration));
        }
        else
        {
            // If they eject, break vacuum automatically as a safety measure
            isVacuumSealed = false;
            UpdateSystemWorkflowState("Ejecting Hardware Components...");
            StartCoroutine(TransferMaskLogic(false, hardwareSequencer.trayRotateDuration));
        }
    }

    private IEnumerator TransferMaskLogic(bool isInserting, float delay)
    {
        if (!isInserting) yield break; // Transfers only happen when the tray reaches the INSIDE of the machine!

        yield return new WaitForSeconds(delay);

        Transform chuck = transform.Find("Hinge/Rotator/Chuck/Aligner_Wafer_Anchor");
        if (chuck == null) chuck = waferDockPosition;
        if (chuck == null || topDeckAnchor == null) yield break;

        Transform maskOnChuck = null;
        bool hasTopLid = false;

        foreach (Transform child in chuck)
        {
            if (child.name.ToLower().Contains("mask")) maskOnChuck = child;
            if (child.name.ToLower().Contains("toplid")) hasTopLid = true;
        }

        Transform maskOnTopDeck = topDeckAnchor.childCount > 0 ? topDeckAnchor.GetChild(0) : null;

        // SCENARIO 1: Inserting a Photomask. Vacuum it up to the TopDeck!
        if (maskOnChuck != null && maskOnTopDeck == null)
        {
            maskOnChuck.SetParent(topDeckAnchor, false);
            maskOnChuck.localPosition = Vector3.zero;
            maskOnChuck.localRotation = Quaternion.identity;
        }
        // SCENARIO 2: Inserting empty TopLid to retrieve the Photomask. Drop mask back onto Chuck!
        else if (maskOnTopDeck != null && hasTopLid)
        {
            maskOnTopDeck.SetParent(chuck, false);
            maskOnTopDeck.localPosition = new Vector3(0f, 0.015f, 0f);
            maskOnTopDeck.localRotation = Quaternion.identity;
            maskOnTopDeck.localScale = new Vector3(0.5f, 0.01f, 0.5f);
        }
    }

    // STEP 2: TOGGLE VACUUM CLAMP
    public void ToggleVacuumClamp()
    {
        if (isExposingNow || !isMaskTrayInserted) return;
        
        isVacuumSealed = !isVacuumSealed;
        UpdateSystemWorkflowState(isVacuumSealed ? "Vacuum seal LOCKED. Execute sequence enabled." : "Vacuum seal UNLOCKED. Substrate pickup active.");
    }

    // STEP 3: EXECUTE UV EXPOSURE WITH SEQUENCER INTEGRATION
    public void TriggerUVLampIgnition()
    {
        Debug.Log($"[HARDWARE STATION] TriggerUVLampIgnition Called! Sealed: {isVacuumSealed}, ExposingNow: {isExposingNow}, SequencerAttached: {hardwareSequencer != null}");

        if (!isVacuumSealed) { Debug.LogError("[HARDWARE STATION] Aborting: Vacuum is NOT sealed!"); return; }
        if (isExposingNow) { Debug.LogError("[HARDWARE STATION] Aborting: System is already exposing!"); return; }
        if (hardwareSequencer == null) { Debug.LogError("[HARDWARE STATION] Aborting: hardwareSequencer slot is empty!"); return; }

        isExposingNow = true;
        UpdateSystemWorkflowState("Initiating exposure sequence steps...");

        Debug.Log("[HARDWARE STATION] Handing execution to Sequencer script...");

        // Hand over execution control directly to the hardware sequencer
        hardwareSequencer.ExecuteExposureSequence(
            onUVTrigger: () => 
            {
                // Callback 1: Instantly trigger physical UV lamp lighting/FX mesh
                if (uvChamberLightSource != null) uvChamberLightSource.SetActive(true);
                UpdateSystemWorkflowState("WARNING: UV Exposure Active. Lamp Stable.");
            },
            onModelCompute: () => 
            {
                // Callback 2: Run your Sentis AI UNet Inference engine pipeline
                if (lithoAIModelEngine != null)
                {
                    lithoAIModelEngine.ExecuteModelExposurePipeline();
                }

                // Smoothly clean up and shut off light emission once computation routine clears
                StartCoroutine(WrapUpExposureWithDelay());
            }
        );
    }

    private IEnumerator WrapUpExposureWithDelay()
    {
        // Keep the light visible slightly so the flare doesn't blink out instantenously
        yield return new WaitForSeconds(1.5f);

        if (uvChamberLightSource != null) uvChamberLightSource.SetActive(false);

        isExposingNow = false;
        UpdateSystemWorkflowState("Exposure complete! Latent diffraction image stabilized.");
    }

    // =====================================================================
    // STRICT INTERLOCK HARDWARE ENGINE (Locks down unready buttons)
    // =====================================================================
    private void UpdateSystemWorkflowState(string message)
    {
        // Status text string readouts
        if (stationStatusText != null) stationStatusText.text = "Mask Aligner: " + message;

        // 1. Tray button locked if exposure cycle is actively running
        if (maskTrayToggleButton != null) maskTrayToggleButton.interactable = !isExposingNow;

        // 2. Can only seal vacuum if tray is inside the machine housing
        if (vacuumButton != null) vacuumButton.interactable = isMaskTrayInserted && !isExposingNow;

        // Update the visual text representation on the toggle button itself
        var vacText = vacuumButton != null ? vacuumButton.GetComponentInChildren<TMP_Text>() : null;
        if (vacText != null) vacText.text = isVacuumSealed ? "Release Vacuum" : "Engage Vacuum Seal";

        // 3. CRITICAL ENGINE INTERLOCK: Light cannot shoot unless vacuum seal is active!
        if (executeExposureButton != null)
        {
            executeExposureButton.interactable = isMaskTrayInserted && isVacuumSealed && !isExposingNow;
        }
    }
}