using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MaskAlignerWizardManager : MonoBehaviour
{
    [Header("Hierarchical Array Links")]
    [Tooltip("Drop img1 to img12 here in exact order (Index 0 = img1, Index 1 = img2, etc.)")]
    public GameObject[] stageImages;

    [Header("Top Toolbar & Global Controls")]
    public Button closeButton;
    public Button newRecipeButton;
    public Button runButton;
    public Button universalContinueButton;

    // Track our current state position (Step 1 to 12)
    private int currentStep = 1;

    void Start()
    {
        // 1. Ensure Global Close Button is persistently active
        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(true);
            closeButton.onClick.AddListener(HandleCloseTerminal);
        }

        // 2. Bind Button Click Actions
        if (newRecipeButton != null) newRecipeButton.onClick.AddListener(AdvanceToStep2);
        if (runButton != null) runButton.onClick.AddListener(AdvanceToStep3);
        if (universalContinueButton != null) universalContinueButton.onClick.AddListener(AdvanceUniversalStep);

        // 3. Initialize UI to absolute Step 1 Defaults
        ResetToStep1();
    }

    /// <summary>
    /// Enforces the initial state constraints on system boot or engine reset.
    /// </summary>
    public void ResetToStep1()
    {
        currentStep = 1;
        UpdateWizardStateLayout();
    }

    /// <summary>
    /// Core State Engine Evaluation Logic.
    /// Evaluates currentStep and locks/unlocks visibility layers natively.
    /// </summary>
    private void UpdateWizardStateLayout()
    {
        // --- 1. HANDLE IMAGE VISIBILITY ARRAY ---
        for (int i = 0; i < stageImages.Length; i++)
        {
            if (stageImages[i] != null)
            {
                // Target step matches array index + 1 (e.g., Step 1 activates index 0)
                stageImages[i].SetActive((i + 1) == currentStep);
            }
        }

        // --- 2. HANDLE INTERACTIVE CONTROL BUTTON CONSTRAINTS ---
        // Shut everything down first, then activate strictly based on state parameters
        if (newRecipeButton != null) newRecipeButton.gameObject.SetActive(false);
        if (runButton != null) runButton.gameObject.SetActive(false);
        if (universalContinueButton != null) universalContinueButton.gameObject.SetActive(false);

        switch (currentStep)
        {
            case 1:
                // Step 1: Only New Recipe button can progress the machine
                if (newRecipeButton != null) newRecipeButton.gameObject.SetActive(true);
                break;

            case 2:
                // Step 2: Only Run button can progress the machine
                if (runButton != null) runButton.gameObject.SetActive(true);
                break;

            default:
                // Step 3 to 12: Transition completely to the fixed universal button positioning
                if (currentStep <= 12)
                {
                    if (universalContinueButton != null)
                    {
                        universalContinueButton.gameObject.SetActive(true);
                        
                        // Example Hook: Disable continue on Step 6 if a process lock is incomplete
                        // universalContinueButton.interactable = EvaluateStepConstraints(currentStep);
                    }
                }
                break;
        }

        Debug.Log($"[MA Wizard] Transition complete. Currently holding state layer: Step {currentStep}");
    }

    // --- BUTTON CLICK ROUTING TARGETS ---

    private void AdvanceToStep2()
    {
        if (currentStep == 1)
        {
            currentStep = 2;
            UpdateWizardStateLayout();
        }
    }

    private void AdvanceToStep3()
    {
        if (currentStep == 2)
        {
            currentStep = 3;
            UpdateWizardStateLayout();
        }
    }

    private void AdvanceUniversalStep()
    {
        // Bound safety check to prevent spilling out past array limits
        if (currentStep >= 3 && currentStep < 12)
        {
            currentStep++;
            UpdateWizardStateLayout();
        }
        else if (currentStep == 12)
        {
            Debug.Log("[MA Wizard] Sequence completely concluded. Final stage reached.");
            // Optional: Auto-close terminal window or prompt evaluation complete card
        }
    }

    private void HandleCloseTerminal()
    {
        Debug.Log("[MA Wizard] Exit execution triggered. Closing core interface panels.");
        // Close overlay panel execution goes here (e.g., canvasRoot.SetActive(false);)
    }

    /// <summary>
    /// Placeholder for your custom validation hooks later (e.g., checking vacuum seal).
    /// </summary>
    private bool EvaluateStepConstraints(int step)
    {
        // Custom criteria evaluation rule slots
        return true; 
    }
}