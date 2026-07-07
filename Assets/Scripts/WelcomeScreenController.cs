using UnityEngine;

public class WelcomeScreenController : MonoBehaviour
{
    private WorkflowManager workflowManager;
    private MouseLook mouseLook;

    private void Start()
    {
        workflowManager = Object.FindAnyObjectByType<WorkflowManager>();
        mouseLook = Object.FindAnyObjectByType<MouseLook>();

        // Pause the workflow
        if (workflowManager != null)
        {
            workflowManager.workflowActive = false;
        }

        // Force cursor visible so player can click "I Acknowledge"
        if (mouseLook != null)
        {
            mouseLook.ForceUnlockForUI();
        }
    }

    public void OnClick_Acknowledge()
    {
        if (workflowManager != null)
        {
            workflowManager.workflowActive = true;
            workflowManager.currentStep = 100; // Start at SC-1 Wet Bench Cleaning stage
        }

        // Disable the UI object immediately so MouseLook's activeInHierarchy sensor lets the lock pass through!
        gameObject.SetActive(false);

        if (mouseLook != null)
        {
            mouseLook.ForceLockFromUI();
        }

        // Destroy the Welcome UI to free memory (happens at the end of the frame)
        Destroy(gameObject);
    }
}
