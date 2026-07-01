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
    private float stepTimer = 0f;

    // UI Syncing
    private RectTransform textRect;
    private RectTransform bgRect;

    private void Start()
    {
        // Try to find the required managers/controllers automatically
        spinCoater = Object.FindAnyObjectByType<SpinCoaterController>();
        playerInteraction = Object.FindAnyObjectByType<PlayerInteraction>();
        hotplate = Object.FindAnyObjectByType<HotplateController>();
        developerUI = Object.FindAnyObjectByType<DeveloperUIManager>();
        exposureStation = Object.FindAnyObjectByType<ExposureStationController>();
        mainWindowController = Object.FindAnyObjectByType<MainWindowController>();
        
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
                SetText("Approach the spincoater machine, and click the highlighted portion on its right to open up its menu");
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
                SetText("Look over to the hotplate on the left and click the wafer to grab it");
                if (playerInteraction != null && playerInteraction.heldObject != null)
                {
                    string objName = playerInteraction.heldObject.name.ToLower();
                    if (objName.Contains("wafer") || objName.Contains("substrate"))
                    {
                        currentStep = 6;
                    }
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
                // Non-blocking timer: Shows metrics text, then transitions to remove wafer text
                if (stepTimer > 0f)
                {
                    stepTimer -= Time.deltaTime;
                    SetText("Once complete, Thickness value of the PR will be visible");
                }
                else
                {
                    SetText("Remove the wafer from the Spin Coater");
                }
                
                // Allow INSTANT skipping to the hotplate workflow if the user is fast
                if (spinCoater != null && spinCoater.currentCoaterState == SpinCoaterController.CoaterState.Empty)
                {
                    currentStep = 14; // Skipped case 13 as requested
                }
                break;

            case 13:
                // Removed Spin Coater Complete transition screen
                currentStep = 14; 
                break;

            // ========================= HOTPLATE WORKFLOW =========================

            case 14:
                SetText("Place the PR coated wafer on top of the Hotplate");
                if (hotplate != null && hotplate.currentState == HotplateController.HotplateState.WaferLoaded)
                {
                    currentStep = 15;
                }
                break;

            case 15:
                SetText("Click on the highlighted region on the hotplate's knob to bring up its menu");
                if (CleanroomUIManager.Instance != null && CleanroomUIManager.Instance.hotplateUIPanel != null)
                {
                    if (CleanroomUIManager.Instance.hotplateUIPanel.activeSelf)
                    {
                        currentStep = 16;
                    }
                }
                break;

            case 16:
                SetText("Adjust the hotplate's set temperature and time if required, then start PREBAKE");
                if (hotplate != null && hotplate.currentState == HotplateController.HotplateState.Baking)
                {
                    currentStep = 17;
                }
                break;

            case 17:
                SetText("Observe metrics displayed on the ellipsometry visual at the top right of the menu");
                if (hotplate != null && hotplate.currentState == HotplateController.HotplateState.Complete)
                {
                    stepTimer = 4f; 
                    currentStep = 18;
                }
                break;

            case 18:
                // Non-blocking timer: Shows metrics text, then transitions to take wafer text
                if (stepTimer > 0f)
                {
                    stepTimer -= Time.deltaTime;
                    SetText("Observe metrics displayed on the ellipsometry visual at the top right of the menu");
                }
                else
                {
                    SetText("Take the wafer from the hotplate");
                }

                // Instantly advance if they physically grab the wafer off the hotplate
                if (playerInteraction != null && playerInteraction.heldObject != null)
                {
                    string objName = playerInteraction.heldObject.name.ToLower();
                    if (objName.Contains("wafer") || objName.Contains("substrate"))
                    {
                        if (hotplate != null) hotplate.currentState = HotplateController.HotplateState.Idle; // Fallback safety
                        currentStep = 19;
                    }
                }
                break;

            case 19:
                SetText("Click on the wafer carrier (its top lid) to place the wafer");
                // When placed, heldObject becomes null
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    currentStep = 20;
                }
                break;

            case 20:
                SetText("Click the Wafer carrier to pick it up securely");
                if (playerInteraction != null && playerInteraction.heldObject != null)
                {
                    string objName = playerInteraction.heldObject.name.ToLower();
                    if (objName.Contains("carrier"))
                    {
                        currentStep = 21;
                    }
                }
                break;

            case 21:
                SetText("Carry it over to the next room and place the Wafer carrier down onto the highlighted spot");
                
                // Advance when the carrier is successfully placed on dock2 (heldObject becomes null)
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    // ANTI-EXPLOIT: Ensure it was placed on dock 2, not dropped on the floor or placed back on dock 1!
                    if (playerInteraction.lastDockPlacedName.ToLower().Contains("dock2"))
                    {
                        currentStep = 22;
                    }
                }
                break;

            case 22:
                SetText("<color=#008800><b>Wafer Transport Complete!</b></color>");
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f)
                {
                    currentStep = 23;
                }
                // FAST PLAY: Jump ahead if they immediately interact with the photomask tray
                if (PhotomaskGalleryUI.Instance != null && PhotomaskGalleryUI.Instance.isGalleryOpen)
                {
                    currentStep = 24;
                }
                break;

            case 23:
                SetText("Click on the Photomask tray to the left (the first item)");
                if (PhotomaskGalleryUI.Instance != null && PhotomaskGalleryUI.Instance.isGalleryOpen)
                {
                    currentStep = 24;
                }
                break;

            case 24:
                SetText("Adjust the critical dimension if required and click the preferred pattern");
                if (playerInteraction != null && playerInteraction.heldObject != null && playerInteraction.heldObject.name.ToLower().Contains("mask"))
                {
                    currentStep = 25;
                }
                break;

            case 25:
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
                        if (maskFound) currentStep = 26;
                    }
                }
                break;

            case 26:
                SetText("Click on the highlighted region to the RIGHT to open the Aligner's Hardware controls");
                bool isExposureOpen = false;
                if (CleanroomUIManager.Instance != null)
                {
                    if (CleanroomUIManager.Instance.oldExposureUIPanel != null && CleanroomUIManager.Instance.oldExposureUIPanel.activeSelf) isExposureOpen = true;
                    if (CleanroomUIManager.Instance.exposureUIController != null && CleanroomUIManager.Instance.exposureUIController.IsPanelCurrentlyOpen()) isExposureOpen = true;
                }
                if (isExposureOpen) currentStep = 27;
                break;

            case 27:
                SetText("Toggle the mask tray inside");
                if (exposureStation != null && exposureStation.isMaskTrayInserted)
                {
                    currentStep = 28;
                }
                break;

            case 28:
                SetText("Close this menu");
                if (CleanroomUIManager.Instance != null && !CleanroomUIManager.Instance.IsAnyPanelOpen())
                {
                    currentStep = 29;
                }
                break;

            case 29:
                SetText("Click on the highlighted region on the monitor to open the control panel");
                if (CleanroomUIManager.Instance != null && CleanroomUIManager.Instance.exposureUIController != null)
                {
                    if (CleanroomUIManager.Instance.exposureUIController.IsPanelCurrentlyOpen()) currentStep = 30;
                }
                break;

            case 30:
                SetText("Click on 'Open Recipe'");
                if (mainWindowController != null && (mainWindowController.IsOnRecipeScreen || mainWindowController.IsOnMachineScreen))
                {
                    currentStep = 31;
                }
                break;

            case 31:
                SetText("Adjust the dose, defocus and NA if required, then click on Run");
                if (mainWindowController != null && mainWindowController.IsOnMachineScreen)
                {
                    currentStep = 32;
                }
                break;

            case 32:
                SetText("Click continue");
                if (mainWindowController != null && mainWindowController.currentMachineState == MainWindowController.MachineState.Step12_InsertSubstrate)
                {
                    currentStep = 33;
                }
                break;

            case 33:
                SetText("Close the menu");
                if (CleanroomUIManager.Instance != null && !CleanroomUIManager.Instance.IsAnyPanelOpen())
                {
                    currentStep = 34;
                }
                break;

            case 34:
                SetText("Open the Hardware Controls UI below");
                if (CleanroomUIManager.Instance != null && CleanroomUIManager.Instance.oldExposureUIPanel != null && CleanroomUIManager.Instance.oldExposureUIPanel.activeSelf)
                {
                    currentStep = 35;
                }
                break;

            case 35:
                SetText("Toggle the mask tray out");
                if (exposureStation != null && !exposureStation.isMaskTrayInserted)
                {
                    currentStep = 36;
                }
                break;

            case 36:
                SetText("Click the mask tray to get top lid");
                if (playerInteraction != null && playerInteraction.heldObject != null && playerInteraction.heldObject.name.ToLower().Contains("toplid"))
                {
                    currentStep = 37;
                }
                break;

            case 37:
                SetText("Place the top lid on the highlighted region of the table to the very left");
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    currentStep = 38;
                }
                break;

            case 38:
                SetText("Click the wafer carrier to get the wafer");
                if (playerInteraction != null && playerInteraction.heldObject != null && (playerInteraction.heldObject.name.ToLower().Contains("wafer") || playerInteraction.heldObject.name.ToLower().Contains("substrate")))
                {
                    currentStep = 39;
                }
                break;

            case 39:
                SetText("Place the wafer on the Chuck of the Mask Aligner machine");
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    currentStep = 40;
                }
                break;

            case 40:
                SetText("Toggle the mask tray in");
                if (exposureStation != null && exposureStation.isMaskTrayInserted)
                {
                    currentStep = 41;
                }
                break;

            case 41:
                SetText("Go back to the menu on the monitor");
                if (CleanroomUIManager.Instance != null && CleanroomUIManager.Instance.exposureUIController != null && CleanroomUIManager.Instance.exposureUIController.IsPanelCurrentlyOpen())
                {
                    currentStep = 42;
                }
                break;

            case 42:
                SetText("Click continue until the process finishes completely");
                if (mainWindowController != null && mainWindowController.currentMachineState == MainWindowController.MachineState.Step14_EndOfProcess)
                {
                    currentStep = 43;
                }
                break;

            case 43:
                SetText("Toggle the mask tray out");
                if (exposureStation != null && !exposureStation.isMaskTrayInserted)
                {
                    // Adding a small step here to prevent instant-skipping of the "place on carrier" prompt
                    currentStep = 44;
                }
                break;

            case 44:
                SetText("Take the wafer from the mask tray");
                if (playerInteraction != null && playerInteraction.heldObject != null && (playerInteraction.heldObject.name.ToLower().Contains("wafer") || playerInteraction.heldObject.name.ToLower().Contains("substrate")))
                {
                    currentStep = 45;
                }
                break;

            case 45:
                SetText("Place the wafer onto the wafer carrier (Click its top lid)");
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    currentStep = 46;
                }
                break;

            case 46:
                SetText("<color=#008800><b>Lithography Complete!</b></color>");
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f)
                {
                    currentStep = 47;
                }
                // FAST PLAY: Jump ahead if they immediately pick up the carrier
                if (playerInteraction != null && playerInteraction.heldObject != null && playerInteraction.heldObject.name.ToLower().Contains("carrier"))
                {
                    currentStep = 48;
                }
                break;

            case 47:
                SetText("Bring the wafer carrier to the previous station in the other room");
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    // Ensure they placed it back on dock 1 specifically
                    if (playerInteraction.lastDockPlacedName.ToLower().Contains("dock1"))
                    {
                        currentStep = 48;
                    }
                }
                break;

            case 48:
                SetText("Click on the wafer carrier to obtain the wafer");
                if (playerInteraction != null && playerInteraction.heldObject != null && (playerInteraction.heldObject.name.ToLower().Contains("wafer") || playerInteraction.heldObject.name.ToLower().Contains("substrate")))
                {
                    currentStep = 49;
                }
                break;

            case 49:
                SetText("Click on the top of hotplate to place it");
                if (hotplate != null && hotplate.currentState == HotplateController.HotplateState.WaferLoaded)
                {
                    currentStep = 50;
                }
                break;

            case 50:
                SetText("Click on the highlighted knob to bring up its menu");
                if (CleanroomUIManager.Instance != null && CleanroomUIManager.Instance.hotplateUIPanel != null && CleanroomUIManager.Instance.hotplateUIPanel.activeSelf)
                {
                    currentStep = 51;
                }
                break;

            case 51:
                SetText("Click on start POSTBAKE and observe the post exposure bake results");
                PostBakeButtonTrigger pbTrigger = Object.FindAnyObjectByType<PostBakeButtonTrigger>();
                if (pbTrigger != null && pbTrigger.postBakeStarted)
                {
                    currentStep = 52;
                }
                break;

            case 52:
                // Non-blocking timer: Shows metrics text, then transitions to close menu
                PostBakeButtonTrigger pbTriggerComplete = Object.FindAnyObjectByType<PostBakeButtonTrigger>();
                if (pbTriggerComplete != null && pbTriggerComplete.postBakeComplete)
                {
                    stepTimer = 4f;
                    currentStep = 53;
                }
                break;

            case 53:
                if (stepTimer > 0f)
                {
                    stepTimer -= Time.deltaTime;
                    SetText("Click on start postbake and observe the post exposure bake results");
                }
                else
                {
                    SetText("Close the menu");
                }

                // FAST PLAY: As soon as they close the panel, proceed instantly rather than making them wait for the timer
                if (CleanroomUIManager.Instance != null && !CleanroomUIManager.Instance.IsAnyPanelOpen())
                {
                    currentStep = 54;
                }
                break;

            case 54:
                SetText("Click on the wafer to pick it up");
                if (playerInteraction != null && playerInteraction.heldObject != null && (playerInteraction.heldObject.name.ToLower().Contains("wafer") || playerInteraction.heldObject.name.ToLower().Contains("substrate")))
                {
                    currentStep = 55;
                }
                break;

            case 55:
                SetText("Place it in the wafer carrier (Click its toplid)");
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    currentStep = 56;
                }
                break;

            case 56:
                SetText("<color=#008800><b>Post-Exposure Bake Complete!</b></color>");
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f)
                {
                    currentStep = 57;
                }
                // FAST PLAY: Jump ahead if they immediately pick up the carrier
                if (playerInteraction != null && playerInteraction.heldObject != null && playerInteraction.heldObject.name.ToLower().Contains("carrier"))
                {
                    currentStep = 58;
                }
                break;

            case 57:
                SetText("Click the wafer carrier to pick it up");
                if (playerInteraction != null && playerInteraction.heldObject != null && playerInteraction.heldObject.name.ToLower().Contains("carrier"))
                {
                    currentStep = 58;
                }
                break;

            case 58:
                SetText("Bring the wafer carrier to the developer station just opposite to this station");
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    // Strict check: Must be placed on dock 3 (Developer Station), NOT put back on dock 1!
                    if (playerInteraction.lastDockPlacedName.ToLower().Contains("dock3"))
                    {
                        currentStep = 59;
                    }
                }
                break;

            case 59:
                SetText("Click on the developer solution bottle on the right (2nd bottle) to hold it");
                if (playerInteraction != null && playerInteraction.heldObject != null)
                {
                    string objName = playerInteraction.heldObject.name.ToLower();
                    if (objName.Contains("bottle") && (objName.Contains("dev") || objName.Contains("developer")))
                    {
                        currentStep = 60;
                    }
                }
                break;

            case 60:
                SetText("Click the right beaker to pour the solution");
                if (developerUI != null && developerUI.developerFluid != null && developerUI.developerFluid.hasPoured)
                {
                    currentStep = 61;
                }
                break;

            case 61:
                SetText("Place the bottle back");
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    currentStep = 62;
                }
                break;

            case 62:
                SetText("Click on the DI water bottle on the left to hold it");
                if (playerInteraction != null && playerInteraction.heldObject != null)
                {
                    string objName = playerInteraction.heldObject.name.ToLower();
                    if (objName.Contains("bottle") && objName.Contains("di"))
                    {
                        currentStep = 63;
                    }
                }
                break;

            case 63:
                SetText("Click on the left beaker to pour the solution");
                if (developerUI != null && developerUI.diFluid != null && developerUI.diFluid.hasPoured)
                {
                    currentStep = 64;
                }
                break;

            case 64:
                SetText("Place the bottle back");
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    currentStep = 65;
                }
                break;

            case 65:
                SetText("Click on the wafer carrier to obtain wafer");
                if (playerInteraction != null && playerInteraction.heldObject != null && (playerInteraction.heldObject.name.ToLower().Contains("wafer") || playerInteraction.heldObject.name.ToLower().Contains("substrate")))
                {
                    currentStep = 66;
                }
                break;

            case 66:
                SetText("Click the right beaker to immerse the wafer in developer solution");
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    bool isImmersed = false;
                    if (developerUI != null && developerUI.developerFluid != null)
                    {
                        foreach (Transform child in developerUI.developerFluid.transform)
                        {
                            if (child.name.ToLower().Contains("wafer") || child.name.ToLower().Contains("substrate"))
                            {
                                isImmersed = true;
                                break;
                            }
                        }
                    }
                    if (isImmersed)
                    {
                        currentStep = 67;
                    }
                }
                break;

            case 67:
                SetText("Click on the highlighted region on the right for controls");
                if (developerUI != null && developerUI.gameObject.activeInHierarchy)
                {
                    currentStep = 68;
                }
                break;

            case 68:
                SetText("Adjust the immersion/sweving time if required, and press swerve developer beaker");
                if (developerUI != null && developerUI.hasStartedSwerving)
                {
                    currentStep = 69;
                }
                break;

            case 69:
                // Non-blocking wait for swerving to finish
                if (developerUI != null && !developerUI.isCountingDown && developerUI.hasStartedSwerving)
                {
                    stepTimer = 4f;
                    currentStep = 70;
                }
                break;

            case 70:
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
                    currentStep = 71;
                }
                break;

            case 71:
                SetText("Click on the wafer in the developer beaker to pick it up");
                if (playerInteraction != null && playerInteraction.heldObject != null && (playerInteraction.heldObject.name.ToLower().Contains("wafer") || playerInteraction.heldObject.name.ToLower().Contains("substrate")))
                {
                    currentStep = 72;
                }
                break;

            case 72:
                SetText("Click the left beaker to immerse the wafer in DI water");
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    bool isImmersed = false;
                    if (developerUI != null && developerUI.diFluid != null)
                    {
                        foreach (Transform child in developerUI.diFluid.transform)
                        {
                            if (child.name.ToLower().Contains("wafer") || child.name.ToLower().Contains("substrate"))
                            {
                                isImmersed = true;
                                break;
                            }
                        }
                    }
                    if (isImmersed)
                    {
                        currentStep = 73;
                    }
                }
                break;

            case 73:
                SetText("Click on the highlighted region on the right for controls again");
                if (developerUI != null && developerUI.gameObject.activeInHierarchy)
                {
                    currentStep = 74;
                }
                break;

            case 74:
                SetText("Adjust the immersion/sweving time if required, and press swerve DI beaker");
                if (developerUI != null && developerUI.hasStartedSwervingDI)
                {
                    currentStep = 75;
                }
                break;

            case 75:
                if (developerUI != null && !developerUI.isCountingDown && developerUI.hasStartedSwervingDI)
                {
                    stepTimer = 2f;
                    currentStep = 76;
                }
                break;

            case 76:
                if (stepTimer > 0f)
                {
                    stepTimer -= Time.deltaTime;
                }
                else
                {
                    SetText("Close the menu");
                }

                // FAST PLAY: Instantly advance if they manually close the UI
                if (developerUI != null && !developerUI.gameObject.activeInHierarchy)
                {
                    currentStep = 77;
                }
                break;

            case 77:
                SetText("<color=#008800><b>Photolithography complete, observe pattern on top of the wafer!</b></color>");
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
}
