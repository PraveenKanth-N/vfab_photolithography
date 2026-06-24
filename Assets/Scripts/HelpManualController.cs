using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

[System.Serializable]
public struct ManualTab
{
    public string tabName;
    public TextAsset manualTextFile;
}

public class HelpManualController : MonoBehaviour
{
    [Header("UI Document")]
    public UIDocument uiDocument;

    [Header("Manual Setup")]
    public List<ManualTab> manuals = new List<ManualTab>();
    
    private VisualElement root;
    private VisualElement overlay;
    private VisualElement tabBar;
    private ScrollView contentScroll;
    private Button closeBtn;

    private List<Button> tabButtons = new List<Button>();
    public bool isOpen = false;

    public static HelpManualController Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (uiDocument == null) return;
        
        root = uiDocument.rootVisualElement;
        overlay = root.Q<VisualElement>("helpOverlay");
        tabBar = root.Q<VisualElement>("tabBar");
        contentScroll = root.Q<ScrollView>("contentScroll");
        closeBtn = root.Q<Button>("closeBtn");

        if (overlay != null) overlay.style.display = DisplayStyle.None; // Hide initially

        if (closeBtn != null)
        {
            closeBtn.clicked += () => ToggleHelpMenu(false);
        }

        BuildTabs();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            ToggleHelpMenu();
        }
    }

    public void ToggleHelpMenu(bool show)
    {
        if (overlay == null) return;
        overlay.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        isOpen = show;

        MouseLook mouseLook = Object.FindAnyObjectByType<MouseLook>();
        if (mouseLook != null)
        {
            if (show) mouseLook.ForceUnlockForUI();
            else mouseLook.ForceLockFromUI();
        }

        if (show && tabButtons.Count > 0)
        {
            // Open the first tab by default
            LoadManual(0);
        }
    }

    public void ToggleHelpMenu()
    {
        if (overlay == null) return;
        bool isShowing = overlay.style.display == DisplayStyle.Flex;
        ToggleHelpMenu(!isShowing);
    }

    private void BuildTabs()
    {
        if (tabBar == null) return;
        tabBar.Clear();
        tabButtons.Clear();

        for (int i = 0; i < manuals.Count; i++)
        {
            int index = i; // capture loop variable
            Button tabBtn = new Button();
            tabBtn.text = manuals[i].tabName;
            tabBtn.AddToClassList("help-tab");
            
            tabBtn.clicked += () => LoadManual(index);
            
            tabBar.Add(tabBtn);
            tabButtons.Add(tabBtn);
        }
    }

    private void LoadManual(int index)
    {
        if (index < 0 || index >= manuals.Count) return;

        // Update tab styling
        for (int i = 0; i < tabButtons.Count; i++)
        {
            if (i == index) tabButtons[i].AddToClassList("help-tab-active");
            else tabButtons[i].RemoveFromClassList("help-tab-active");
        }

        // Clear existing content
        contentScroll.Clear();

        if (manuals[index].manualTextFile == null) return;

        string rawText = manuals[index].manualTextFile.text;
        string[] lines = rawText.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);

        string currentTextBuffer = "";

        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            
            if (trimmed.StartsWith("[IMAGE:") && trimmed.EndsWith("]"))
            {
                // Flush text buffer first
                if (!string.IsNullOrEmpty(currentTextBuffer.Trim()))
                {
                    FlushTextBuffer(currentTextBuffer);
                    currentTextBuffer = "";
                }

                // Extract image name
                string imageName = trimmed.Substring(7, trimmed.Length - 8).Trim();
                
                // Spawn Image block
                VisualElement imageBlock = new VisualElement();
                imageBlock.AddToClassList("help-image-block");
                
                Texture2D loadedTex = Resources.Load<Texture2D>("Images/" + imageName);
                if (loadedTex != null)
                {
                    imageBlock.style.backgroundImage = loadedTex;
                }
                else
                {
                    // Fallback visual if image not found
                    Label errorLabel = new Label($"[Image missing: {imageName}]");
                    errorLabel.style.color = new StyleColor(Color.red);
                    errorLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                    imageBlock.Add(errorLabel);
                }
                
                contentScroll.Add(imageBlock);
            }
            else
            {
                currentTextBuffer += line + "\n";
            }
        }

        // Flush remaining text
        if (!string.IsNullOrEmpty(currentTextBuffer.Trim()))
        {
            FlushTextBuffer(currentTextBuffer);
        }

        // Scroll back to top
        contentScroll.scrollOffset = Vector2.zero;
    }

    private void FlushTextBuffer(string text)
    {
        Label textLabel = new Label(text);
        textLabel.AddToClassList("help-text-block");
        textLabel.enableRichText = true;
        contentScroll.Add(textLabel);
    }
}
