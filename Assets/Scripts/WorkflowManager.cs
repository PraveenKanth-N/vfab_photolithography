using UnityEngine;
using TMPro;

public class WorkflowManager : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("Drag your HUD TMPro Text element here")]
    public TextMeshProUGUI hudText;

    [Header("Workflow State")]
    public int currentStep = 0;
    
    [Header("UI Customization")]
    [Tooltip("Tweak this to align the background box perfectly behind the text")]
    public Vector2 backgroundOffset = new Vector2(0f, -10f);
    public bool workflowActive = false; // Controlled by the WelcomeScreenController
    
    [Header("Context Image")]
    [Tooltip("Drag the ContextImg (RawImage or Image) GameObject here")]
    public GameObject contextImgObj;
    [Tooltip("Optional: Drag your 'I understand' button here to auto-hide/disable it")]
    public UnityEngine.UI.Button contextAcknowledgeButton;
    [Tooltip("Optional: Drag the Title Text GameObject here to sync its visibility")]
    public GameObject contextImgTitle;
    private bool hasShownChuckContext = false;

    // Cached References
    private SpinCoaterController spinCoater;
    private PlayerInteraction playerInteraction;
    private HotplateController hotplate;
    private DeveloperUIManager developerUI;
    private ExposureStationController exposureStation;
    private MainWindowController mainWindowController;
    private SC1Controller sc1Controller;
    private DehydrationOvenController ovenController;
    private float stepTimer = 0f;

    // UI Syncing
    private RectTransform textRect;
    private RectTransform bgRect;

    private void Start()
    {
        // Try to find the required managers/controllers automatically
        spinCoater = Object.FindAnyObjectByType<SpinCoaterController>();
        playerInteraction = Object.FindAnyObjectByType<PlayerInteraction>();
        hotplate = HotplateController.GetProcessingHotplate();
        developerUI = Object.FindAnyObjectByType<DeveloperUIManager>();
        exposureStation = Object.FindAnyObjectByType<ExposureStationController>();
        mainWindowController = Object.FindAnyObjectByType<MainWindowController>();
        sc1Controller = Object.FindAnyObjectByType<SC1Controller>();
        ovenController = Object.FindAnyObjectByType<DehydrationOvenController>();
        
        if (hudText != null)
        {
            // Fix blur and rendering order by forcing the parent Canvas to Overlay and high sorting order.
            // Since you moved this text to its own dedicated GuideCanvas, this won't pull the crosshair up!
            Canvas parentCanvas = hudText.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                parentCanvas.sortingOrder = 32000;
            }

            // Force the text to be 100% solid, sharp black
            hudText.color = Color.black;

            // Procedurally generate a 100% solid white background box behind the text
            GameObject bgObj = new GameObject("WorkflowBackground");
            bgObj.transform.SetParent(hudText.transform.parent, false);
            bgObj.transform.SetSiblingIndex(hudText.transform.GetSiblingIndex()); // Place directly behind text
            
            UnityEngine.UI.Image bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(1f, 1f, 1f, 1f); // 100% solid white
            bgImage.raycastTarget = false;

            bgRect = bgObj.GetComponent<RectTransform>();
            textRect = hudText.GetComponent<RectTransform>();
        }
        else
        {
            Debug.LogWarning("[WorkflowManager] HUD Text is not assigned! Please drag your TMPro text element into the Inspector slot.");
        }
        
        if (contextImgObj != null)
        {
            contextImgObj.SetActive(false);
        }
        if (contextAcknowledgeButton != null)
        {
            contextAcknowledgeButton.gameObject.SetActive(false);
        }
        if (contextImgTitle != null)
        {
            contextImgTitle.SetActive(false);
        }
    }

    // Helper method to set text without mark tags
    private void SetText(string content)
    {
        hudText.text = content;
    }

    private void Update()
    {
        if (hudText == null) return;
        
        if (!workflowActive)
        {
            if (bgRect != null) bgRect.gameObject.SetActive(false);
            SetText("");
            return;
        }
        else
        {
            if (bgRect != null) bgRect.gameObject.SetActive(true);
        }

        // Dynamically shrink/grow the background box based on the EXACT width of the letters
        if (bgRect != null && textRect != null)
        {
            bgRect.anchorMin = textRect.anchorMin;
            bgRect.anchorMax = textRect.anchorMax;
            bgRect.pivot = textRect.pivot;
            
            // Match position and apply user's custom offset for perfect alignment
            bgRect.anchoredPosition = textRect.anchoredPosition + backgroundOffset;
            
            // Use preferredWidth/Height to perfectly hug the text regardless of the UI box size
            bgRect.sizeDelta = new Vector2(hudText.preferredWidth + 40f, hudText.preferredHeight + 20f); 
        }

        switch (currentStep)
        {
            // The WelcomeScreenController instantly sets currentStep = 2 when 'I Acknowledge' is clicked.
            case 2:
                SetText("Approach the spincoater machine, and click the highlighted yellow portion on its right to open up its menu");
                if (CleanroomUIManager.Instance != null && CleanroomUIManager.Instance.spinCoaterUIPanel != null)
                {
                    if (CleanroomUIManager.Instance.spinCoaterUIPanel.activeSelf)
                    {
                        currentStep = 3;
                    }
                }
                break;

            case 3:
                SetText("Toggle the spincoater to open its hatch");
                if (spinCoater != null && spinCoater.lidController != null && spinCoater.lidController.isOpen)
                {
                    currentStep = 4;
                }
                break;

            case 4:
                SetText("Close the Spin Coater menu");
                if (CleanroomUIManager.Instance != null && !CleanroomUIManager.Instance.IsAnyPanelOpen())
                {
                    currentStep = 5;
                }
                break;

            case 5:
                SetText("Look over to the hotplate on the left and click on the wafer to grab it");
                if (IsHoldingWaferOrSubstrate())
                {
                    currentStep = 6;
                }
                break;

            case 6:
                SetText("Place the Wafer on the spincoater by clicking the spincoater region");
                if (spinCoater != null && spinCoater.currentCoaterState != SpinCoaterController.CoaterState.Empty)
                {
                    currentStep = 7;
                }
                break;

            case 7:
                SetText("Click on the spincoater's highlighted region to open its menu again");
                if (CleanroomUIManager.Instance != null && CleanroomUIManager.Instance.spinCoaterUIPanel != null)
                {
                    if (CleanroomUIManager.Instance.spinCoaterUIPanel.activeSelf)
                    {
                        currentStep = 8;
                    }
                }
                break;

            case 8:
                SetText("Adjust the speed and time if required, then dispense the Photoresist (PR)");
                if (spinCoater != null && (spinCoater.currentCoaterState == SpinCoaterController.CoaterState.PRDispensing || spinCoater.currentCoaterState == SpinCoaterController.CoaterState.PRDispensed))
                {
                    currentStep = 9;
                }
                break;

            case 9:
                SetText("Toggle the spincoater to close its hatch");
                if (spinCoater != null && spinCoater.lidController != null && !spinCoater.lidController.isOpen)
                {
                    currentStep = 10;
                }
                break;

            case 10:
                SetText("Start the spin operation");
                if (spinCoater != null && (spinCoater.currentCoaterState == SpinCoaterController.CoaterState.Spinning || spinCoater.currentCoaterState == SpinCoaterController.CoaterState.Complete))
                {
                    currentStep = 11;
                }
                break;

            case 11:
                SetText("Once complete, Thickness value of the PR will be visible");
                if (spinCoater != null && spinCoater.currentCoaterState == SpinCoaterController.CoaterState.Complete)
                {
                    stepTimer = 3f; 
                    currentStep = 12;
                }
                break;

            case 12:
                // Non-blocking timer: Shows metrics text briefly, then asks to toggle open hatch
                if (stepTimer > 0f)
                {
                    stepTimer -= Time.deltaTime;
                    SetText("Once complete, Thickness value of the PR will be visible");
                }
                else
                {
                    SetText("Toggle the spincoater to open its hatch");
                }
                if (spinCoater != null && spinCoater.lidController != null && spinCoater.lidController.isOpen)
                {
                    currentStep = 13;
                }
                // Allow INSTANT skipping if they physically removed the wafer already
                if (spinCoater != null && spinCoater.currentCoaterState == SpinCoaterController.CoaterState.Empty)
                {
                    currentStep = 15;
                }
                break;

            case 13:
                SetText("Close the Spin Coater menu");
                if (CleanroomUIManager.Instance != null && !CleanroomUIManager.Instance.IsAnyPanelOpen())
                {
                    currentStep = 14;
                }
                if (spinCoater != null && spinCoater.currentCoaterState == SpinCoaterController.CoaterState.Empty)
                {
                    currentStep = 15;
                }
                break;

            case 14:
                SetText("Remove the wafer from the Spin Coater");
                if (spinCoater != null && spinCoater.currentCoaterState == SpinCoaterController.CoaterState.Empty)
                {
                    if (GetProcessingHotplate() != null) GetProcessingHotplate().currentState = HotplateController.HotplateState.Idle;
                    currentStep = 15;
                }
                break;

            // HOTPLATE WORKFLOW

            case 15:
                SetText("Place the PR coated wafer on top of the Hotplate to your left");
                if (GetProcessingHotplate() != null && GetProcessingHotplate().currentState == HotplateController.HotplateState.WaferLoaded)
                {
                    currentStep = 16;
                }
                break;

            case 16:
                SetText("Click on the highlighted region on the hotplate's knob to bring up its menu");
                if (CleanroomUIManager.Instance != null && CleanroomUIManager.Instance.hotplateUIPanel != null)
                {
                    if (CleanroomUIManager.Instance.hotplateUIPanel.activeSelf)
                    {
                        currentStep = 17;
                    }
                }
                break;

            case 17:
                SetText("Adjust the hotplate's set temperature and time if required, then start PREBAKE");
                if (GetProcessingHotplate() != null && GetProcessingHotplate().currentState == HotplateController.HotplateState.Baking)
                {
                    currentStep = 18;
                }
                break;

            case 18:
                SetText("Observe metrics displayed on the ellipsometry visual at the top right of the menu");
                if (GetProcessingHotplate() != null && GetProcessingHotplate().currentState == HotplateController.HotplateState.Complete)
                {
                    stepTimer = 4f; 
                    currentStep = 19;
                }
                break;

            case 19:
                // Non-blocking timer: Shows metrics text briefly, then asks to close menu
                if (stepTimer > 0f)
                {
                    stepTimer -= Time.deltaTime;
                    SetText("Observe metrics displayed on the ellipsometry visual at the top right of the menu");
                }
                else
                {
                    SetText("Close the Hotplate menu");
                }

                if (CleanroomUIManager.Instance != null && !CleanroomUIManager.Instance.IsAnyPanelOpen())
                {
                    currentStep = 20;
                }

                // Instantly advance if they physically grab the wafer off the hotplate
                if (IsHoldingWaferOrSubstrate())
                {
                    if (GetProcessingHotplate() != null) GetProcessingHotplate().currentState = HotplateController.HotplateState.Idle; // Fallback safety
                    currentStep = 21;
                }
                break;

            case 20:
                SetText("Take the wafer from the hotplate");
                if (IsHoldingWaferOrSubstrate())
                {
                    if (GetProcessingHotplate() != null) GetProcessingHotplate().currentState = HotplateController.HotplateState.Idle;
                    currentStep = 21;
                }
                break;

            case 21:
                SetText("Click on the wafer carrier on your right (its top lid) to place the wafer (Blue region)");
                // When placed, heldObject becomes null
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    currentStep = 22;
                }
                break;

            case 22:
                SetText("Click the Wafer carrier to pick it up securely");
                if (playerInteraction != null && playerInteraction.heldObject != null)
                {
                    string objName = playerInteraction.heldObject.name.ToLower();
                    if (objName.Contains("carrier"))
                    {
                        currentStep = 23;
                    }
                }
                break;

            case 23:
                SetText("Carry it over to the next room and place the Wafer carrier down onto the highlighted spot");
                
                // Advance when the carrier is successfully placed on dock2 (heldObject becomes null)
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    // ANTI-EXPLOIT: Ensure it was placed on dock 2, not dropped on the floor or placed back on dock 1!
                    if (playerInteraction.lastDockPlacedName.ToLower().Contains("dock2"))
                    {
                        currentStep = 24;
                    }
                }
                break;

            case 24:
                SetText("<color=#008800><b>Wafer Transport Complete!</b></color>");
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f)
                {
                    currentStep = 25;
                }
                // FAST PLAY: Jump ahead if they immediately interact with the photomask tray
                if (PhotomaskGalleryUI.Instance != null && PhotomaskGalleryUI.Instance.isGalleryOpen)
                {
                    currentStep = 26;
                }
                break;

            case 25:
                SetText("Click on the Photomask tray to the left (the first item)");
                if (PhotomaskGalleryUI.Instance != null && PhotomaskGalleryUI.Instance.isGalleryOpen)
                {
                    currentStep = 26;
                }
                break;

            case 26:
                SetText("Adjust the critical dimension if required and click the preferred pattern");
                if (playerInteraction != null && playerInteraction.heldObject != null && playerInteraction.heldObject.name.ToLower().Contains("mask"))
                {
                    currentStep = 27;
                }
                break;

            case 27:
                SetText("Place the photomask EXACTLY on the Chuck of Mask Tray to the right");
                
                if (!hasShownChuckContext)
                {
                    hasShownChuckContext = true;
                    TriggerContextImage("Images/MAcontextImg");
                }

                if (exposureStation != null)
                {
                    Transform chuck = exposureStation.transform.Find("Hinge/Rotator/Chuck/Aligner_Wafer_Anchor");
                    if (chuck == null) chuck = exposureStation.waferDockPosition;

                    if (chuck != null)
                    {
                        bool maskFound = false;
                        foreach (Transform child in chuck)
                        {
                            if (child.name.ToLower().Contains("mask"))
                            {
                                maskFound = true;
                                break;
                            }
                        }
                        if (maskFound) currentStep = 28;
                    }
                }
                break;

            case 28:
                SetText("Click on the highlighted region to the RIGHT to open the Aligner's Hardware controls");
                bool isExposureOpen = false;
                if (CleanroomUIManager.Instance != null)
                {
                    if (CleanroomUIManager.Instance.oldExposureUIPanel != null && CleanroomUIManager.Instance.oldExposureUIPanel.activeSelf) isExposureOpen = true;
                    if (CleanroomUIManager.Instance.exposureUIController != null && CleanroomUIManager.Instance.exposureUIController.IsPanelCurrentlyOpen()) isExposureOpen = true;
                }
                if (isExposureOpen) currentStep = 29;
                break;

            case 29:
                SetText("Toggle the mask tray inside");
                if (exposureStation != null && exposureStation.isMaskTrayInserted)
                {
                    currentStep = 30;
                }
                break;

            case 30:
                SetText("Close this menu");
                if (CleanroomUIManager.Instance != null && !CleanroomUIManager.Instance.IsAnyPanelOpen())
                {
                    currentStep = 31;
                }
                break;

            case 31:
                SetText("Click on the highlighted region on the monitor to open the control panel");
                if (CleanroomUIManager.Instance != null && CleanroomUIManager.Instance.exposureUIController != null)
                {
                    if (CleanroomUIManager.Instance.exposureUIController.IsPanelCurrentlyOpen()) currentStep = 32;
                }
                break;

            case 32:
                SetText("Click on 'Open Recipe'");
                if (mainWindowController != null && (mainWindowController.IsOnRecipeScreen || mainWindowController.IsOnMachineScreen))
                {
                    currentStep = 33;
                }
                break;

            case 33:
                SetText("Adjust the dose, defocus and NA if required, then click on Run");
                if (mainWindowController != null && mainWindowController.IsOnMachineScreen)
                {
                    currentStep = 34;
                }
                break;

            case 34:
                SetText("Click continue");
                if (mainWindowController != null && mainWindowController.currentMachineState == MainWindowController.MachineState.Step12_InsertSubstrate)
                {
                    currentStep = 35;
                }
                break;

            case 35:
                SetText("Close the menu by clicking X on the top right corner");
                if (CleanroomUIManager.Instance != null && !CleanroomUIManager.Instance.IsAnyPanelOpen())
                {
                    currentStep = 36;
                }
                break;

            case 36:
                SetText("Open the Hardware Controls UI below");
                if (CleanroomUIManager.Instance != null && CleanroomUIManager.Instance.oldExposureUIPanel != null && CleanroomUIManager.Instance.oldExposureUIPanel.activeSelf)
                {
                    currentStep = 37;
                }
                break;

            case 37:
                SetText("Toggle the mask tray out");
                if (exposureStation != null && !exposureStation.isMaskTrayInserted)
                {
                    currentStep = 38;
                }
                break;

            case 38:
                SetText("Close this menu");
                if (CleanroomUIManager.Instance != null && !CleanroomUIManager.Instance.IsAnyPanelOpen())
                {
                    currentStep = 39;
                }
                break;

            case 39:
                SetText("Click the area AROUND the Chuck to get top lid");
                if (playerInteraction != null && playerInteraction.heldObject != null && playerInteraction.heldObject.name.ToLower().Contains("toplid"))
                {
                    currentStep = 40;
                }
                break;

            case 40:
                SetText("Place the top lid on the highlighted region of the table to the very left");
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    currentStep = 41;
                }
                break;

            case 41:
                SetText("Click the wafer carrier to get the wafer");
                if (IsHoldingWaferOrSubstrate())
                {
                    currentStep = 42;
                }
                // Fast-forward if they already placed the wafer on the chuck
                if (exposureStation != null)
                {
                    bool hasWafer = false;
                    if (exposureStation.waferDockPosition != null)
                    {
                        foreach (Transform t in exposureStation.waferDockPosition) if (t.gameObject.activeInHierarchy && IsWaferOrSubstrateName(t.name.ToLower())) hasWafer = true;
                    }
                    if (!hasWafer)
                    {
                        Transform chuck = exposureStation.transform.Find("Hinge/Rotator/Chuck/Aligner_Wafer_Anchor");
                        if (chuck != null) foreach (Transform t in chuck) if (t.gameObject.activeInHierarchy && IsWaferOrSubstrateName(t.name.ToLower())) hasWafer = true;
                    }
                    if (hasWafer) currentStep = 43;
                }
                break;

            case 42:
                SetText("Place the wafer on the Chuck of the Mask Aligner machine");
                if (exposureStation != null)
                {
                    bool hasWafer = false;
                    if (exposureStation.waferDockPosition != null)
                    {
                        foreach (Transform t in exposureStation.waferDockPosition) if (t.gameObject.activeInHierarchy && IsWaferOrSubstrateName(t.name.ToLower())) hasWafer = true;
                    }
                    if (!hasWafer)
                    {
                        Transform chuck = exposureStation.transform.Find("Hinge/Rotator/Chuck/Aligner_Wafer_Anchor");
                        if (chuck != null) foreach (Transform t in chuck) if (t.gameObject.activeInHierarchy && IsWaferOrSubstrateName(t.name.ToLower())) hasWafer = true;
                    }
                    if (hasWafer) currentStep = 43;
                }
                break;

            case 43:
                SetText("Toggle the mask tray in");
                if (exposureStation != null && exposureStation.isMaskTrayInserted)
                {
                    currentStep = 44;
                }
                break;

            case 44:
                SetText("Close this menu");
                if (CleanroomUIManager.Instance != null && !CleanroomUIManager.Instance.IsAnyPanelOpen())
                {
                    currentStep = 45;
                }
                if (CleanroomUIManager.Instance != null && CleanroomUIManager.Instance.exposureUIController != null && CleanroomUIManager.Instance.exposureUIController.IsPanelCurrentlyOpen())
                {
                    currentStep = 45;
                }
                break;

            case 45:
                SetText("Go back to the menu on the monitor");
                if (CleanroomUIManager.Instance != null && CleanroomUIManager.Instance.exposureUIController != null && CleanroomUIManager.Instance.exposureUIController.IsPanelCurrentlyOpen())
                {
                    currentStep = 46;
                }
                break;

            case 46:
                SetText("Click continue until the process finishes completely");
                if (mainWindowController != null && mainWindowController.currentMachineState == MainWindowController.MachineState.Step14_EndOfProcess)
                {
                    currentStep = 47;
                }
                break;

            case 47:
                SetText("Toggle the mask tray out");
                if (exposureStation != null && !exposureStation.isMaskTrayInserted)
                {
                    currentStep = 48;
                }
                break;

            case 48:
                SetText("Close this menu");
                if (CleanroomUIManager.Instance != null && !CleanroomUIManager.Instance.IsAnyPanelOpen())
                {
                    currentStep = 49;
                }
                break;

            case 49:
                SetText("Take the wafer from the mask tray");
                if (IsHoldingWaferOrSubstrate())
                {
                    currentStep = 50;
                }
                break;

            case 50:
                SetText("Place the wafer onto the wafer carrier (Click its top lid)");
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    currentStep = 51;
                }
                break;

            case 51:
                SetText("<color=#008800><b>Lithography Complete!</b></color>");
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f)
                {
                    currentStep = 52;
                }
                // FAST PLAY: Jump ahead if they immediately pick up the carrier
                if (playerInteraction != null && playerInteraction.heldObject != null && playerInteraction.heldObject.name.ToLower().Contains("carrier"))
                {
                    currentStep = 53;
                }
                break;

            case 52:
                SetText("Bring the wafer carrier to the previous station in the other room (Blue region)");
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    // Ensure they placed it back on dock 1 specifically
                    if (playerInteraction.lastDockPlacedName.ToLower().Contains("dock1"))
                    {
                        currentStep = 53;
                    }
                }
                break;

            case 53:
                SetText("Click on the wafer carrier to obtain the wafer");
                if (IsHoldingWaferOrSubstrate())
                {
                    currentStep = 54;
                }
                break;

            case 54:
                SetText("Click on top of the hotplate to place it");
                if (GetProcessingHotplate() != null && GetProcessingHotplate().currentState == HotplateController.HotplateState.WaferLoaded)
                {
                    currentStep = 55;
                }
                break;

            case 55:
                SetText("Click on the highlighted knob to bring up its menu");
                if (CleanroomUIManager.Instance != null && CleanroomUIManager.Instance.hotplateUIPanel != null && CleanroomUIManager.Instance.hotplateUIPanel.activeSelf)
                {
                    currentStep = 56;
                }
                break;

            case 56:
                SetText("Click on start POSTBAKE and observe the post exposure bake results");
                PostBakeButtonTrigger pbTrigger = Object.FindAnyObjectByType<PostBakeButtonTrigger>();
                if (pbTrigger != null && pbTrigger.postBakeStarted)
                {
                    currentStep = 57;
                }
                break;

            case 57:
                SetText("Observing post exposure bake results...");
                // Non-blocking timer: Shows metrics text while baking completes
                PostBakeButtonTrigger pbTriggerComplete = Object.FindAnyObjectByType<PostBakeButtonTrigger>();
                if (pbTriggerComplete != null && pbTriggerComplete.postBakeComplete)
                {
                    stepTimer = 4f;
                    currentStep = 58;
                }
                break;

            case 58:
                if (stepTimer > 0f)
                {
                    stepTimer -= Time.deltaTime;
                    SetText("Observing post exposure bake results...");
                }
                else
                {
                    SetText("Close the menu");
                }

                // FAST PLAY: As soon as they close the panel, proceed instantly rather than making them wait for the timer
                if (CleanroomUIManager.Instance != null && !CleanroomUIManager.Instance.IsAnyPanelOpen())
                {
                    currentStep = 59;
                }
                break;

            case 59:
                SetText("Click on the wafer to pick it up");
                if (IsHoldingWaferOrSubstrate())
                {
                    currentStep = 60;
                }
                break;

            case 60:
                SetText("Place it in the wafer carrier (Click its toplid)");
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    currentStep = 61;
                }
                break;

            case 61:
                SetText("<color=#008800><b>Post-Exposure Bake Complete!</b></color>");
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f)
                {
                    currentStep = 62;
                }
                // FAST PLAY: Jump ahead if they immediately pick up the carrier
                if (playerInteraction != null && playerInteraction.heldObject != null && playerInteraction.heldObject.name.ToLower().Contains("carrier"))
                {
                    currentStep = 63;
                }
                break;

            case 62:
                SetText("Click the wafer carrier to pick it up");
                if (playerInteraction != null && playerInteraction.heldObject != null && playerInteraction.heldObject.name.ToLower().Contains("carrier"))
                {
                    currentStep = 63;
                }
                break;

            case 63:
                SetText("Bring the wafer carrier to the developer station just opposite to this station");
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    // Strict check: Must be placed on dock 3 (Developer Station), NOT put back on dock 1!
                    if (playerInteraction.lastDockPlacedName.ToLower().Contains("dock3"))
                    {
                        if (developerUI != null && developerUI.diFluid != null)
                        {
                            developerUI.diFluid.hasPoured = true;
                        }
                        currentStep = 64; // Proceed to pour developer solution; DI water pouring (steps 67-69) is omitted
                    }
                }
                break;

            case 64:
                SetText("Click on the developer solution bottle on the far right (3nd bottle) to hold it");
                if (playerInteraction != null && playerInteraction.heldObject != null)
                {
                    string objName = playerInteraction.heldObject.name.ToLower();
                    if (objName.Contains("bottle") && (objName.Contains("dev") || objName.Contains("developer")))
                    {
                        currentStep = 65;
                    }
                }
                break;

            case 65:
                SetText("Click on the RIGHT beaker to pour the solution");
                if (developerUI != null && developerUI.developerFluid != null && developerUI.developerFluid.hasPoured)
                {
                    currentStep = 66;
                }
                break;

            case 66:
                SetText("Place the bottle back");
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    if (developerUI != null && developerUI.diFluid != null) developerUI.diFluid.hasPoured = true;
                    currentStep = 70; // Skip DI bottle pouring steps 67-69
                }
                break;

            case 67:
            case 68:
            case 69:
                if (developerUI != null && developerUI.diFluid != null)
                {
                    developerUI.diFluid.hasPoured = true;
                }
                currentStep = 70;
                break;

            case 70:
                SetText("Click on the wafer carrier to obtain wafer");
                if (IsHoldingWaferOrSubstrate())
                {
                    currentStep = 71;
                }
                break;

            case 71:
                SetText("Click the right beaker to immerse the wafer in developer solution");
                // ENFORCEMENT: Make sure BOTH beakers are filled before allowing immersion!
                if (developerUI != null && (developerUI.developerFluid == null || !developerUI.developerFluid.hasPoured || developerUI.diFluid == null || !developerUI.diFluid.hasPoured))
                {
                    break;
                }
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    bool isImmersed = false;
                    if (developerUI != null && developerUI.developerFluid != null)
                    {
                        foreach (Transform child in developerUI.developerFluid.transform)
                        {
                            if (IsWaferOrSubstrateName(child.name.ToLower()))
                            {
                                isImmersed = true;
                                break;
                            }
                        }
                    }
                    if (isImmersed)
                    {
                        currentStep = 72;
                    }
                }
                break;

            case 72:
                SetText("Click on the highlighted region on the right for controls");
                if (developerUI != null && (developerUI.developerFluid == null || !developerUI.developerFluid.hasPoured || developerUI.diFluid == null || !developerUI.diFluid.hasPoured))
                {
                    currentStep = 71; // Fallback if reached without filling both beakers
                    break;
                }
                if (developerUI != null && developerUI.gameObject.activeInHierarchy)
                {
                    currentStep = 73;
                }
                break;

            case 73:
                SetText("Adjust the immersion/sweving time if required, and press swerve developer beaker");
                if (developerUI != null && developerUI.hasStartedSwerving)
                {
                    currentStep = 74;
                }
                break;

            case 74:
                // Non-blocking wait for swerving to finish
                if (developerUI != null && !developerUI.isCountingDown && developerUI.hasStartedSwerving)
                {
                    stepTimer = 4f;
                    currentStep = 75;
                }
                break;

            case 75:
                if (stepTimer > 0f)
                {
                    stepTimer -= Time.deltaTime;
                    SetText("Observe final developed pattern and metrics when complete");
                }
                else
                {
                    SetText("Close the menu");
                }

                // FAST PLAY: Instantly advance if they manually close the UI
                if (developerUI != null && !developerUI.gameObject.activeInHierarchy)
                {
                    currentStep = 76;
                }
                break;

            case 76:
                SetText("Click on the wafer in the developer beaker to pick it up");
                if (IsHoldingWaferOrSubstrate())
                {
                    currentStep = 77;
                }
                break;

            case 77:
                SetText("Click the left beaker to immerse the wafer in DI water");
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    bool isImmersed = false;
                    if (developerUI != null && developerUI.diFluid != null)
                    {
                        foreach (Transform child in developerUI.diFluid.transform)
                        {
                            if (IsWaferOrSubstrateName(child.name.ToLower()))
                            {
                                isImmersed = true;
                                break;
                            }
                        }
                    }
                    if (isImmersed)
                    {
                        currentStep = 78;
                    }
                }
                break;

            case 78:
                SetText("Click on the highlighted region on the right for controls again");
                if (developerUI != null && developerUI.gameObject.activeInHierarchy)
                {
                    currentStep = 79;
                }
                break;

            case 79:
                SetText("Adjust the immersion/sweving time if required, and press swerve DI beaker");
                if (developerUI != null && developerUI.hasStartedSwervingDI)
                {
                    currentStep = 80;
                }
                break;

            case 80:
                if (developerUI != null && !developerUI.isCountingDown && developerUI.hasStartedSwervingDI)
                {
                    stepTimer = 10f;
                    currentStep = 81;
                }
                break;

            case 81:
                if (stepTimer > 0f)
                {
                    stepTimer -= Time.deltaTime;
                    SetText("<color=#008800><b>Photolithography complete, observe final pattern results!</b></color>");
                }
                else
                {
                    SetText("<color=#008800><b>Refresh the page to start the sequence all over again!</b></color>");
                }
                break;

            // PHASE 1: SC1 CLEANING & DEHYDRATION BAKE WORKFLOW
            case 100:
                SetText("Approach the wet bench infront, and click on the leftmost bottle (SC-1)");
                if (playerInteraction != null && playerInteraction.heldObject != null)
                {
                    string n = playerInteraction.heldObject.name.ToLower();
                    if (n.Contains("sc") || n.Contains("clean") || n.Contains("rca") || (n.Contains("bottle") && !n.Contains("di")))
                    {
                        currentStep = 101;
                    }
                }
                break;

            case 101:
                SetText("Click the beaker placed on top of the Hotplate to pour the solution");
                if (sc1Controller == null) sc1Controller = Object.FindAnyObjectByType<SC1Controller>();
                if (sc1Controller != null && sc1Controller.cleaningBeakerFluid != null && sc1Controller.cleaningBeakerFluid.hasPoured)
                {
                    currentStep = 102;
                }
                break;

            case 102:
                SetText("Place the bottle back");
                if (playerInteraction != null && (playerInteraction.heldObject == null || (CleanroomUIManager.Instance != null && CleanroomUIManager.Instance.sc1CleanerUIPanel != null && CleanroomUIManager.Instance.sc1CleanerUIPanel.activeSelf)))
                {
                    currentStep = 103;
                }
                break;

            case 103:
                SetText("Click on the yellow highlighted portion on the cleaner hotplate to open its controls");
                if (CleanroomUIManager.Instance != null && CleanroomUIManager.Instance.sc1CleanerUIPanel != null && CleanroomUIManager.Instance.sc1CleanerUIPanel.activeSelf)
                {
                    currentStep = 104;
                }
                break;

            case 104:
                SetText("Start the cleaning process by clicking 'Start Clean'");
                if (sc1Controller == null) sc1Controller = Object.FindAnyObjectByType<SC1Controller>();
                if (sc1Controller != null && sc1Controller.isComplete)
                {
                    currentStep = 105;
                }
                break;

            case 105:
                SetText("Close the cleaning menu");
                if (CleanroomUIManager.Instance != null && !CleanroomUIManager.Instance.IsAnyPanelOpen())
                {
                    currentStep = 106;
                }
                break;

            case 106:
                SetText("Click on the distilled water bottle (2nd bottle) to hold it");
                if (playerInteraction != null && playerInteraction.heldObject != null)
                {
                    string n = playerInteraction.heldObject.name.ToLower();
                    if (n.Contains("di") || n.Contains("water") || n.Contains("rinse") || n.Contains("2") || (n.Contains("bottle") && !n.Contains("sc") && !n.Contains("clean")))
                    {
                        currentStep = 107;
                    }
                }
                break;

            case 107:
                SetText("Click the beaker in front of that bottle to pour the distilled water");
                FluidInteractionController diBeaker = GetSC1DIFluid();
                if (diBeaker != null && diBeaker.hasPoured)
                {
                    currentStep = 108;
                }
                break;

            case 108:
                SetText("Place the bottle back");
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    if (ovenController == null) ovenController = Object.FindAnyObjectByType<DehydrationOvenController>();
                    if (ovenController != null && ovenController.hasWafer)
                    {
                        currentStep = 113;
                    }
                    else if (IsWaferInDIBeaker())
                    {
                        currentStep = 111;
                    }
                    else if (IsHoldingWaferOrSubstrate())
                    {
                        currentStep = 110;
                    }
                    else
                    {
                        currentStep = 109;
                    }
                }
                break;

            case 109:
                SetText("Click on the wafer to retrieve it");
                if (ovenController == null) ovenController = Object.FindAnyObjectByType<DehydrationOvenController>();
                if (ovenController != null && ovenController.hasWafer)
                {
                    currentStep = 113;
                }
                else if (IsWaferInDIBeaker())
                {
                    currentStep = 111;
                }
                else if (IsHoldingWaferOrSubstrate())
                {
                    currentStep = 110;
                }
                break;

            case 110:
                SetText("Place the wafer onto the beaker with the distilled water solution");
                if (ovenController == null) ovenController = Object.FindAnyObjectByType<DehydrationOvenController>();
                if (ovenController != null && ovenController.hasWafer)
                {
                    currentStep = 113;
                }
                else if (IsWaferInDIBeaker() || (playerInteraction != null && playerInteraction.heldObject == null))
                {
                    currentStep = 111;
                }
                break;

            case 111:
                SetText("Click on the wafer to retrieve it");
                if (ovenController == null) ovenController = Object.FindAnyObjectByType<DehydrationOvenController>();
                if (ovenController != null && ovenController.hasWafer)
                {
                    currentStep = 113;
                }
                else if (IsHoldingWaferOrSubstrate())
                {
                    currentStep = 112;
                }
                break;

            case 112:
                SetText("Bring the wafer to the oven just opposite to this station and click the oven to insert it");
                if (ovenController == null) ovenController = Object.FindAnyObjectByType<DehydrationOvenController>();
                if (ovenController != null && ovenController.hasWafer && (playerInteraction == null || playerInteraction.heldObject == null))
                {
                    currentStep = 113;
                }
                break;

            case 113:
                SetText("Click on the highlighted portion to the left of the oven to access its controls");
                if (CleanroomUIManager.Instance != null && CleanroomUIManager.Instance.dehydrationOvenUIPanel != null && CleanroomUIManager.Instance.dehydrationOvenUIPanel.activeSelf)
                {
                    currentStep = 114;
                }
                break;

            case 114:
                SetText("Adjust temperature and time if required, then click on 'Start Bake'");
                DehydrationOvenUIController ovenUI = Object.FindAnyObjectByType<DehydrationOvenUIController>();
                if (ovenUI != null && ovenUI.isComplete)
                {
                    currentStep = 115;
                }
                break;

            case 115:
                SetText("Close the dehydration oven menu");
                if (CleanroomUIManager.Instance != null && !CleanroomUIManager.Instance.IsAnyPanelOpen())
                {
                    currentStep = 116;
                }
                break;

            case 116:
                SetText("Click on the oven doors to open and retrieve the baked wafer");
                if (IsHoldingWaferOrSubstrate())
                {
                    currentStep = 117;
                }
                break;

            case 117:
                SetText("Place the wafer on the hotplate next to the oven");
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    if (developerUI != null && developerUI.diFluid != null) developerUI.diFluid.hasPoured = false;
                    currentStep = 2; // Proceed to Spincoater sequence!
                }
                break;
        }
    }
    
    // Call this from your "I understand" UI Button's OnClick event
    public void AcknowledgeContextImage()
    {
        if (contextImgObj != null)
        {
            contextImgObj.SetActive(false);
            
            if (contextAcknowledgeButton != null) 
            {
                contextAcknowledgeButton.gameObject.SetActive(false);
            }
            if (contextImgTitle != null)
            {
                contextImgTitle.SetActive(false);
            }
            
            // Re-lock the camera and hide the mouse so the player can return to normal gameplay
            MouseLook mouseLook = Object.FindAnyObjectByType<MouseLook>();
            if (mouseLook != null) mouseLook.ForceLockFromUI();
        }
    }

    private void TriggerContextImage(string resourcePath)
    {
        if (contextImgObj == null) return;
        
        Texture2D tex = Resources.Load<Texture2D>(resourcePath);
        if (tex != null)
        {
            UnityEngine.UI.RawImage raw = contextImgObj.GetComponent<UnityEngine.UI.RawImage>();
            if (raw != null) raw.texture = tex;
            else
            {
                UnityEngine.UI.Image img = contextImgObj.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                {
                    img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
            
            // Add a simple black border if missing
            UnityEngine.UI.Outline outline = contextImgObj.GetComponent<UnityEngine.UI.Outline>();
            if (outline == null)
            {
                outline = contextImgObj.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(4f, -4f);
            }
            
            contextImgObj.SetActive(true);
            
            if (contextAcknowledgeButton != null)
            {
                contextAcknowledgeButton.gameObject.SetActive(true);
            }
            if (contextImgTitle != null)
            {
                contextImgTitle.SetActive(true);
            }
            
            // Unlock the cursor so the player can physically click the button
            MouseLook mouseLook = Object.FindAnyObjectByType<MouseLook>();
            if (mouseLook != null) mouseLook.ForceUnlockForUI();
        }
        else
        {
            Debug.LogWarning($"[WorkflowManager] Could not find Context Image at Resources/{resourcePath}");
        }
    }

    private bool IsWaferOrSubstrateName(string lowerName)
    {
        if (string.IsNullOrEmpty(lowerName)) return false;
        return (lowerName.Contains("wafer") && !lowerName.Contains("carrier") && !lowerName.Contains("dock")) || lowerName.Contains("substrate");
    }

    private bool IsHoldingWaferOrSubstrate()
    {
        if (playerInteraction == null || playerInteraction.heldObject == null) return false;
        return IsWaferOrSubstrateName(playerInteraction.heldObject.name.ToLower());
    }

    private bool IsWaferInDIBeaker()
    {
        FluidInteractionController diBeaker = GetSC1DIFluid();
        if (diBeaker != null)
        {
            foreach (Transform child in diBeaker.GetComponentsInChildren<Transform>(true))
            {
                if (child != diBeaker.transform && IsWaferOrSubstrateName(child.name.ToLower()))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private FluidInteractionController GetSC1DIFluid()
    {
        if (developerUI != null && developerUI.diFluid != null) return developerUI.diFluid;

        foreach (FluidInteractionController fic in Object.FindObjectsByType<FluidInteractionController>(FindObjectsSortMode.None))
        {
            if (developerUI != null && fic == developerUI.developerFluid) continue;
            if (sc1Controller != null && fic == sc1Controller.cleaningBeakerFluid) continue;

            string fname = (fic.name + " " + fic.gameObject.name).ToLower();
            if (fname.Contains("di") || fname.Contains("water") || fname.Contains("rinse") || fname.Contains("2") || fname.Contains("beaker"))
            {
                if (!fname.Contains("dev") && !fname.Contains("sc1") && !fname.Contains("clean"))
                {
                    return fic;
                }
            }
        }
        return null;
    }

    private HotplateController GetProcessingHotplate()
    {
        if (hotplate != null && !HotplateController.IsCleanerHotplate(hotplate)) return hotplate;
        hotplate = HotplateController.GetProcessingHotplate();
        return hotplate;
    }
}
