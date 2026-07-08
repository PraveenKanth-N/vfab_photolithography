using UnityEngine;
using System.Collections;

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

    public bool isRunning = false;
    public bool isTrayInserted = false;

    public void ToggleMaskTray()
    {
        if (isRunning) return;

        if (!isTrayInserted)
        {
            StartCoroutine(ExecuteTrayInsertionSequence());
        }
        else
        {
            StartCoroutine(ExecuteTrayEjectionSequence());
        }
    }

    public void ExecuteExposureSequence(System.Action onUVTrigger, System.Action onModelCompute)
    {
        StartCoroutine(ExecuteExposureCycleAsync(onUVTrigger, onModelCompute));
    }

    // 1. INSERTION SEQUENCE
    private IEnumerator ExecuteTrayInsertionSequence()
    {
        isRunning = true;
        isTrayInserted = true;

        // Step 1: Rotate tray in
        PlayAnimation(trayHingeAnimator, "Tray_Rotate_In");
        yield return new WaitForSeconds(trayRotateDuration);

        // Step 2: Bring tray down (assumes lift-down puts it in place)
        PlayAnimation(trayLiftAnimator, "Tray_Lift_Down");
        yield return new WaitForSeconds(trayLiftDuration);

        // Step 3: Bring holder down
        PlayAnimation(holderAnimator, "Holder_Down");
        yield return new WaitForSeconds(holderDuration);

        // Step 4: Slider inspect
        PlayAnimation(sliderAnimator, "Slider_Inspect");
        yield return new WaitForSeconds(sliderMoveDuration);

        // Step 5: Microscope down
        PlayAnimation(microscopeAnimator, "Microscope_Down");
        yield return new WaitForSeconds(microscopeDuration);

        isRunning = false;
    }

    // 2. EJECTION SEQUENCE
    private IEnumerator ExecuteTrayEjectionSequence()
    {
        isRunning = true;
        isTrayInserted = false;

        // Step 1: Microscope up
        PlayAnimation(microscopeAnimator, "Microscope_Up");
        yield return new WaitForSeconds(microscopeDuration);

        // Step 2: Slider despect
        PlayAnimation(sliderAnimator, "Slider_Despect");
        yield return new WaitForSeconds(sliderMoveDuration);

        // Step 3: Holder up
        PlayAnimation(holderAnimator, "Holder_Up");
        yield return new WaitForSeconds(holderDuration);

        // Step 4: Tray up
        PlayAnimation(trayLiftAnimator, "Tray_Lift_Up");
        yield return new WaitForSeconds(trayLiftDuration);

        // Step 5: Rotate tray out
        PlayAnimation(trayHingeAnimator, "Tray_Rotate_Out");
        yield return new WaitForSeconds(trayRotateDuration);

        isRunning = false;
        Debug.Log("[Sequencer] Ejection Complete. Tray Clear.");
    }

    // 3. EXPOSURE CYCLE SEQUENCE
    private IEnumerator ExecuteExposureCycleAsync(System.Action onUVTrigger, System.Action onModelCompute)
    {
        isRunning = true;
        Debug.Log("[Sequencer] Starting Exposure Cycle...");

        // Step 1: Bring microscope up
        PlayAnimation(microscopeAnimator, "Microscope_Up");
        yield return new WaitForSeconds(microscopeDuration);

        // Step 2: Slider forward
        PlayAnimation(sliderAnimator, "Slider_Forward");
        yield return new WaitForSeconds(sliderMoveDuration);

        // STEP 3: TRIGGER UV LIGHT AND AI CALCULATION RUNTIME HERE
        Debug.Log("[Sequencer] Firing UV Optics Beam...");
        onUVTrigger?.Invoke();    // Triggers your UV graphic flashes
        onModelCompute?.Invoke(); // Fires your UNet Inference pipeline array
        
        // Hold exposure window open briefly for visual effect
        yield return new WaitForSeconds(1.5f); 

        // Step 4: Slider back (backward)
        PlayAnimation(sliderAnimator, "Slider_Backward");
        yield return new WaitForSeconds(sliderMoveDuration);

        // Step 5: Bring microscope down
        PlayAnimation(microscopeAnimator, "Microscope_Down");
        yield return new WaitForSeconds(microscopeDuration);

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