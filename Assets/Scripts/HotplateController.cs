using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HotplateController : MonoBehaviour
{
    public enum HotplateState { Idle, WaferLoaded, Baking, Complete }

    [Header("Process Tracking")]
    public HotplateState currentState = HotplateState.Idle;
    public float targetTemperature = 90f; // Target soft-bake temperature in °C
    public float bakeDuration = 7f;       // Paced time duration for simulation loop
    private float bakeTimer = 0f;

    [Header("UI Wall Connections")]
    public Text hotplateStatusText; // Assign a text element on your wall canvas
    public Text temperatureDisplay;  // Shows the thermal read-out

    [Header("Material References")]
    public Material bakedAmberMaterial; // The final, dry, dull uniform amber look

    void Start()
    {
        UpdateHotplateUI("Hotplate stable at baseline temperature.");
    }

    void Update()
    {
        if (currentState == HotplateState.Baking)
        {
            bakeTimer += Time.deltaTime;
            
            // Display simulated temperature fluctuating realistically near 90°C
            if (temperatureDisplay != null)
            {
                float currentTemp = targetTemperature + Random.Range(-0.4f, 0.4f);
                temperatureDisplay.text = $"{currentTemp:F1}°C";
            }

            if (bakeTimer >= bakeDuration)
            {
                CompleteSoftBake();
            }
        }
    }

    // Automatically triggered by PlayerInteraction.cs when dropped on the hotplate!
    public void RegisterWaferPlacement(GameObject wafer)
    {
        // Double check: We only care if the spin coating cycle was already finished
        SpinCoaterController coater = Object.FindAnyObjectByType<SpinCoaterController>();
        if (coater != null && coater.currentCoaterState == SpinCoaterController.CoaterState.Complete)
        {
            currentState = HotplateState.Baking;
            bakeTimer = 0f;
            
            StartCoroutine(BakeCycleRoutine(wafer));
        }
        else
        {
            currentState = HotplateState.WaferLoaded;
            UpdateHotplateUI("Substrate loaded. Awaiting process activation sequence.");
        }
    }

    IEnumerator BakeCycleRoutine(GameObject wafer)
    {
        UpdateHotplateUI("Dehydration Soft-Bake active. Evaporating volatile solvents...");
        yield return new WaitForSeconds(bakeDuration);
    }

    void CompleteSoftBake()
    {
        currentState = HotplateState.Complete;
        UpdateHotplateUI("Soft-bake complete! Photoresist polymer layer matrix fully dried and stabilized.");

        // Find the wafer sitting on top of the plate and swap its material to the final matte look
        Collider[] hits = Physics.OverlapSphere(transform.position + new Vector3(0f, 0.5f, 0f), 0.6f);
        foreach (Collider hit in hits)
        {
            if (hit.gameObject.name.ToLower().Contains("wafer"))
            {
                Renderer waferRen = hit.gameObject.GetComponent<Renderer>();
                if (waferRen != null && bakedAmberMaterial != null)
                {
                    waferRen.material = bakedAmberMaterial;
                    Debug.Log("Wafer chemical matrix altered: Dried Matte Photoresist layer locked.");
                }
            }
        }
    }

    void UpdateHotplateUI(string message)
    {
        if (hotplateStatusText != null) hotplateStatusText.text = "POLOS Hotplate: " + message;
        if (temperatureDisplay != null && currentState != HotplateState.Baking) 
        {
            temperatureDisplay.text = $"{targetTemperature:F1}°C";
        }
    }
}