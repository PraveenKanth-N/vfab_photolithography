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
        Step10_FinalDualScreen
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
            
            targetUIDocument.gameObject.SetActive(false);
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
    }

    public void OpenInterfaceToStep1()
    {
        Debug.Log("Opening UI to Image 1 (Screen 1)");
        isUiActivelyVisible = true;
        InitializeUI();

        if (targetUIDocument != null)
        {
            if (!targetUIDocument.gameObject.activeSelf)
                targetUIDocument.gameObject.SetActive(true);

            if (targetUIDocument.rootVisualElement != null)
            {
                targetUIDocument.rootVisualElement.style.display = DisplayStyle.Flex;
            }
        }

        // CleanroomUIManager already handles unlocking the camera when opening panels, 
        // but as a fallback if you run this script isolated:
        if (CleanroomUIManager.Instance == null)
        {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }

        Show(screen1);
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
            CloseEntireInterface();
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

        if (statusText == null || dualLayer == null || schematicLayer == null) return;

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
        }
    }

    public void CloseEntireInterface()
    {
        if (!isUiActivelyVisible) return; // Prevent infinite loop when CleanroomUIManager calls this

        isUiActivelyVisible = false;
        
        if (targetUIDocument != null)
        {
            if (targetUIDocument.rootVisualElement != null)
            {
                targetUIDocument.rootVisualElement.style.display = DisplayStyle.None;
            }
            targetUIDocument.gameObject.SetActive(false);
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
