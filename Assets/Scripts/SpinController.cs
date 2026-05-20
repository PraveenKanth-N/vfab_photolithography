using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class SpinCoaterController : MonoBehaviour
{
    public enum CoaterState { Empty, WaferLoaded, PRDispensing, PRDispensed, Spinning, Complete }
    
    [Header("Process Tracking")]
    public CoaterState currentCoaterState = CoaterState.Empty;
    public float targetRPM = 4000f;
    public float spinDuration = 5f; 
    private float spinTimer = 0f;

    [Header("Wafer Reference")]
    public Transform waferObject; 
    private Vector3 originalWaferScale;

    [Header("Fluid Management System")]
    public GameObject dropletPrefab;       
    public Transform dropletSpawnPoint;    
    public int dropCount = 3;              
    public float timeBetweenDrops = 0.25f;

    [Header("UI Control Inputs")]
    public Button dispenseButton;
    public Button startSpinButton;
    public Slider rpmSlider;
    public TMP_Text coaterStatusText;

    [Header("Chemical Material Swaps")]
    public Material liquidAmberMaterial; 

    // NEW: Reference to the hinge tracking script to verify open/close status
    private ClamshellLidController lidController;

    void Start()
    {
        if (waferObject != null) originalWaferScale = waferObject.localScale;

        // Automatically locate the lid component sitting on your hinge parent asset
        lidController = Object.FindAnyObjectByType<ClamshellLidController>();
        if (lidController == null) lidController = GetComponentInChildren<ClamshellLidController>();

        if (dispenseButton != null) dispenseButton.onClick.AddListener(TriggerFluidDispenser);
        if (startSpinButton != null) startSpinButton.onClick.AddListener(BeginHighSpeedSpin);
        
        if (rpmSlider != null) { rpmSlider.minValue = 3000f; rpmSlider.maxValue = 5000f; rpmSlider.value = 4000f; }

        UpdateCoaterUI("Awaiting substrate insertion...");
    }

    void Update()
    {
        // Continuously capture slider modifications before spin tracking runs
        if (rpmSlider != null && currentCoaterState != CoaterState.Spinning) 
        {
            targetRPM = rpmSlider.value;
        }

        if (currentCoaterState == CoaterState.Spinning)
        {
            spinTimer += Time.deltaTime;

            if (waferObject != null)
            {
                waferObject.Rotate(Vector3.up * targetRPM * Time.deltaTime);
                float thinningFactor = Mathf.Lerp(1.0f, 0.85f, spinTimer / spinDuration);
                waferObject.localScale = new Vector3(originalWaferScale.x, originalWaferScale.y * thinningFactor, originalWaferScale.z);
            }

            if (spinTimer >= spinDuration)
            {
                CompleteSpinCycle();
            }
        }
    }

    void TriggerFluidDispenser()
    {
        if (currentCoaterState != CoaterState.WaferLoaded) return;
        StartCoroutine(DispenseFluidSequence());
    }

    IEnumerator DispenseFluidSequence()
    {
        currentCoaterState = CoaterState.PRDispensing;
        UpdateCoaterUI("Dispenser line engaged. Delivering polymer photoresist matrix...");

        for (int i = 0; i < dropCount; i++)
        {
            if (dropletPrefab != null && dropletSpawnPoint != null)
            {
                GameObject drop = Instantiate(dropletPrefab, dropletSpawnPoint.position, Quaternion.identity);
                Destroy(drop, 1.5f);
            }
            yield return new WaitForSeconds(timeBetweenDrops);
        }

        yield return new WaitForSeconds(0.4f);
        currentCoaterState = CoaterState.PRDispensed;
        
        Renderer waferRen = waferObject.GetComponent<Renderer>();
        if (waferRen != null && liquidAmberMaterial != null) waferRen.material = liquidAmberMaterial;

        UpdateCoaterUI("Dispense operation complete. Puddle ready for spin stabilization cycle.");
    }

    void BeginHighSpeedSpin()
    {
        if (currentCoaterState != CoaterState.PRDispensed) return;

        // FIXED QUIRK 2: INTERLOCK INTERCEPTOR
        // If the physical lid object is open, reject execution and throw a warning notice!
        if (lidController != null)
        {
            // Accessing our open toggle tracker inside ClamshellLidController
            // Note: If 'isOpen' is private in your script, make sure to add "public" before "bool isOpen" in ClamshellLidController!
            System.Reflection.FieldInfo openField = lidController.GetType().GetField("isOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            bool isLidOpen = openField != null ? (bool)openField.GetValue(lidController) : false;

            if (isLidOpen)
            {
                UpdateCoaterUI("ERROR: Safety interlock violation! Seal clamshell shield before executing motor spin loop.");
                Debug.LogWarning("POLOS Coater: Spin cycle blocked. Vacuum chamber ambient shroud must be locked.");
                return; // Stop right here, don't spin!
            }
        }

        // FIXED QUIRK 1: Grab the current real value from the UI slider right at the frame of ignition
        if (rpmSlider != null) targetRPM = rpmSlider.value;

        currentCoaterState = CoaterState.Spinning;
        spinTimer = 0f;
        UpdateCoaterUI($"Spinning chuck assembly at {targetRPM:F0} RPM...");
    }

    void CompleteSpinCycle()
    {
        currentCoaterState = CoaterState.Complete;
        if (waferObject != null) waferObject.localRotation = Quaternion.identity;
        UpdateCoaterUI("Spin coating cycle finished! Uniform polymer layer stabilized.");
    }

    public void LoadWaferIntoCoater()
    {
        currentCoaterState = CoaterState.WaferLoaded;
        UpdateCoaterUI("Substrate registered on chuck. Ready for chemical dispensing.");
    }

    void UpdateCoaterUI(string message = "")
    {
        if (dispenseButton != null) dispenseButton.interactable = (currentCoaterState == CoaterState.WaferLoaded);
        if (startSpinButton != null) startSpinButton.interactable = (currentCoaterState == CoaterState.PRDispensed || currentCoaterState == CoaterState.Spinning);
        if (rpmSlider != null) rpmSlider.interactable = (currentCoaterState == CoaterState.PRDispensed);
        
        if (coaterStatusText != null && message != "") coaterStatusText.text = "POLOS Coater: " + message;
    }
}