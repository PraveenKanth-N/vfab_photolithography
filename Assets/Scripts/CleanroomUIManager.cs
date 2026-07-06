using UnityEngine;

public class CleanroomUIManager : MonoBehaviour
{
    public static CleanroomUIManager Instance { get; private set; }

    [Header("Camera Tracking Reference")]
    public MouseLook cameraLookScript; 

    [Header("Screen Space Canvas Panels")]
    public GameObject hotplateUIPanel;
    public GameObject spinCoaterUIPanel;
    public GameObject dispenserUIPanel;
    public GameObject oldExposureUIPanel; // 🚨 Drag your old Canvas Exposure_UI here!
    public GameObject sc1CleanerUIPanel;  // 🚨 Drag your SC1 Cleaner UI Panel here!
    public GameObject dehydrationOvenUIPanel; // 🚨 Drag your Dehydration Oven UI Panel here!
    
    [Header("UI Toolkit Integration")]
    public MainWindowController exposureUIController; // 🚨 Drag the component here!

    private GameObject activeGameObjectPanel = null;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (exposureUIController == null)
        {
            Debug.LogError("[CleanroomUIManager] Exposure UI Controller slot is missing! Please drag the GameObject containing MainWindowController into the 'Exposure UI Controller' slot in the CleanroomUIManager inspector.");
        }
        DeactivateAllPanels();
    }

    public void OpenMachinePanel(string machineName)
    {
        DeactivateAllPanels();

        // Freeze world space looking axes
        if (cameraLookScript != null) cameraLookScript.ForceUnlockForUI();

        switch (machineName)
        {
            case "Hotplate":
                if (hotplateUIPanel != null) { hotplateUIPanel.SetActive(true); activeGameObjectPanel = hotplateUIPanel; }
                break;
            case "SpinCoater":
                if (spinCoaterUIPanel != null) { spinCoaterUIPanel.SetActive(true); activeGameObjectPanel = spinCoaterUIPanel; }
                break;
            case "Dispenser":
                if (dispenserUIPanel != null) { dispenserUIPanel.SetActive(true); activeGameObjectPanel = dispenserUIPanel; }
                break;
            case "Cleaner_hotplate":
            case "Cleaner":
            case "SC1":
                if (sc1CleanerUIPanel != null) { sc1CleanerUIPanel.SetActive(true); activeGameObjectPanel = sc1CleanerUIPanel; }
                break;
            case "Oven":
            case "DehydrationOven":
            case "dehydration_oven":
            case "Dehydration_Oven":
                if (dehydrationOvenUIPanel != null) { dehydrationOvenUIPanel.SetActive(true); activeGameObjectPanel = dehydrationOvenUIPanel; }
                break;
            case "ExposureControls": // The old Canvas UI
                if (oldExposureUIPanel != null) { oldExposureUIPanel.SetActive(true); activeGameObjectPanel = oldExposureUIPanel; }
                break;
            case "ExposureMachine": // The new UXML 8-Step UI
            case "Exposure": // Fallback
                if (exposureUIController != null) 
                { 
                    exposureUIController.OpenInterface();
                }
                break;
        }
    }

    public void CloseActivePanel()
    {
        if (activeGameObjectPanel != null)
        {
            activeGameObjectPanel.SetActive(false);
            activeGameObjectPanel = null;
        }

        if (exposureUIController != null)
        {
            exposureUIController.CloseEntireInterface();
        }

        if (cameraLookScript != null) cameraLookScript.ForceLockFromUI();
    }

    private void DeactivateAllPanels()
    {
        if (hotplateUIPanel != null) hotplateUIPanel.SetActive(false);
        if (spinCoaterUIPanel != null) spinCoaterUIPanel.SetActive(false);
        if (dispenserUIPanel != null) dispenserUIPanel.SetActive(false);
        if (oldExposureUIPanel != null) oldExposureUIPanel.SetActive(false);
        if (sc1CleanerUIPanel != null) sc1CleanerUIPanel.SetActive(false);
        if (dehydrationOvenUIPanel != null) dehydrationOvenUIPanel.SetActive(false);
        
        if (exposureUIController != null)
        {
            exposureUIController.CloseEntireInterface();
        }
    }
    
    public bool IsAnyPanelOpen()
    {
        bool isUxmlOpen = (exposureUIController != null) && exposureUIController.IsPanelCurrentlyOpen();
        return activeGameObjectPanel != null || isUxmlOpen;
    }
}