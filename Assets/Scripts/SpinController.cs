using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class SpinCoaterController : MonoBehaviour
{
    public enum CoaterState { Empty, WaferLoaded, PRDispensing, PRDispensed, Spinning, Complete }
    
    [Header("Process Tracking")]
    public CoaterState currentCoaterState = CoaterState.Empty;
    private float recipeTimer = 0f;
    private string currentPhaseName = "Idle";

    [Header("Equipment Controls (Inputs)")]
    public float targetSpinSpeed = 3000f; 
    public float targetSpinTime = 30f;    
    public int selectedPRIndex = 0;       

    [Header("UI Control Inputs (Standard Sliders)")]
    public Button startSpinButton;              
    public Button dispenseButton;
    public Slider speedSlider;
    public Slider timeSlider;

    [Header("UI Control Inputs (TMPro Dropdown)")]
    public TMP_Dropdown prDropdown; // <-- Converted to TextMeshPro dropdown asset

    [Header("UI Display Outputs (TMPro Text)")]
    public TMP_Text coaterStatusText;     // <-- Modern crisp text components
    public TMP_Text speedReadoutText;
    public TMP_Text timeReadoutText;
    public TMP_Text thicknessDisplayText; 

    [Header("References")]
    public Transform waferObject; 
    private Vector3 originalWaferScale;
    public GameObject dropletPrefab;       
    public Transform dropletSpawnPoint;    
    public Material liquidAmberMaterial; 

    private ClamshellLidController lidController;

    void Start()
{
    if (waferObject != null) originalWaferScale = waferObject.localScale;
    lidController = Object.FindAnyObjectByType<ClamshellLidController>();

    // 1. HARDCODED DISPENSE BUTTON HOOK:
    if (dispenseButton != null)
    {
        dispenseButton.onClick.RemoveAllListeners(); 
        dispenseButton.onClick.AddListener(TriggerFluidDispenser); 
    }
    
    // 2. FIXED: ADD THE MISSING START SPIN BUTTON LISTENER HOOK RIGHT HERE!
    if (startSpinButton != null)
    {
        startSpinButton.onClick.RemoveAllListeners(); // Clear out any loose or dead hooks
        startSpinButton.onClick.AddListener(BeginAutomatedRecipe); // Connect it straight to the spin loop!
    }
    
    InitializeEquipmentUI();
    UpdateCoaterUI("Awaiting substrate insertion...");
}

    void Update()
    {
        if (currentCoaterState != CoaterState.Spinning) 
        {
            CapturePanelInputs();
        }
    }

    void InitializeEquipmentUI()
    {
        if (speedSlider != null) { speedSlider.minValue = 2000f; speedSlider.maxValue = 5000f; speedSlider.value = 3000f; }
        if (timeSlider != null) { timeSlider.minValue = 15f; timeSlider.maxValue = 60f; timeSlider.value = 30f; }
        
        if (prDropdown != null)
        {
            prDropdown.ClearOptions(); // TMPro uses the exact same PascalCase method call structure!
            
            // Create TMPro specific option datasets
            var optionsList = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
            optionsList.Add(new TMP_Dropdown.OptionData("PR-1000"));
            optionsList.Add(new TMP_Dropdown.OptionData("PR-1200"));
            optionsList.Add(new TMP_Dropdown.OptionData("PR-1500"));
            
            prDropdown.AddOptions(optionsList);
            prDropdown.RefreshShownValue();
        }
        
        if (thicknessDisplayText != null) thicknessDisplayText.text = "Thickness: --- nm";
    }

    void CapturePanelInputs()
    {
        if (speedSlider != null) targetSpinSpeed = speedSlider.value;
        if (timeSlider != null) targetSpinTime = timeSlider.value;
        if (prDropdown != null) selectedPRIndex = prDropdown.value;

        if (speedReadoutText != null) speedReadoutText.text = $"Set Speed: {targetSpinSpeed:F0} RPM";
        if (timeReadoutText != null) timeReadoutText.text = $"Set Time: {targetSpinTime:F0} Seconds";
    }

    // Call this via your UI Dispense Button component hook setup
    public void TriggerFluidDispenser()
    {
        if (currentCoaterState != CoaterState.WaferLoaded) return;
        StartCoroutine(DispenseFluidSequence());
    }

    IEnumerator DispenseFluidSequence()
    {
        currentCoaterState = CoaterState.PRDispensing;
        string prName = prDropdown.options[selectedPRIndex].text;
        UpdateCoaterUI($"Dispensing chemical matrix: {prName}...");

        for (int i = 0; i < 3; i++)
        {
            if (dropletPrefab != null && dropletSpawnPoint != null)
            {
                GameObject drop = Instantiate(dropletPrefab, dropletSpawnPoint.position, Quaternion.identity);
                Destroy(drop, 1.5f);
            }
            yield return new WaitForSeconds(0.25f);
        }

        yield return new WaitForSeconds(0.4f);
        currentCoaterState = CoaterState.PRDispensed;
        
        Renderer waferRen = waferObject.GetComponent<Renderer>();
        if (waferRen != null && liquidAmberMaterial != null) waferRen.material = liquidAmberMaterial;

        UpdateCoaterUI("Dispense complete. Chemical logged. Secure lid to spin.");
    }

    // Call this via your UI Start Spin Button component hook setup
    public void BeginAutomatedRecipe()
    {
        if (currentCoaterState != CoaterState.PRDispensed) return;

        if (lidController != null && lidController.isOpen)
        {
            UpdateCoaterUI("ERROR: Safety interlock violation! Chamber must be sealed to spin.");
            return;
        }

        StartCoroutine(AutomatedSpinRecipeRoutine());
    }

    IEnumerator AutomatedSpinRecipeRoutine()
    {
        currentCoaterState = CoaterState.Spinning;
        recipeTimer = 0f;

        // --- STAGE 1: AUTOMATED SPREADING PHASE (Fixed Safety Profile) ---
        currentPhaseName = "SPREADING";
        float spreadDuration = 5f;
        float spreadRPM = 500f;
        
        while (recipeTimer < spreadDuration)
        {
            recipeTimer += Time.deltaTime;
            if (waferObject != null) waferObject.Rotate(Vector3.up * spreadRPM * 0.2f * Time.deltaTime);
            UpdateCoaterUI($"[{currentPhaseName}] Speed: {spreadRPM:F0} RPM | Time Left: {(spreadDuration - recipeTimer):F1}s");
            yield return null;
        }

        // --- STAGE 2: AUTOMATED CASTING PHASE (User Configured Controls) ---
        currentPhaseName = "CASTING";
        recipeTimer = 0f;
        
        while (recipeTimer < targetSpinTime)
        {
            recipeTimer += Time.deltaTime;
            if (waferObject != null)
            {
                waferObject.Rotate(Vector3.up * targetSpinSpeed * 0.2f * Time.deltaTime);
                float thinningFactor = Mathf.Lerp(1.0f, 0.82f, recipeTimer / targetSpinTime);
                waferObject.localScale = new Vector3(originalWaferScale.x, originalWaferScale.y * thinningFactor, originalWaferScale.z);
            }
            UpdateCoaterUI($"[{currentPhaseName}] Speed: {targetSpinSpeed:F0} RPM | Time Left: {(targetSpinTime - recipeTimer):F1}s");
            yield return null;
        }

        CompleteSpinCycle();
    }

    void CompleteSpinCycle()
    {
        currentCoaterState = CoaterState.Complete;
        if (waferObject != null) waferObject.localRotation = Quaternion.identity;
        
        float baseThickness = selectedPRIndex == 0 ? 1000f : (selectedPRIndex == 1 ? 1200f : 1500f);
        float customThicknessOutput = baseThickness * (3000f / targetSpinSpeed);

        if (thicknessDisplayText != null)
        {
            thicknessDisplayText.text = $"Thickness: {customThicknessOutput:F0} nm";
        }
        
        UpdateCoaterUI("Spin cycle complete. Uniform film thickness achieved.");
    }

    public void LoadWaferIntoCoater()
    {
        currentCoaterState = CoaterState.WaferLoaded;
        UpdateCoaterUI("Substrate loaded. Ready for chemical dispensing.");
    }

    void UpdateCoaterUI(string message = "")
    {
        // Unlocked as soon as a wafer is dropped on the chuck
        bool canSelectRecipe = (currentCoaterState == CoaterState.WaferLoaded || currentCoaterState == CoaterState.PRDispensed);
        
        // Unlocked ONLY after the liquid chemical puddle has been applied
        bool canSpinNow = (currentCoaterState == CoaterState.PRDispensed);
        
        // FIXED: The Dispense button is now perfectly active ONLY when a bare wafer is waiting for its chemical coat!
        if (dispenseButton != null) 
        {
            dispenseButton.interactable = (currentCoaterState == CoaterState.WaferLoaded);
        }

        if (startSpinButton != null)
        {
            startSpinButton.interactable = canSpinNow;
        }

        // Keep your recipe tuning inputs accessible
        if (speedSlider != null) speedSlider.interactable = canSelectRecipe;
        if (timeSlider != null) timeSlider.interactable = canSelectRecipe;
        if (prDropdown != null) prDropdown.interactable = canSelectRecipe;
        
        // Update the master display board
        if (coaterStatusText != null && message != "") coaterStatusText.text = "POLOS Coater: " + message;
    }
}