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
            if (CleanroomUIManager.Instance != null && CleanroomUIManager.Instance.IsAnyPanelOpen())
            {
                return;
            }

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

            if (nameLower.Contains("beaker") || nameLower.Contains("fluid") || nameLower.Contains("chemical") || nameLower.Contains("mesh"))
            {
                Transform foundWafer = null;
                for (int i = 0; i < target.transform.childCount; i++)
                {
                    Transform child = target.transform.GetChild(i);
                    if (child.name.ToLower().Contains("wafer"))
                    {
                        foundWafer = child;
                        break;
                    }
                }

                if (foundWafer == null && target.transform.parent != null)
                {
                    foreach (Transform siblingOrChild in target.transform.parent.GetComponentsInChildren<Transform>())
                    {
                        if (siblingOrChild.name.ToLower().Contains("wafer"))
                        {
                            foundWafer = siblingOrChild;
                            break;
                        }
                    }
                }

                if (foundWafer != null)
                {
                    target = foundWafer.gameObject;
                    nameLower = target.name.ToLower();
                }
            }

            // --- WAFER PICKUP CHECK ---
            if (nameLower.Contains("wafer")) 
            {
                ExposureStationController aligner = Object.FindAnyObjectByType<ExposureStationController>();
                if (aligner != null && aligner.waferDockPosition != null && Vector3.Distance(target.transform.position, aligner.waferDockPosition.position) < 0.5f)
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

                SpinCoaterController coaterCheck = target.GetComponentInParent<SpinCoaterController>();
                if (coaterCheck != null)
                {
                    coaterCheck.RemoveWafer();
                }

                PickUpHeldObject(target);
                return;
            }
            
            // --- SPIN COATER STATION GRAB ---
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

            // --- PHOTOMASK INTERACTION & PICKUP ---
            if (nameLower.Contains("mask") || nameLower.Contains("tray"))
            {
                ExposureStationController aligner = Object.FindAnyObjectByType<ExposureStationController>();
                if (aligner != null && aligner.isVacuumSealed)
                {
                    Debug.LogWarning("Pickup Denied: Mask is currently vacuum-sealed inside the exposure housing assembly.");
                    return; 
                }
                
                // FIXED CHECK: Look for either 'storage' or your new empty 'dock' anchor container parent!
                bool isSittingOnStorageCube = target.transform.parent != null && 
                                             (target.transform.parent.name.ToLower().Contains("storage") || 
                                              target.transform.parent.name.ToLower().Contains("dock"));

                if (isSittingOnStorageCube)
                {
                    // Photomask on storage anchor -> Click it -> Pop open selection layout panel menu
                    Debug.Log("[Sequence Log] Interacting with mask on Photomask_Dock. Triggering design collage gallery UI...");
                    PhotomaskGalleryUI.Instance.OpenGallery(target);
                }
                else
                {
                    // From Aligner or anywhere else: Pull back cleanly into hand socket
                    if (aligner != null) aligner.isMaskTrayInserted = false; 
                    PickUpHeldObject(target);
                }
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

        if (heldObject.name.ToLower().Contains("wafer") || heldObject.name.ToLower().Contains("mask") || heldObject.name.ToLower().Contains("tray"))
        {
            heldObject.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);
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

            // =====================================================================
            // 1. GLOBAL MASK ALIGNER HOUSING INTERACTION PASSTHROUGH
            // =====================================================================
            if (targetName.Contains("chuck") || targetName.Contains("dock") || 
                targetName.Contains("aligner") || targetName.Contains("machine") || 
                targetName.Contains("anchor") || targetName.Contains("hinge") || targetName.Contains("rotator"))
            {
                ExposureStationController aligner = target.GetComponentInParent<ExposureStationController>();
                if (aligner == null) aligner = Object.FindAnyObjectByType<ExposureStationController>();

                if (aligner != null)
                {
                    // Find the single absolute destination target node: Aligner_Wafer_Anchor
                    Transform sharedWaferAnchorNode = aligner.transform.Find("Hinge/Rotator/Chuck/Aligner_Wafer_Anchor");
                    
                    // Fallback directly to the controller's exposed script slot if layout tracing breaks
                    if (sharedWaferAnchorNode == null) sharedWaferAnchorNode = aligner.waferDockPosition;

                    if (sharedWaferAnchorNode != null)
                    {
                        // ---- SUB-ROUTE A: PLACING THE PHOTOMASK RIGHT ON THE WAFER ANCHOR DOCK ----
                        if (heldObject.name.ToLower().Contains("mask") || heldObject.name.ToLower().Contains("tray"))
                        {
                            if (aligner.isVacuumSealed)
                            {
                                Debug.LogWarning("Placement Denied: Vacuum chuck is pressurized!");
                                return;
                            }

                            RestoreHeldObjectLayer();
                            DetachHeldObject();

                            heldObject.transform.SetParent(sharedWaferAnchorNode, false);
                            
                            // Stack it slightly above the wafer coordinate space plane on the local Y axis
                            heldObject.transform.localPosition = new Vector3(0f, 0.015f, 0f); 
                            heldObject.transform.localRotation = Quaternion.identity;
                            heldObject.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);

                            aligner.isMaskTrayInserted = true;

                            Rigidbody maskRb = heldObject.GetComponent<Rigidbody>();
                            if (maskRb != null) maskRb.isKinematic = true;

                            Debug.Log($"[Sequence Log] Photomask successfully stacked onto Aligner_Wafer_Anchor via element: {target.name}");
                            heldObject = null;
                            return;
                        }

                        // ---- SUB-ROUTE B: PLACING THE WAFER RIGHT ON THE WAFER ANCHOR DOCK ----
                        if (heldObject.name.ToLower().Contains("wafer"))
                        {
                            RestoreHeldObjectLayer();
                            DetachHeldObject();

                            heldObject.transform.SetParent(sharedWaferAnchorNode, false);
                            
                            // Sit it completely flush on the local base center of the anchor node
                            heldObject.transform.localPosition = Vector3.zero;
                            heldObject.transform.localRotation = Quaternion.identity;
                            heldObject.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);

                            Rigidbody waferRb = heldObject.GetComponent<Rigidbody>();
                            if (waferRb != null) waferRb.isKinematic = true;

                            Debug.Log($"[Sequence Log] Wafer successfully snapped flat onto Aligner_Wafer_Anchor via element: {target.name}");
                            heldObject = null;
                            return;
                        }
                    }
                }
            }

            // =====================================================================
            // 2. STORAGE CUBE PHOTOMASK SELECTION ROUTING
            // =====================================================================
            if (targetName.Contains("storage") || targetName.Contains("dock"))
                {
                    RestoreHeldObjectLayer();
                    DetachHeldObject();
                    
                    // Look for your new explicit empty anchor node inside or next to the storage object
                    Transform storageDockNode = target.transform.Find("Photomask_Dock");
                    if (storageDockNode == null && target.transform.parent != null)
                    {
                        storageDockNode = target.transform.parent.Find("Photomask_Dock");
                    }

                    // Parent to the unscaled dock anchor if found, fallback to target mesh if not
                    if (storageDockNode != null)
                    {
                        heldObject.transform.SetParent(storageDockNode, false);
                        heldObject.transform.localPosition = Vector3.zero;
                    }
                    else
                    {
                        heldObject.transform.SetParent(target.transform, false);
                        heldObject.transform.localPosition = new Vector3(0f, 0.6f, 0f); 
                    }

                    heldObject.transform.localRotation = Quaternion.identity;
                    heldObject.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);

                    Rigidbody maskRb = heldObject.GetComponent<Rigidbody>();
                    if (maskRb != null) maskRb.isKinematic = true;

                    Debug.Log($"[Sequence Log] Photomask safely docked at storage anchor: {heldObject.transform.parent.name}");
                    heldObject = null;
                    return;
                }

            // =====================================================================
            // 3. HOTPLATE & SPIN COATER WAFER INTERACTION CHECKS
            // =====================================================================
            if (heldObject.name.ToLower().Contains("wafer"))
            {
                // --- HOTPLATE ROUTING ---
                HotplateController hotplate = target.GetComponentInParent<HotplateController>();
                if (hotplate == null && targetName.Contains("top"))
                {
                    hotplate = Object.FindAnyObjectByType<HotplateController>();
                }

                if (hotplate != null)
                {
                    if (hotplate.currentState == HotplateController.HotplateState.Baking)
                    {
                        Debug.LogWarning("Placement Denied: Thermal zone active.");
                        return;
                    }

                    RestoreHeldObjectLayer();
                    DetachHeldObject();
                    hotplate.RegisterWaferPlacement(heldObject);
                    heldObject = null;
                    return;
                }

                // --- SPIN COATER ROUTING ---
                if (targetName.Contains("spin") || targetName.Contains("coater") || targetName.Contains("chuck") || targetName.Contains("base"))
                {
                    SpinCoaterController coater = target.GetComponentInParent<SpinCoaterController>();
                    if (coater != null)
                    {
                        RestoreHeldObjectLayer();
                        GameObject objectToPlace = heldObject;
                        DetachHeldObject(); 

                        if (coater.RegisterWaferPlacement(objectToPlace))
                        {
                            heldObject = null;
                        }
                        else
                        {
                            PickUpHeldObject(objectToPlace);
                        }
                        return;
                    }
                }

                // --- BEAKER / FLUID DEVELOPMENT ROUTING ---
                if (targetName.Contains("beaker") || targetName.Contains("fluid") || targetName.Contains("chemical"))
                {
                    FluidInteractionController liquid = target.GetComponentInParent<FluidInteractionController>();
                    if (liquid != null)
                    {
                        RestoreHeldObjectLayer();
                        DetachHeldObject();

                        heldObject.transform.position = liquid.transform.position + new Vector3(0f, -0.1f, 0f);
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

    public void ForcePickUpBakedMask(GameObject bakedMask)
    {
        PickUpHeldObject(bakedMask);
    }
}