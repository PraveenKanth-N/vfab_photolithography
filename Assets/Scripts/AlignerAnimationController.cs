using UnityEngine;
using System.Threading.Tasks;

public class AlignerAnimationSequencer : MonoBehaviour
{
    [Header("Component Animators")]
    public Animator trayHingeAnimator;      // Attached to Hinge Pivot
    public Animator trayLiftAnimator;       // Attached to Tray Mesh
    public Animator holderAnimator;         // Attached to Holder Plate Component
    public Animator sliderAnimator;         // Attached to Horizontal Carriage Slider
    public Animator microscopeAnimator;     // Attached to Microscope Vertical Segment

    [Header("Individual Clip Settings (Seconds)")]
    public float trayRotateDuration = 1.0f;
    public float trayLiftDuration = 0.8f;
    public float holderDuration = 0.7f;
    public float sliderMoveDuration = 1.2f;
    public float microscopeDuration = 0.5f;

    private bool isRunning = false;
    private bool isTrayInserted = false;

    // MASTER TRIGGER: Hook this directly to your UI "Toggle Mask Tray" Button click
    public void ToggleMaskTray()
    {
        if (isRunning) return;

        if (!isTrayInserted)
        {
            _ = ExecuteTrayInsertionSequence();
        }
        else
        {
            _ = ExecuteTrayEjectionSequence();
        }
    }

    // MASTER TRIGGER: Hook this directly to your UI "Execute Exposure" Button click
    public void ExecuteExposureSequence(System.Action onUVTrigger, System.Action onModelCompute)
    {
        if (isRunning || !isTrayInserted) return;
        _ = ExecuteExposureCycleAsync(onUVTrigger, onModelCompute);
    }

    // =====================================================================
    // 1. INSERTION SEQUENCE
    // =====================================================================
    private async Task ExecuteTrayInsertionSequence()
    {
        isRunning = true;
        isTrayInserted = true;
        Debug.Log("[Sequencer] Running Tray Insertion...");

        // Step 1: Rotate tray in
        PlayAnimation(trayHingeAnimator, "Tray_Rotate_In");
        await Task.Delay((int)(trayRotateDuration * 1000));

        // Step 2: Bring tray down (assumes lift-down puts it in place)
        PlayAnimation(trayLiftAnimator, "Tray_Lift_Down");
        await Task.Delay((int)(trayLiftDuration * 1000));

        // Step 3: Bring holder down
        PlayAnimation(holderAnimator, "Holder_Down");
        await Task.Delay((int)(holderDuration * 1000));

        // Step 4: Slider inspect
        PlayAnimation(sliderAnimator, "Slider_Inspect");
        await Task.Delay((int)(sliderMoveDuration * 1000));

        // Step 5: Microscope down
        PlayAnimation(microscopeAnimator, "Microscope_Down");
        await Task.Delay((int)(microscopeDuration * 1000));

        isRunning = false;
        Debug.Log("[Sequencer] Insertion Complete. Optics Engaged.");
    }

    // =====================================================================
    // 2. EJECTION SEQUENCE
    // =====================================================================
    private async Task ExecuteTrayEjectionSequence()
    {
        isRunning = true;
        isTrayInserted = false;
        Debug.Log("[Sequencer] Running Tray Ejection...");

        // Step 1: Microscope up
        PlayAnimation(microscopeAnimator, "Microscope_Up");
        await Task.Delay((int)(microscopeDuration * 1000));

        // Step 2: Slider despect
        PlayAnimation(sliderAnimator, "Slider_Despect");
        await Task.Delay((int)(sliderMoveDuration * 1000));

        // Step 3: Holder up
        PlayAnimation(holderAnimator, "Holder_Up");
        await Task.Delay((int)(holderDuration * 1000));

        // Step 4: Tray up
        PlayAnimation(trayLiftAnimator, "Tray_Lift_Up");
        await Task.Delay((int)(trayLiftDuration * 1000));

        // Step 5: Rotate tray out
        PlayAnimation(trayHingeAnimator, "Tray_Rotate_Out");
        await Task.Delay((int)(trayRotateDuration * 1000));

        isRunning = false;
        Debug.Log("[Sequencer] Ejection Complete. Tray Clear.");
    }

    // =====================================================================
    // 3. EXPOSURE CYCLE SEQUENCE
    // =====================================================================
    private async Task ExecuteExposureCycleAsync(System.Action onUVTrigger, System.Action onModelCompute)
    {
        isRunning = true;
        Debug.Log("[Sequencer] Starting Exposure Cycle...");

        // Step 1: Bring microscope up
        PlayAnimation(microscopeAnimator, "Microscope_Up");
        await Task.Delay((int)(microscopeDuration * 1000));

        // Step 2: Slider forward
        PlayAnimation(sliderAnimator, "Slider_Forward");
        await Task.Delay((int)(sliderMoveDuration * 1000));

        // STEP 3: TRIGGER UV LIGHT AND AI CALCULATION RUNTIME HERE
        Debug.Log("[Sequencer] Firing UV Optics Beam...");
        onUVTrigger?.Invoke();    // Triggers your UV graphic flashes
        onModelCompute?.Invoke(); // Fires your UNet Inference pipeline array
        
        // Hold exposure window open briefly for visual effect
        await Task.Delay(1500); 

        // Step 4: Slider back (backward)
        PlayAnimation(sliderAnimator, "Slider_Backward");
        await Task.Delay((int)(sliderMoveDuration * 1000));

        // Step 5: Bring microscope down
        PlayAnimation(microscopeAnimator, "Microscope_Down");
        await Task.Delay((int)(microscopeDuration * 1000));

        isRunning = false;
        Debug.Log("[Sequencer] Exposure Cycle Finished.");
    }

    // Helper wrapper to prevent console exceptions if reference blocks are empty
    private void PlayAnimation(Animator animator, string stateName)
    {
        if (animator != null)
        {
            animator.Play(stateName);
        }
        else
        {
            Debug.LogWarning($"[Sequencer Missing Reference]: Cannot play '{stateName}' because animator slot is unassigned.");
        }
    }
}