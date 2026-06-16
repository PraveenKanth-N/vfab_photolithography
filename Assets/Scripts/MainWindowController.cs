using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MainWindowController : MonoBehaviour
{
    [Header("UI References")]
    public UIDocument targetUIDocument;

    private VisualElement screen1;
    private VisualElement screen2;
    private VisualElement screen3;

    // We track whether the UI logic is supposed to be active or not
    public bool isUiActivelyVisible = false;

    public enum MachineState
    {
        Step3_InsertMask,
        Step4_ConfigureOptics,
        Step5_InsertUVFilter,
        Step6_InsertMaskholder,
        Step7_FixMaskholder,
        Step8_InsertChuck,
        Step9_InsertLoadframe,
        Step10_FinalDualScreen,
        Step11_AdjustMicroscope,
        Step12_InsertSubstrate,
        Step13_FinalAlignment
    }

    private MachineState currentMachineState = MachineState.Step3_InsertMask;

    private void Start()
    {
        InitializeUI();

        // Start hidden
        isUiActivelyVisible = false;
        if (targetUIDocument != null)
        {
            if (targetUIDocument.rootVisualElement != null)
                targetUIDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }

    public void InitializeUI()
    {
        if (targetUIDocument == null)
        {
            targetUIDocument = Object.FindObjectOfType<UIDocument>(true);
        }

        if (targetUIDocument == null)
        {
            Debug.LogError("No UIDocument assigned or found on MainWindowController!");
            return;
        }

        // We MUST wake it up to grab the root visual element properly!
        if (!targetUIDocument.gameObject.activeSelf)
        {
            targetUIDocument.gameObject.SetActive(true);
        }

        var mainRoot = targetUIDocument.rootVisualElement;
        if (mainRoot == null)
        {
            Debug.LogWarning("UIDocument rootVisualElement is null! Ensure the UI object has a source asset.");
            return;
        }

        screen1 = mainRoot.Q<VisualElement>("screen1");
        screen2 = mainRoot.Q<VisualElement>("screen2");
        screen3 = mainRoot.Q<VisualElement>("screen3");

        var newRecipeBtn = mainRoot.Q<Button>("newRecipe");
        if (newRecipeBtn != null) newRecipeBtn.clicked += OpenRecipe;

        var runBtn = mainRoot.Q<Button>("runButton");
        if (runBtn != null) runBtn.clicked += OpenMachine;

        // Hook up the new Continue button on screen 3 to advance state!
        var continueBtn = mainRoot.Q<Button>("continueButton");
        if (continueBtn != null)
        {
            continueBtn.clicked -= AdvanceMachineState;
            continueBtn.clicked += AdvanceMachineState;
        }

        // =========================================================
        // DEV CONTROLS WIRING
        // =========================================================
        var devToggleTrayBtn = mainRoot.Q<Button>("devToggleTrayBtn");
        var devToggleVacuumBtn = mainRoot.Q<Button>("devToggleVacuumBtn");
        var devExecuteExposureBtn = mainRoot.Q<Button>("devExecuteExposureBtn");
        
        var exposureController = Object.FindAnyObjectByType<ExposureStationController>();
        if (exposureController != null)
        {
            if (devToggleTrayBtn != null) 
            { 
                devToggleTrayBtn.clicked -= exposureController.maskTrayToggleButton.onClick.Invoke; 
                devToggleTrayBtn.clicked += exposureController.maskTrayToggleButton.onClick.Invoke; 
            }
            if (devToggleVacuumBtn != null) 
            { 
                devToggleVacuumBtn.clicked -= exposureController.vacuumButton.onClick.Invoke; 
                devToggleVacuumBtn.clicked += exposureController.vacuumButton.onClick.Invoke; 
            }
            if (devExecuteExposureBtn != null) 
            { 
                devExecuteExposureBtn.clicked -= exposureController.executeExposureButton.onClick.Invoke; 
                devExecuteExposureBtn.clicked += exposureController.executeExposureButton.onClick.Invoke; 
            }
        }

        // Hook up the new EXIT button on screen 3 bottom bar
        var exitBtn = mainRoot.Q<Button>("exitButton");
        if (exitBtn != null)
        {
            exitBtn.clicked -= CloseEntireInterface;
            exitBtn.clicked += CloseEntireInterface;
        }

        // Hook up your UXML exit button cleanly
        var closeBtn = mainRoot.Q<Button>("CloseBtn");
        if (closeBtn != null)
        {
            // Remove previous listeners just in case
            closeBtn.clicked -= CloseEntireInterface;
            closeBtn.clicked += CloseEntireInterface;
        }

        var closeMaskPopupBtn = mainRoot.Q<Button>("closeMaskPopupBtn");
        if (closeMaskPopupBtn != null)
        {
            closeMaskPopupBtn.clicked -= HideMaskPopup;
            closeMaskPopupBtn.clicked += HideMaskPopup;
        }
    }

    private void HideMaskPopup()
    {
        if (targetUIDocument == null) return;
        var popup = targetUIDocument.rootVisualElement.Q<VisualElement>("maskPopupOverlay");
        if (popup != null) popup.style.display = DisplayStyle.None;
    }

    private bool hasInitializedBefore = false;

    public void OpenInterface()
    {
        Debug.Log("Opening UI");
        isUiActivelyVisible = true;
        InitializeUI();

        if (targetUIDocument != null && targetUIDocument.rootVisualElement != null)
        {
            targetUIDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        }

        // CleanroomUIManager already handles unlocking the camera when opening panels, 
        // but as a fallback if you run this script isolated:
        if (CleanroomUIManager.Instance == null)
        {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }

        if (!hasInitializedBefore)
        {
            Show(screen1);
            hasInitializedBefore = true;
        }
        else
        {
            UpdateScreen3Visuals();
        }
    }

    private void OpenRecipe()
    {
        Debug.Log("Opening Recipe UI (Screen 2)");
        Show(screen2);
    }

    private void OpenMachine()
    {
        Debug.Log("Opening Machine UI (Screen 3)");
        ResetMachineState();
        Show(screen3);
    }

    private void AdvanceMachineState()
    {
        if (currentMachineState == MachineState.Step10_FinalDualScreen)
        {
            var exposureController = Object.FindAnyObjectByType<ExposureStationController>();
            bool hasMask = false;
            
            if (exposureController != null)
            {
                if (exposureController.topDeckAnchor != null)
                {
                    foreach (Transform t in exposureController.topDeckAnchor) if (t.name.ToLower().Contains("mask")) hasMask = true;
                }
                if (!hasMask)
                {
                    Transform waferAnchor = exposureController.transform.Find("Hinge/Rotator/Chuck/Aligner_Wafer_Anchor");
                    if (waferAnchor != null) foreach (Transform t in waferAnchor) if (t.name.ToLower().Contains("mask")) hasMask = true;
                }
            }

            if (!hasMask || exposureController == null || !exposureController.isMaskTrayInserted)
            {
                // Show popup!
                if (targetUIDocument != null)
                {
                    var popup = targetUIDocument.rootVisualElement.Q<VisualElement>("maskPopupOverlay");
                    if (popup != null) 
                    {
                        popup.style.display = DisplayStyle.Flex;
                        popup.BringToFront();
                    }
                }
                return; // Do not advance!
            }
        }
        else if (currentMachineState == MachineState.Step11_AdjustMicroscope)
        {
            currentMachineState++;
            UpdateScreen3Visuals();
            return;
        }
        else if (currentMachineState == MachineState.Step12_InsertSubstrate)
        {
            var exposureController = Object.FindAnyObjectByType<ExposureStationController>();
            bool hasWafer = false;
            
            if (exposureController != null)
            {
                if (exposureController.waferDockPosition != null)
                {
                    foreach (Transform t in exposureController.waferDockPosition) if (t.name.ToLower().Contains("wafer")) hasWafer = true;
                }
                if (!hasWafer)
                {
                    Transform chuck = exposureController.transform.Find("Hinge/Rotator/Chuck/Aligner_Wafer_Anchor");
                    if (chuck != null) foreach (Transform t in chuck) if (t.name.ToLower().Contains("wafer")) hasWafer = true;
                }
            }

            if (!hasWafer)
            {
                if (targetUIDocument != null)
                {
                    var popup = targetUIDocument.rootVisualElement.Q<VisualElement>("maskPopupOverlay");
                    if (popup != null) 
                    {
                        var popupLabel = popup.Q<Label>("maskPopupLabel");
                        if (popupLabel != null) popupLabel.text = "Load the wafer in!";
                        popup.style.display = DisplayStyle.Flex;
                        popup.BringToFront();
                    }
                }
                return; 
            }
            currentMachineState++;
            UpdateScreen3Visuals();
            return;
        }
        else if (currentMachineState == MachineState.Step13_FinalAlignment)
        {
            CloseEntireInterface();
            var exposureController = Object.FindAnyObjectByType<ExposureStationController>();
            if (exposureController != null && exposureController.executeExposureButton != null)
            {
                exposureController.isVacuumSealed = true;
                exposureController.isMaskTrayInserted = true;
                exposureController.executeExposureButton.onClick.Invoke();
            }
            return;
        }

        currentMachineState++;
        UpdateScreen3Visuals();
    }

    public void ResetMachineState()
    {
        currentMachineState = MachineState.Step3_InsertMask;
        UpdateScreen3Visuals();
    }

    private void UpdateScreen3Visuals()
    {
        if (targetUIDocument == null) return;
        var mainRoot = targetUIDocument.rootVisualElement;
        var statusText = mainRoot.Q<Label>("s3StatusText");
        var dualLayer = mainRoot.Q<VisualElement>("s3-dual-layer");
        var schematicLayer = mainRoot.Q<VisualElement>("s3-schematic-layer");

        var sidebarDefault = mainRoot.Q<VisualElement>("s3-sidebar-default");
        var sidebarOptics = mainRoot.Q<VisualElement>("s3-sidebar-optics");

        if (statusText == null || dualLayer == null || schematicLayer == null) return;

        // By default, ensure sidebar is in default mode
        if (sidebarDefault != null) sidebarDefault.style.display = DisplayStyle.Flex;
        if (sidebarOptics != null) sidebarOptics.style.display = DisplayStyle.None;

        var waferLeft = mainRoot.Q<VisualElement>("huge-crosshair-left");
        var waferRight = mainRoot.Q<VisualElement>("huge-crosshair-right");
        var thickLeft = mainRoot.Q<VisualElement>("thick-crosshair-left");
        var thickRight = mainRoot.Q<VisualElement>("thick-crosshair-right");
        
        // Hide crosshairs by default on earlier steps
        if (waferLeft != null) waferLeft.style.display = DisplayStyle.None;
        if (waferRight != null) waferRight.style.display = DisplayStyle.None;
        if (thickLeft != null) thickLeft.style.display = DisplayStyle.None;
        if (thickRight != null) thickRight.style.display = DisplayStyle.None;

        switch (currentMachineState)
        {
            case MachineState.Step3_InsertMask:
                statusText.text = "Insert Mask And Press <Continue>";
                dualLayer.style.display = DisplayStyle.Flex;
                schematicLayer.style.display = DisplayStyle.None;
                break;
            case MachineState.Step4_ConfigureOptics:
                statusText.text = "Configure Optics and press continue";
                dualLayer.style.display = DisplayStyle.None;
                schematicLayer.style.display = DisplayStyle.Flex;
                break;
            case MachineState.Step5_InsertUVFilter:
                statusText.text = "Insert UV filter [Default] and press continue";
                dualLayer.style.display = DisplayStyle.Flex;
                schematicLayer.style.display = DisplayStyle.None;
                break;
            case MachineState.Step6_InsertMaskholder:
                statusText.text = "Insert maskholder and press continue";
                dualLayer.style.display = DisplayStyle.None;
                schematicLayer.style.display = DisplayStyle.Flex;
                break;
            case MachineState.Step7_FixMaskholder:
                statusText.text = "Fix Maskholder and press continue";
                dualLayer.style.display = DisplayStyle.Flex;
                schematicLayer.style.display = DisplayStyle.None;
                break;
            case MachineState.Step8_InsertChuck:
                statusText.text = "Insert chuck, connect vacuum and press continue";
                dualLayer.style.display = DisplayStyle.None;
                schematicLayer.style.display = DisplayStyle.Flex;
                break;
            case MachineState.Step9_InsertLoadframe:
                statusText.text = "Insert loadframe and press continue";
                dualLayer.style.display = DisplayStyle.None;
                schematicLayer.style.display = DisplayStyle.Flex;
                break;
            case MachineState.Step10_FinalDualScreen:
                statusText.text = "Insert Mask And Press <Continue>";
                dualLayer.style.display = DisplayStyle.Flex;
                schematicLayer.style.display = DisplayStyle.None;
                break;
            case MachineState.Step11_AdjustMicroscope:
                statusText.text = "Adjust Microscope And Press <Continue> {Joystick used for alignment}";
                dualLayer.style.display = DisplayStyle.Flex;
                schematicLayer.style.display = DisplayStyle.None;
                if (sidebarDefault != null) sidebarDefault.style.display = DisplayStyle.None;
                if (sidebarOptics != null) sidebarOptics.style.display = DisplayStyle.Flex;

                var btnL = mainRoot.Q<Button>("btnL");
                var btnR = mainRoot.Q<Button>("btnR");
                var continueBtn = mainRoot.Q<Button>("continueButton");

                if (waferLeft != null && waferRight != null && btnL != null && btnR != null && isActiveAndEnabled)
                {
                    waferLeft.style.display = DisplayStyle.Flex;
                    waferRight.style.display = DisplayStyle.Flex;
                    StartCoroutine(PlayAlignmentAnimation(waferRight, waferLeft, btnR, btnL, continueBtn));
                }
                break;
            case MachineState.Step12_InsertSubstrate:
                statusText.text = "Insert Substrate for WEC";
                dualLayer.style.display = DisplayStyle.Flex;
                schematicLayer.style.display = DisplayStyle.None;
                if (sidebarDefault != null) sidebarDefault.style.display = DisplayStyle.Flex;
                if (sidebarOptics != null) sidebarOptics.style.display = DisplayStyle.None;
                break;
            case MachineState.Step13_FinalAlignment:
                statusText.text = "Final Alignment";
                dualLayer.style.display = DisplayStyle.Flex;
                schematicLayer.style.display = DisplayStyle.None;
                if (sidebarDefault != null) sidebarDefault.style.display = DisplayStyle.None;
                if (sidebarOptics != null) sidebarOptics.style.display = DisplayStyle.Flex;

                var btnL13 = mainRoot.Q<Button>("btnL");
                var btnR13 = mainRoot.Q<Button>("btnR");
                var continueBtn13 = mainRoot.Q<Button>("continueButton");

                // Show both sets of crosshairs
                if (waferLeft != null && waferRight != null) 
                {
                    waferLeft.style.display = DisplayStyle.Flex;
                    waferRight.style.display = DisplayStyle.Flex;
                    // Reset huge to center and grey
                    waferLeft.style.translate = new StyleTranslate(new Translate(0, 0, 0));
                    waferRight.style.translate = new StyleTranslate(new Translate(0, 0, 0));
                    var finalGrey = new StyleColor(new Color(0.5f, 0.5f, 0.5f, 0.6f));
                    waferLeft.ElementAt(0).style.backgroundColor = finalGrey;
                    waferLeft.ElementAt(1).style.backgroundColor = finalGrey;
                    waferRight.ElementAt(0).style.backgroundColor = finalGrey;
                    waferRight.ElementAt(1).style.backgroundColor = finalGrey;
                }

                if (thickLeft != null && thickRight != null && btnL13 != null && btnR13 != null && isActiveAndEnabled)
                {
                    thickLeft.style.display = DisplayStyle.Flex;
                    thickRight.style.display = DisplayStyle.Flex;
                    StartCoroutine(PlayAlignmentAnimation(thickRight, thickLeft, btnR13, btnL13, continueBtn13));
                }
                break;
        }
    }

    public void CloseEntireInterface()
    {
        if (!isUiActivelyVisible) return; // Prevent infinite loop when CleanroomUIManager calls this

        isUiActivelyVisible = false;
        
        if (targetUIDocument != null && targetUIDocument.rootVisualElement != null)
        {
            targetUIDocument.rootVisualElement.style.display = DisplayStyle.None;
        }

        // Safely route the closure back to your main UI manager so it can release the camera!
        if (CleanroomUIManager.Instance != null)
        {
            CleanroomUIManager.Instance.CloseActivePanel();
        }
        else
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }
    }

    public bool IsPanelCurrentlyOpen()
    {
        return isUiActivelyVisible;
    }

    private IEnumerator PlayAlignmentAnimation(VisualElement rightCrosshair, VisualElement leftCrosshair, Button btnR, Button btnL, Button continueBtn)
    {
        if (continueBtn != null) continueBtn.SetEnabled(false);

        float moveDuration = 2.4f;
        
        var rightV = rightCrosshair.ElementAt(0);
        var rightH = rightCrosshair.ElementAt(1);
        var leftV = leftCrosshair.ElementAt(0);
        var leftH = leftCrosshair.ElementAt(1);
        
        rightV.style.backgroundColor = new StyleColor(Color.black);
        rightH.style.backgroundColor = new StyleColor(Color.black);
        leftV.style.backgroundColor = new StyleColor(Color.black);
        leftH.style.backgroundColor = new StyleColor(Color.black);

        rightCrosshair.style.translate = new StyleTranslate(new Translate(new Length(200, LengthUnit.Percent), new Length(-200, LengthUnit.Percent), 0));
        leftCrosshair.style.translate = new StyleTranslate(new Translate(new Length(-200, LengthUnit.Percent), new Length(200, LengthUnit.Percent), 0));
        
        yield return new WaitForSeconds(0.8f);
        
        // --- 1. RIGHT ALIGNMENT ---
        btnR.style.backgroundColor = new StyleColor(new Color(0.5f, 0.8f, 0.5f));
        btnR.style.color = new StyleColor(Color.black);

        // Right Horizontal Movement
        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            t = t * t * (3f - 2f * t);
            
            float rightX = Mathf.Lerp(200, 0, t);
            rightCrosshair.style.translate = new StyleTranslate(new Translate(new Length(rightX, LengthUnit.Percent), new Length(-200, LengthUnit.Percent), 0));
            
            yield return null;
        }
        
        rightCrosshair.style.translate = new StyleTranslate(new Translate(new Length(0, LengthUnit.Percent), new Length(-200, LengthUnit.Percent), 0));
        
        // Joystick Pause
        yield return new WaitForSeconds(0.2f);
        
        // Right Vertical Movement
        elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            t = t * t * (3f - 2f * t);
            
            float rightY = Mathf.Lerp(-200, 0, t);
            rightCrosshair.style.translate = new StyleTranslate(new Translate(new Length(0, LengthUnit.Percent), new Length(rightY, LengthUnit.Percent), 0));
            
            yield return null;
        }

        rightCrosshair.style.translate = new StyleTranslate(new Translate(0, 0, 0));
        
        // Final translucent grey
        var finalGrey = new StyleColor(new Color(0.5f, 0.5f, 0.5f, 0.6f));
        rightV.style.backgroundColor = finalGrey;
        rightH.style.backgroundColor = finalGrey;
        
        btnR.style.backgroundColor = new StyleColor(StyleKeyword.Null);
        btnR.style.color = new StyleColor(StyleKeyword.Null);
        
        yield return new WaitForSeconds(0.4f);
        
        // --- 2. LEFT ALIGNMENT ---
        btnL.style.backgroundColor = new StyleColor(new Color(0.5f, 0.8f, 0.5f));
        btnL.style.color = new StyleColor(Color.black);

        // Left Horizontal Movement
        elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            t = t * t * (3f - 2f * t);
            
            float leftX = Mathf.Lerp(-200, 0, t);
            leftCrosshair.style.translate = new StyleTranslate(new Translate(new Length(leftX, LengthUnit.Percent), new Length(200, LengthUnit.Percent), 0));
            
            yield return null;
        }
        
        leftCrosshair.style.translate = new StyleTranslate(new Translate(new Length(0, LengthUnit.Percent), new Length(200, LengthUnit.Percent), 0));

        // Joystick Pause
        yield return new WaitForSeconds(0.2f);

        // Left Vertical Movement
        elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            t = t * t * (3f - 2f * t);
            
            float leftY = Mathf.Lerp(200, 0, t);
            leftCrosshair.style.translate = new StyleTranslate(new Translate(new Length(0, LengthUnit.Percent), new Length(leftY, LengthUnit.Percent), 0));
            
            yield return null;
        }

        leftCrosshair.style.translate = new StyleTranslate(new Translate(0, 0, 0));
        leftV.style.backgroundColor = finalGrey;
        leftH.style.backgroundColor = finalGrey;

        btnL.style.backgroundColor = new StyleColor(StyleKeyword.Null);
        btnL.style.color = new StyleColor(StyleKeyword.Null);

        if (continueBtn != null) continueBtn.SetEnabled(true);
    }

    private void Show(VisualElement screenToShow)
    {
        if (screen1 != null) screen1.AddToClassList("hidden");
        if (screen2 != null) screen2.AddToClassList("hidden");
        if (screen3 != null) screen3.AddToClassList("hidden");

        if (screenToShow != null)
        {
            screenToShow.RemoveFromClassList("hidden");
        }
    }
}
