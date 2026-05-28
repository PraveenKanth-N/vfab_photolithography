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
    
    private int originalLayer; 
    private int ignoreRaycastLayer;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
    }

    void Update()
    {
        Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactionDistance, Color.red);

        if (Input.GetMouseButtonDown(0))
        {
            if (TryClickWorldUI()) return;

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
            
            if ((UI_Target.name.Contains("Blocker") || UI_Target.name.Contains("Template")) && results.Count > 1)
            {
                for (int i = 1; i < results.Count; i++)
                {
                    if (!results[i].gameObject.name.Contains("Blocker"))
                    {
                        UI_Target = results[i].gameObject;
                        break;
                    }
                }
            }
            
            Toggle toggleComponent = UI_Target.GetComponent<Toggle>();
            if (toggleComponent == null) toggleComponent = UI_Target.GetComponentInParent<Toggle>();
            
            if (toggleComponent != null || UI_Target.name.Contains("Item"))
            {
                GameObject clickTarget = toggleComponent != null ? toggleComponent.gameObject : UI_Target;
                ExecuteEvents.Execute(clickTarget, pointerData, ExecuteEvents.pointerClickHandler);
                return true;
            }

            TMP_Dropdown tmpDropdown = UI_Target.GetComponent<TMP_Dropdown>();
            if (tmpDropdown == null) tmpDropdown = UI_Target.GetComponentInParent<TMP_Dropdown>();
            if (tmpDropdown != null)
            {
                ExecuteEvents.Execute(tmpDropdown.gameObject, pointerData, ExecuteEvents.pointerClickHandler);
                return true;
            }

            Button button = UI_Target.GetComponent<Button>();
            if (button == null) button = UI_Target.GetComponentInParent<Button>();
            if (button != null && button.interactable)
            {
                ExecuteEvents.Execute(button.gameObject, pointerData, ExecuteEvents.pointerClickHandler);
                return true; 
            }

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

    void TryPickUpObject() 
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            GameObject target = hit.collider.gameObject;
            string nameLower = target.name.ToLower();

            if (nameLower.Contains("wafer")) 
            {
                ExposureStationController aligner = Object.FindAnyObjectByType<ExposureStationController>();
                if (aligner != null && aligner.waferDockPosition != null && Vector3.Distance(target.transform.position, aligner.waferDockPosition.position) < 0.3f)
                {
                    if (aligner.isVacuumSealed)
                    {
                        Debug.LogWarning("Pickup Denied: Vacuum chuck active.");
                        return; 
                    }
                }

                HotplateController hotplate = Object.FindAnyObjectByType<HotplateController>();
                if (hotplate != null && hotplate.currentState != HotplateController.HotplateState.Baking)
                {
                    Collider[] hotplateHits = Physics.OverlapSphere(hotplate.transform.position + new Vector3(0f, 0.5f, 0f), 0.6f);
                    foreach (Collider col in hotplateHits)
                    {
                        if (col.gameObject == hit.collider.gameObject)
                        {
                            hotplate.currentState = HotplateController.HotplateState.Idle;
                            break;
                        }
                    }
                }
                
                FluidInteractionController liquid = target.GetComponentInParent<FluidInteractionController>();
                if (liquid == null) liquid = Object.FindAnyObjectByType<FluidInteractionController>(); 
                if (liquid != null && Vector3.Distance(target.transform.position, liquid.transform.position) < 0.8f)
                {
                    liquid.TriggerWaferInteractionSpike();
                }

                // --- FIX: DETECT IF THE WAFER IS CURRENTLY SEATED INSIDE A SPIN COATER CHUCK ---
                SpinCoaterController coaterCheck = target.GetComponentInParent<SpinCoaterController>();
                if (coaterCheck != null)
                {
                    // Force the coater to clear its state machine layers cleanly before we steal the transform
                    coaterCheck.RemoveWafer();
                }

                PickUpHeldObject(target);
                return;
            }
            
            if (nameLower.Contains("spin") || nameLower.Contains("coater") || nameLower.Contains("chuck") || nameLower.Contains("base"))
            {
                SpinCoaterController coater = target.GetComponentInParent<SpinCoaterController>();
                if (coater == null) coater = target.GetComponent<SpinCoaterController>();
                if (coater == null) coater = target.GetComponentInChildren<SpinCoaterController>();

                if (coater != null)
                {
                    GameObject extractedWafer = coater.RemoveWafer();
                    if (extractedWafer != null)
                    {
                        PickUpHeldObject(extractedWafer);
                        return; 
                    }
                }

                Collider[] collidersCloseBy = Physics.OverlapSphere(hit.point, 0.5f);
                foreach (Collider nearbyCollider in collidersCloseBy)
                {
                    if (nearbyCollider.gameObject.name.ToLower().Contains("wafer"))
                    {
                        PickUpHeldObject(nearbyCollider.gameObject);
                        if (coater != null) coater.RemoveWafer(); 
                        return;
                    }
                }
            }

            if (nameLower.Contains("mask") || nameLower.Contains("tray"))
            {
                ExposureStationController aligner = Object.FindAnyObjectByType<ExposureStationController>();
                if (aligner != null && aligner.isMaskTrayInserted)
                {
                    Debug.LogWarning("Cannot pick up tray while locked in housing.");
                    return; 
                }
                
                PickUpHeldObject(target);
                return;
            }
        }
    }

    void PickUpHeldObject(GameObject targetAsset)
    {
        heldObject = targetAsset;
        heldRigidbody = heldObject.GetComponent<Rigidbody>();

        if (heldRigidbody != null) heldRigidbody.isKinematic = true; 

        originalLayer = heldObject.layer;
        heldObject.layer = ignoreRaycastLayer;

        foreach (Transform child in heldObject.transform) {
            child.gameObject.layer = ignoreRaycastLayer;
        }

        // Snap it to the hand socket tree
        heldObject.transform.SetParent(waferHolderSocket);
        heldObject.transform.localPosition = Vector3.zero;
        heldObject.transform.localRotation = Quaternion.identity;

        if (heldObject.name.ToLower().Contains("wafer"))
        {
            heldObject.transform.localScale = new Vector3(0.65f, 0.013f, 0.65f);
        }
    }
    void TryPlaceObject()
    {
        if (heldObject == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            GameObject target = hit.collider.gameObject;
            string targetName = target.name.ToLower();
            HotplateController hotplate = target.GetComponentInParent<HotplateController>();

            // 2. Fallback check: If the hierarchy is split, find the script globally or via string match
            if (hotplate == null && targetName.Contains("top"))
            {
                hotplate = Object.FindAnyObjectByType<HotplateController>();
            }

            // If we successfully found the controller via either routing method
            if (hotplate != null)
            {
                if (heldObject.name.ToLower().Contains("mask")) return;

                if (hotplate.currentState == HotplateController.HotplateState.Baking)
                {
                    Debug.LogWarning("Placement Denied: Thermal zone active.");
                    return;
                }

                RestoreHeldObjectLayer();
                DetachHeldObject();
                
                // Hand over control to the hotplate script to handle alignment positioning
                hotplate.RegisterWaferPlacement(heldObject);

                heldObject = null;
                return;
            }

            // --- SPIN COATER INTERACTION CHECK ---
            if (targetName.Contains("spin") || targetName.Contains("coater") || targetName.Contains("chuck") || targetName.Contains("base"))
            {
                if (heldObject.name.ToLower().Contains("mask")) return;

                SpinCoaterController coater = target.GetComponentInParent<SpinCoaterController>();

                if (coater != null)
                {
                    RestoreHeldObjectLayer();
                    
                    GameObject objectToPlace = heldObject;
                    DetachHeldObject(); 

                    bool success = coater.RegisterWaferPlacement(objectToPlace);

                    if (success)
                    {
                        heldObject = null;
                    }
                    else
                    {
                        // Safely snap back to hand socket if coater rejects it (e.g., already loaded)
                        PickUpHeldObject(objectToPlace);
                    }
                    return;
                }
            }

            // --- EXPOSURE ALIGNER INTERACTION CHECK ---
            if (targetName.Contains("exposure") || targetName.Contains("aligner") || targetName.Contains("machine"))
            {
                ExposureStationController aligner = target.GetComponentInParent<ExposureStationController>();

                if (aligner != null)
                {
                    if (heldObject.name.ToLower().Contains("mask") || heldObject.name.ToLower().Contains("tray"))
                    {
                        RestoreHeldObjectLayer();
                        DetachHeldObject();
                        heldObject.transform.SetParent(aligner.transform);
                        heldObject.transform.localPosition = new Vector3(0.4f, 0f, 0.8f); 
                        heldObject.transform.localRotation = Quaternion.identity;
                        aligner.maskTrayTransform = heldObject.transform; 
                        aligner.isMaskTrayInserted = false;
                        heldObject = null;
                        return;
                    }
                    
                    if (heldObject.name.ToLower().Contains("wafer"))
                    {
                        RestoreHeldObjectLayer();
                        DetachHeldObject();
                        if (aligner.waferDockPosition != null)
                        {
                            heldObject.transform.position = aligner.waferDockPosition.position;
                            heldObject.transform.rotation = aligner.waferDockPosition.rotation;
                        }
                        else
                        {
                            heldObject.transform.position = target.transform.position + new Vector3(0f, 0.15f, 0f);
                            heldObject.transform.rotation = Quaternion.identity;
                        }
                        heldObject = null;
                        return;
                    }
                }
            }

            // --- BEAKER / FLUID ZONE INTERACTION CHECK ---
            if (targetName.Contains("beaker") || targetName.Contains("fluid") || targetName.Contains("chemical"))
            {
                if (heldObject.name.ToLower().Contains("wafer"))
                {
                    FluidInteractionController liquid = target.GetComponentInParent<FluidInteractionController>();

                    if (liquid != null)
                    {
                        RestoreHeldObjectLayer();
                        DetachHeldObject();

                        heldObject.transform.position = liquid.transform.position + new Vector3(0f, -0.2f, 0f);
                        heldObject.transform.rotation = Quaternion.identity;
                        heldObject.transform.SetParent(liquid.transform);
                        liquid.TriggerWaferInteractionSpike();

                        heldObject = null;
                        return;
                    }
                }
            }

            Debug.LogWarning($"Placement rejected: Targeting non-registered object '{target.name}'");
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