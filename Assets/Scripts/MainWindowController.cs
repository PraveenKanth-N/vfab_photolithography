using UnityEngine;
using UnityEngine.UIElements;

public class MainWindowController : MonoBehaviour
{
    [Header("UI References")]
    public UIDocument targetUIDocument;

    private VisualElement screen1;
    private VisualElement screen2;
    private VisualElement screen3;
    private VisualElement mainRoot;

    private bool isUiActivelyVisible = false;

    void OnEnable()
    {
        // Force UI re-query because the visual tree might have been regenerated 
        // after being disabled and re-enabled!
        mainRoot = null; 
        InitializeUI();
        
        if (mainRoot != null)
        {
            if (!isUiActivelyVisible)
            {
                HideUiCompletely();
            }
            else
            {
                mainRoot.style.display = DisplayStyle.Flex;
            }
        }
    }

    void Start()
    {
        mainRoot = null;
        InitializeUI();
        if (!isUiActivelyVisible)
        {
            HideUiCompletely();
        }
    }

    private void InitializeUI()
    {
        if (mainRoot != null) return;

        if (targetUIDocument == null)
        {
            targetUIDocument = GetComponent<UIDocument>();
            if (targetUIDocument == null && transform.parent != null)
            {
                targetUIDocument = transform.parent.GetComponentInChildren<UIDocument>(true);
            }
            if (targetUIDocument == null)
            {
                targetUIDocument = Object.FindAnyObjectByType<UIDocument>(FindObjectsInactive.Include);
            }
        }

        if (targetUIDocument == null)
        {
            Debug.LogError("[MainWindowController] No UIDocument found in the scene or on this object!");
            return;
        }

        if (targetUIDocument.panelSettings == null)
        {
            Debug.LogWarning("[MainWindowController] No Panel Settings assigned to UIDocument! The UI will render with Constant Pixel Size and appear extremely large. Please assign a Panel Settings asset (like MainPanelSettings) to the UIDocument component.");
        }

        VisualElement rootElement = targetUIDocument.rootVisualElement;
        if (rootElement == null) return;

        // Query the actual top-level container defined in UXML
        mainRoot = rootElement.Q<VisualElement>(className: "root");
        if (mainRoot == null) mainRoot = rootElement; // fallback

        // Query the sub-panels from your UXML
        screen1 = mainRoot.Q<VisualElement>("screen1");
        screen2 = mainRoot.Q<VisualElement>("screen2");
        screen3 = mainRoot.Q<VisualElement>("screen3");

        var newRecipeBtn = mainRoot.Q<Button>("newRecipe");
        if (newRecipeBtn != null) newRecipeBtn.clicked += OpenRecipe;

        var runBtn = mainRoot.Q<Button>("runButton");
        if (runBtn != null) runBtn.clicked += OpenMachine;

        // Hook up the new Continue button on screen 3 to close the UI!
        var continueBtn = mainRoot.Q<Button>("continueButton");
        if (continueBtn != null)
        {
            continueBtn.clicked -= CloseEntireInterface;
            continueBtn.clicked += CloseEntireInterface;
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
        isUiActivelyVisible = true; // Set flag FIRST
        
        if (targetUIDocument == null) InitializeUI();
        
        if (targetUIDocument != null)
        {
            targetUIDocument.gameObject.SetActive(true);
            if (targetUIDocument.transform.parent != null && !targetUIDocument.transform.parent.gameObject.activeSelf)
            {
                targetUIDocument.transform.parent.gameObject.SetActive(true);
            }
        }
        else
        {
            gameObject.SetActive(true); 
        }

        // Force a re-initialize in case rootElement changed
        mainRoot = null;
        InitializeUI();
        
        // 1. Force the master visual tree to display
        if (mainRoot != null) mainRoot.style.display = DisplayStyle.Flex;

        // 2. Unlock the mouse cursor so you can actually click things
        var look = Object.FindAnyObjectByType<MouseLook>();
        if (look != null) look.ForceUnlockForUI();

        // 3. Show screen 1 layout page
        Show(screen1);
    }

    public void CloseEntireInterface()
    {
        InitializeUI();
        HideUiCompletely();

        // Lock the cursor back into gameplay mode smoothly
        var look = Object.FindAnyObjectByType<MouseLook>();
        if (look != null) look.ForceLockFromUI();
    }

    private void HideUiCompletely()
    {
        InitializeUI();
        isUiActivelyVisible = false;
        if (mainRoot != null) mainRoot.style.display = DisplayStyle.None;
        
        if (targetUIDocument != null)
        {
            targetUIDocument.gameObject.SetActive(false);
            
            // Also explicitly disable the parent UI object if it exists so the whole cluster is hidden!
            if (targetUIDocument.transform.parent != null && targetUIDocument.transform.parent.name == "UI")
            {
                targetUIDocument.transform.parent.gameObject.SetActive(false);
            }
        }
        else
        {
            gameObject.SetActive(false); // Disable GameObject when completely hidden
        }
    }

    void Show(VisualElement active)
    {
        if (screen1 != null) screen1.style.display = (active == screen1) ? DisplayStyle.Flex : DisplayStyle.None;
        if (screen2 != null) screen2.style.display = (active == screen2) ? DisplayStyle.Flex : DisplayStyle.None;
        if (screen3 != null) screen3.style.display = (active == screen3) ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void OpenRecipe()
    {
        Show(screen2);
    }

    void OpenMachine()
    {
        Show(screen3);
    }

    public bool IsPanelCurrentlyOpen()
    {
        return isUiActivelyVisible;
    }
}
