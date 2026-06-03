using UnityEngine;

public class CleanroomUIManager : MonoBehaviour
{
    public static CleanroomUIManager Instance { get; private set; }

    [Header("Camera Tracking Reference")]
    public MouseLook cameraLookScript; // Drag your main camera holding MouseLook here!

    [Header("Screen Space Machine Panels")]
    public GameObject hotplateUIPanel;
    public GameObject spinCoaterUIPanel;
    public GameObject dispenserUIPanel;
    public GameObject exposureUIPanel;

    private GameObject activePanel = null;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DeactivateAllPanels();
    }

    public void OpenMachinePanel(string machineName)
    {
        DeactivateAllPanels();

        // 1. FREEZE CAMERA & RELEASE MOUSE: Invoke the custom hook on your look script
        if (cameraLookScript != null)
        {
            cameraLookScript.ForceUnlockForUI();
        }

        // Activate the corresponding overlay screen layout
        switch (machineName)
        {
            case "Hotplate":
                if (hotplateUIPanel != null) { hotplateUIPanel.SetActive(true); activePanel = hotplateUIPanel; }
                break;
            case "SpinCoater":
                if (spinCoaterUIPanel != null) { spinCoaterUIPanel.SetActive(true); activePanel = spinCoaterUIPanel; }
                break;
            case "Dispenser":
                if (dispenserUIPanel != null) { dispenserUIPanel.SetActive(true); activePanel = dispenserUIPanel; }
                break;
            case "Exposure":
                if (exposureUIPanel != null) { exposureUIPanel.SetActive(true); activePanel = exposureUIPanel; }
                break;
        }
    }

    public void CloseActivePanel()
    {
        if (activePanel != null)
        {
            activePanel.SetActive(false);
            activePanel = null;
        }

        // 1. UNFREEZE CAMERA & HIDE MOUSE: Re-engage standard in-world navigation
        if (cameraLookScript != null)
        {
            cameraLookScript.ForceLockFromUI();
        }
    }

    private void DeactivateAllPanels()
    {
        if (hotplateUIPanel != null) hotplateUIPanel.SetActive(false);
        if (spinCoaterUIPanel != null) spinCoaterUIPanel.SetActive(false);
        if (dispenserUIPanel != null) dispenserUIPanel.SetActive(false);
        if (exposureUIPanel != null) exposureUIPanel.SetActive(false);
    }
}