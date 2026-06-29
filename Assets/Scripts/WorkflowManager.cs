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
    public Vector2 backgroundOffset = new Vector2(0f, -5f); // Nudge it down by 5 pixels by default

    // Cached References
    private SpinCoaterController spinCoater;
    private PlayerInteraction playerInteraction;
    private HotplateController hotplate;
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
    }

    // Helper method to set text without mark tags
    private void SetText(string content)
    {
        hudText.text = content;
    }

    private void Update()
    {
        if (hudText == null) return;

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
            case 0:
                SetText("Press W, A, S, D to move");
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
                {
                    currentStep = 1;
                    stepTimer = 2f;
                }
                break;

            case 1:
                SetText("<color=#cc0000><b>Disclaimer:</b> Do not press Escape (ESC)</color>");
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f)
                {
                    currentStep = 2;
                }
                break;

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
                SetText("Adjust the RPM and time if required, then dispense the Photoresist (PR)");
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
                SetText("Adjust the hotplate's set temperature and time if required, then start prebake/softbake");
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
                SetText("Click on the wafer carrier to place the wafer securely");
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
                SetText("Carry it over to the Mask Aligner workstation and place the Wafer carrier down onto the highlighted spot securely");
                
                // Advance when the carrier is successfully placed on a dock (heldObject becomes null)
                if (playerInteraction != null && playerInteraction.heldObject == null)
                {
                    // To ensure they didn't just drop it on the floor, we could check if a WaferCarrier is on a dock.
                    // But for the workflow, heldObject == null after holding the carrier is a good enough trigger.
                    currentStep = 22;
                }
                break;

            case 22:
                SetText("<color=#008800><b>Wafer Transport Complete!</b></color>");
                break;
        }
    }
}
