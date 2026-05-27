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
                
                // --- INTEGRATION: CHECK IF WAFER IS INSIDE A BEAKER ON PICKUP ---
                FluidInteractionController liquid = target.GetComponentInParent<FluidInteractionController>();
                if (liquid == null) liquid = Object.FindAnyObjectByType<FluidInteractionController>(); 
                if (liquid != null && Vector3.Distance(target.transform.position, liquid.transform.position) < 0.8f)
                {
                    // Trigger removal splash surge!
                    liquid.TriggerWaferInteractionSpike();
                }

                PickUpHeldObject(target);
                return;
            }
            
            if (nameLower.Contains("spin") || nameLower.Contains("coater") || nameLower.Contains("bottom") || nameLower.Contains("hotplate") || nameLower.Contains("beaker"))
            {
                Collider[] collidersCloseBy = Physics.OverlapSphere(hit.point, 0.5f);
                foreach (Collider nearbyCollider in collidersCloseBy)
                {
                    if (nearbyCollider.gameObject.name.ToLower().Contains("wafer"))
                    {
                        // --- INTEGRATION: TRIGGER SPLASH ON RADIAL AREA EXTRACTION ---
                        FluidInteractionController liquid = target.GetComponentInChildren<FluidInteractionController>();
                        if (liquid == null) liquid = target.GetComponent<FluidInteractionController>();
                        if (liquid != null) liquid.TriggerWaferInteractionSpike();

                        PickUpHeldObject(nearbyCollider.gameObject);
                        
                        SpinCoaterController coater = target.GetComponentInParent<SpinCoaterController>();
                        if (coater == null) coater = target.GetComponent<SpinCoaterController>();
                        if (coater != null) coater.currentCoaterState = SpinCoaterController.CoaterState.Empty;
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

        heldObject.transform.SetParent(waferHolderSocket);
        heldObject.transform.localPosition = Vector3.zero;
        heldObject.transform.localRotation = Quaternion.identity;
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

            // Inside Case 1: Spin Coater
            if (targetName.Contains("spin") || targetName.Contains("coater"))
            {
                if (heldObject.name.ToLower().Contains("mask")) return;

                SpinCoaterController coater = target.GetComponent<SpinCoaterController>();
                if (coater == null) coater = target.GetComponentInParent<SpinCoaterController>();

                if (coater != null)
                {
                    RestoreHeldObjectLayer();
                    DetachHeldObject();
                    coater.LoadWaferIntoCoater(); 
                    heldObject.transform.position = target.transform.position + new Vector3(0f, 0.2f, 0f);
                    heldObject.transform.rotation = Quaternion.identity; 
                    heldObject = null;
                    return;
                }
            }
            
            // Inside Case 2: Hotplate
            if (targetName.Contains("hotplate"))
            {
                RestoreHeldObjectLayer();
                DetachHeldObject();
                heldObject.transform.position = target.transform.position + new Vector3(0f, 0.25f, 0f);
                heldObject.transform.rotation = Quaternion.identity;
                
                HotplateController hotplate = target.GetComponent<HotplateController>();
                if (hotplate != null) hotplate.RegisterWaferPlacement(heldObject);

                heldObject = null;
                return;
            }

            // Inside Case 3: Exposure Aligner Machine
            if (targetName.Contains("exposure") || targetName.Contains("aligner") || targetName.Contains("machine"))
            {
                if (heldObject.name.ToLower().Contains("mask") || heldObject.name.ToLower().Contains("tray"))
                {
                    ExposureStationController aligner = target.GetComponent<ExposureStationController>();
                    if (aligner == null) aligner = target.GetComponentInParent<ExposureStationController>();

                    if (aligner != null)
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
                }
                
                if (heldObject.name.ToLower().Contains("wafer"))
                {
                    ExposureStationController aligner = target.GetComponent<ExposureStationController>();
                    if (aligner == null) aligner = target.GetComponentInParent<ExposureStationController>();

                    if (aligner != null)
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

            // --- INSIDE NEW CASE 4: THE DEVELOPER BEAKER DROPPER ZONE ---
            if (targetName.Contains("beaker") || targetName.Contains("fluid") || targetName.Contains("chemical"))
            {
                if (heldObject.name.ToLower().Contains("wafer"))
                {
                    // Find the fluid controller component on the target beaker structure
                    FluidInteractionController liquid = target.GetComponentInChildren<FluidInteractionController>();
                    if (liquid == null) liquid = target.GetComponent<FluidInteractionController>();
                    if (liquid == null) liquid = target.GetComponentInParent<FluidInteractionController>();

                    if (liquid != null)
                    {
                        RestoreHeldObjectLayer();
                        DetachHeldObject();

                        // 1. Physically seat the wafer flat inside the beaker's core base
                        heldObject.transform.position = liquid.transform.position + new Vector3(0f, -0.2f, 0f);
                        heldObject.transform.rotation = Quaternion.identity;

                        // 2. Set parent to the beaker so if you pick up/slide the beaker, the wafer moves with it!
                        heldObject.transform.SetParent(liquid.transform);

                        // 3. Trigger our newly exposed explicit surge spike function!
                        liquid.TriggerWaferInteractionSpike();

                        heldObject = null;
                        Debug.Log("[INTERACTION] Wafer cleanly submerged into chemical solution context.");
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