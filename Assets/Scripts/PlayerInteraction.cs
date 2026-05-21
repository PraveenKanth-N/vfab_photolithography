using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionDistance = 5.0f; 

    [Header("References")]
    public Camera playerCamera;
    public Transform waferHolderSocket; 

    private GameObject heldObject = null;
    private Rigidbody heldRigidbody = null;
    
    // Cache the original layer of the wafer so we can restore it when placed
    private int originalLayer; 
    private int ignoreRaycastLayer;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Find the built-in "Ignore Raycast" layer ID (usually index 2)
        ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
    }

    void Update()
    {
        Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactionDistance, Color.red);

        if (Input.GetMouseButtonDown(0))
        {
            // FIRST PRIORITY: Try to click UI world-space buttons using the crosshair
            if (TryClickWorldUI())
            {
                return; // If we clicked a wall button successfully, stop here!
            }

            // SECOND PRIORITY: Handle physical pick up and drop tracking
            if (heldObject == null)
            {
                TryPickUpObject();
            }
            else
            {
                TryPlaceObject();
            }
        }
    }

    bool TryClickWorldUI()
    {
        if (EventSystem.current == null) return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = new Vector2(Screen.width / 2f, Screen.height / 2f);

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count > 0)
        {
            GameObject UI_Target = results[0].gameObject;
            
            // 1. FILTER OUT BLOCKERS: If we hit a dropdown background click-catcher, skip past it!
            if ((UI_Target.name.Contains("Blocker") || UI_Target.name.Contains("Template")) && results.Count > 1)
            {
                // Force focus down to the actual option list items sitting beneath the veil
                for (int i = 1; i < results.Count; i++)
                {
                    if (!results[i].gameObject.name.Contains("Blocker"))
                    {
                        UI_Target = results[i].gameObject;
                        break;
                    }
                }
            }
            
            // 2. TRIGGER SELECTION: Fire a direct click event on whatever component or text item we hit
            Toggle toggleComponent = UI_Target.GetComponent<Toggle>();
            if (toggleComponent == null) toggleComponent = UI_Target.GetComponentInParent<Toggle>();
            
            // TMPro menu option items are structured as Toggles under the hood!
            if (toggleComponent != null || UI_Target.name.Contains("Item"))
            {
                GameObject clickTarget = toggleComponent != null ? toggleComponent.gameObject : UI_Target;
                ExecuteEvents.Execute(clickTarget, pointerData, ExecuteEvents.pointerClickHandler);
                Debug.Log($"Crosshair intercepted choice layer assignment on: '{clickTarget.name}'");
                return true;
            }

            // 3. DROPDOWN INITIAL CLOSURE/OPENING HOOKS
            TMP_Dropdown tmpDropdown = UI_Target.GetComponent<TMP_Dropdown>();
            if (tmpDropdown == null) tmpDropdown = UI_Target.GetComponentInParent<TMP_Dropdown>();
            if (tmpDropdown != null)
            {
                ExecuteEvents.Execute(tmpDropdown.gameObject, pointerData, ExecuteEvents.pointerClickHandler);
                return true;
            }

            // 4. STANDARD BUTTON HANDLERS
            Button button = UI_Target.GetComponent<Button>();
            if (button == null) button = UI_Target.GetComponentInParent<Button>();
            if (button != null && button.interactable)
            {
                ExecuteEvents.Execute(button.gameObject, pointerData, ExecuteEvents.pointerClickHandler);
                return true; 
            }

            // 5. SLIDER ADJUSTMENT AXIS
            Slider slider = UI_Target.GetComponent<Slider>();
            if (slider == null) slider = UI_Target.GetComponentInParent<Slider>();
            if (slider != null && slider.interactable)
            {
                RectTransform rect = slider.GetComponent<RectTransform>();
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, pointerData.position, playerCamera, out localPoint))
                {
                    float sliderWidth = rect.rect.width;
                    float percentage = Mathf.Clamp01((localPoint.x + (sliderWidth * 0.5f)) / sliderWidth); 
                    slider.value = Mathf.Lerp(slider.minValue, slider.maxValue, percentage);
                }
                return true;
            }
        }
        return false;
    }

    void TryPickUpObject() {
    Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
    RaycastHit hit;

    if (Physics.Raycast(ray, out hit, interactionDistance))
    {
        GameObject target = hit.collider.gameObject;
        string nameLower = target.name.ToLower();

        // Case 1: Direct hit on the wafer mesh collider
        if (nameLower.Contains("wafer")) 
        {
            HotplateController hotplate = Object.FindAnyObjectByType<HotplateController>();
            if (hotplate != null && hotplate.currentState != HotplateController.HotplateState.Baking)
            {
                // If it was sitting on the hotplate, reset the hotplate back to default idle state
                Collider[] hotplateHits = Physics.OverlapSphere(hotplate.transform.position + new Vector3(0f, 0.5f, 0f), 0.6f);
                foreach (Collider col in hotplateHits)
                {
                    if (col.gameObject == hit.collider.gameObject)
                    {
                        hotplate.currentState = HotplateController.HotplateState.Idle;
                        hotplate.BeginThermalBakeCycle(); // Re-runs internal UI update to clear messages
                        break;
                    }
                }
            }
            PickUpWafer(target);
            return;
        }
        
        // Case 2: Hit the spin coater base or hotplate, but the wafer is sitting inside/on it
        if (nameLower.Contains("spin") || nameLower.Contains("coater") || nameLower.Contains("bottom") || nameLower.Contains("hotplate"))
        {
            // Scan a tiny physical sphere (radius of 0.5 units) around the hit point to find any wafer
            Collider[] collidersCloseBy = Physics.OverlapSphere(hit.point, 0.5f);
            foreach (Collider nearbyCollider in collidersCloseBy)
            {
                if (nearbyCollider.gameObject.name.ToLower().Contains("wafer"))
                {
                    // Found it nearby! Pick it up.
                    PickUpWafer(nearbyCollider.gameObject);
                    
                    // Reset the spin coater state back to empty so safety protocols activate
                    SpinCoaterController coater = target.GetComponentInParent<SpinCoaterController>();
                    if (coater == null) coater = target.GetComponent<SpinCoaterController>();
                    if (coater != null) 
                    {
                        coater.currentCoaterState = SpinCoaterController.CoaterState.Empty;
                    }
                    return;
                }
            }
        }
    }
}

    // Helper method to keep things clean
    void PickUpWafer(GameObject wafer)
    {
        heldObject = wafer;
        heldRigidbody = heldObject.GetComponent<Rigidbody>();

        if (heldRigidbody != null) heldRigidbody.isKinematic = true; 

        originalLayer = heldObject.layer;
        heldObject.layer = ignoreRaycastLayer;

        foreach (Transform child in heldObject.transform) {
            child.gameObject.layer = ignoreRaycastLayer;
        }

        heldObject.transform.SetParent(waferHolderSocket);
        heldObject.transform.localPosition = Vector3.zero;
        heldObject.transform.localRotation = Quaternion.identity;
        
        Debug.Log($"SUCCESS: Retreived substrate '{heldObject.name}' from station.");
    }

    void TryPlaceObject()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            GameObject target = hit.collider.gameObject;
            string targetName = target.name.ToLower();

            // Inside Case 1: Spin Coater Placement Block
            if (targetName.Contains("spin") || targetName.Contains("coater"))
            {
                SpinCoaterController coater = target.GetComponent<SpinCoaterController>();
                if (coater == null) coater = target.GetComponentInParent<SpinCoaterController>();

                if (coater != null)
                {
                    RestoreHeldObjectLayer();
                    DetachHeldObject();
                    coater.LoadWaferIntoCoater(); 
                    
                    // SNAP POSITION AND FORCE THE WAFER TO LIE PERFECTLY FLAT
                    heldObject.transform.position = target.transform.position + new Vector3(0f, 0.2f, 0f);
                    heldObject.transform.rotation = Quaternion.identity; 
                    
                    heldObject = null;
                    return;
                }
            }
            
            // Inside Case 2: Hotplate Placement Block
            if (targetName.Contains("hotplate"))
            {
                RestoreHeldObjectLayer();
                DetachHeldObject();
                
                heldObject.transform.position = target.transform.position + new Vector3(0f, 0.25f, 0f);
                heldObject.transform.rotation = Quaternion.identity;
                
                // NEW: Alert the hotplate script to start checking thermal state parameters!
                HotplateController hotplate = target.GetComponent<HotplateController>();
                if (hotplate != null)
                {
                    hotplate.RegisterWaferPlacement(heldObject);
                }

                heldObject = null;
                Debug.Log("SUCCESS: Placed wafer back onto hotplate element.");
                return;
            }

            Debug.LogWarning($"Placement rejected: Crosshair targeting non-registered object '{target.name}'");
        }
        else
        {
            Debug.LogWarning("Placement rejected: Raycast extended into open air. Aim lower at your bench stations!");
        }
    }

    void RestoreHeldObjectLayer()
    {
        if (heldObject == null) return;
        heldObject.layer = originalLayer;
        foreach (Transform child in heldObject.transform) {
            child.gameObject.layer = originalLayer;
        }
    }

    void DetachHeldObject()
    {
        if (heldObject == null) return;
        heldObject.transform.SetParent(null);
        if (heldRigidbody != null) heldRigidbody.isKinematic = false;
    }
}