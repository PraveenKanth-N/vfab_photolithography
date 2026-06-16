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

    // --- HARD COORDINATE RESISTANCE TRACKING MATRIX ---
    private Transform originalBottleParent = null;
    private Vector3 originalBottleLocalScale;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
    }

    void Update()
    {
        Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactionDistance, Color.red);

        if (CleanroomUIManager.Instance != null && CleanroomUIManager.Instance.IsAnyPanelOpen()) return;
        if (PhotomaskGalleryUI.Instance != null && PhotomaskGalleryUI.Instance.isGalleryOpen) return;
        
        var mainWindow = Object.FindAnyObjectByType<MainWindowController>();
        if (mainWindow != null && mainWindow.IsPanelCurrentlyOpen()) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

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
                    if (!results[i].gameObject.name.Contains("Blocker")) { UI_Target = results[i].gameObject; break; }
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

            // Beaker sub-wafer intercept safety layer
            if (nameLower.Contains("beaker") || nameLower.Contains("fluid") || nameLower.Contains("chemical") || nameLower.Contains("mesh"))
            {
                Transform foundWafer = null;
                for (int i = 0; i < target.transform.childCount; i++)
                {
                    Transform child = target.transform.GetChild(i);
                    if (child.name.ToLower().Contains("wafer")) { foundWafer = child; break; }
                }
                if (foundWafer == null && target.transform.parent != null)
                {
                    foreach (Transform siblingOrChild in target.transform.parent.GetComponentsInChildren<Transform>())
                    {
                        if (siblingOrChild.name.ToLower().Contains("wafer")) { foundWafer = siblingOrChild; break; }
                    }
                }
                if (foundWafer != null) { target = foundWafer.gameObject; nameLower = target.name.ToLower(); }
            }

            // --- 1. STANDALONE WAFER PICKUP ---
            if (nameLower.Contains("wafer")) 
            {
                PickUpHeldObject(target);
                return;
            }
            
            // --- 2. PHOTOMASK PICKUP ---
            if (nameLower.Contains("mask") || nameLower.Contains("tray"))
            {
                bool isSittingOnStorageCube = target.transform.parent != null && 
                                             (target.transform.parent.name.ToLower().Contains("storage") || 
                                              target.transform.parent.name.ToLower().Contains("dock"));

                if (isSittingOnStorageCube)
                {
                    PhotomaskGalleryUI.Instance.OpenGallery(target);
                }
                else
                {
                    ExposureStationController aligner = Object.FindAnyObjectByType<ExposureStationController>();
                    if (aligner != null) aligner.isMaskTrayInserted = false; 
                    PickUpHeldObject(target);
                }
                return;
            }

            // --- 3. RINSE BOTTLE PICKUP (HIERARCHY WRAPPER CONTEXT) ---
            if (nameLower.Contains("bottle"))
            {
                GameObject parentWrapper = target.transform.parent != null ? target.transform.parent.gameObject : target;
                PickUpHeldObject(parentWrapper);
                return;
            }

            // --- 4. SPIN COATER GENUINE SUBSTRATE EXTRACTION ---
            SpinCoaterController coater = target.GetComponentInParent<SpinCoaterController>();
            if (coater != null)
            {
                GameObject extractedWafer = coater.RemoveWafer();
                if (extractedWafer != null) PickUpHeldObject(extractedWafer);
                return;
            }

            // --- 5. MASK ALIGNER SUBSTRATE HOVER PICKUP EXTRACTION ---
            ExposureStationController alignerController = target.GetComponentInParent<ExposureStationController>();
            if (alignerController != null)
            {
                Transform sharedWaferAnchorNode = alignerController.transform.Find("Hinge/Rotator/Chuck/Aligner_Wafer_Anchor");
                if (sharedWaferAnchorNode == null) sharedWaferAnchorNode = alignerController.waferDockPosition;

                if (sharedWaferAnchorNode != null && sharedWaferAnchorNode.childCount > 0)
                {
                    GameObject extractedObject = sharedWaferAnchorNode.GetChild(0).gameObject;
                    if (extractedObject.name.ToLower().Contains("mask")) alignerController.isMaskTrayInserted = false;
                    PickUpHeldObject(extractedObject);
                    return;
                }
            }

            // --- 6. TOPLID PICKUP ---
            if (nameLower.Contains("toplid"))
            {
                // Only allow pickup if no photomask is currently sitting on top of it in the chuck
                bool isBlockedByMask = false;
                if (target.transform.parent != null)
                {
                    foreach (Transform sibling in target.transform.parent)
                    {
                        if (sibling.name.ToLower().Contains("mask")) isBlockedByMask = true;
                    }
                }
                
                if (!isBlockedByMask)
                {
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
        if (heldRigidbody == null) heldRigidbody = heldObject.GetComponentInParent<Rigidbody>();

        if (heldRigidbody != null) 
        {
            heldRigidbody.isKinematic = true; 
            heldRigidbody.detectCollisions = false; 
        }

        Collider objCollider = heldObject.GetComponent<Collider>();
        if (objCollider == null) objCollider = heldObject.GetComponentInChildren<Collider>();
        if (objCollider != null) objCollider.enabled = false;

        foreach (Transform child in heldObject.transform) {
            Collider childCol = child.GetComponent<Collider>();
            if (childCol != null) childCol.enabled = false;
        }

        originalBottleParent = heldObject.transform.parent;
        originalBottleLocalScale = heldObject.transform.localScale;

        Vector3 tableWorldScale = heldObject.transform.lossyScale;
        Quaternion tableWorldRotation = heldObject.transform.rotation;

        heldObject.transform.SetParent(waferHolderSocket, false);

        if (heldObject.name.ToLower().Contains("substrate"))
        {
            heldObject.transform.localPosition = new Vector3(0.2f, -0.25f, 0.45f); 
            heldObject.transform.rotation = tableWorldRotation;
            
            heldObject.transform.localScale = new Vector3(
                tableWorldScale.x / waferHolderSocket.lossyScale.x,
                tableWorldScale.y / waferHolderSocket.lossyScale.y,
                tableWorldScale.z / waferHolderSocket.lossyScale.z
            );
        }
        else
        {
            heldObject.transform.localPosition = Vector3.zero;
            heldObject.transform.localRotation = Quaternion.identity;
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
            // 1. RINSE BOTTLE SECTOR PROCESSING TRACK
            // =====================================================================
            if (heldObject.name.ToLower().Contains("substrate"))
            {
                bool isDI = heldObject.name.ToLower().Contains("di");

                // CASE A: Pour mechanism injection target matches
                if (targetName.Contains("beaker") || targetName.Contains("fluid") || targetName.Contains("chemical"))
                {
                    FluidInteractionController pourEngine = target.GetComponent<FluidInteractionController>();
                    if (pourEngine == null) pourEngine = target.GetComponentInParent<FluidInteractionController>();
                    if (pourEngine == null) pourEngine = Object.FindAnyObjectByType<FluidInteractionController>();

                    if (pourEngine != null)
                    {
                        pourEngine.ExecuteChemicalPour(heldObject);
                    }
                    return;
                }

                // CASE B: Workbench landing target triggers
                if (targetName.Contains("developer") || targetName.Contains("station") || targetName.Contains("table") || targetName.Contains("desk") || targetName.Contains("bench"))
                {
                    RestoreHeldObjectLayer();
                    DetachHeldObject(); 

                    GameObject mainStationObj = GameObject.Find("Station_developer");
                    if (mainStationObj == null) mainStationObj = GameObject.Find("Station_Developer");

                    if (mainStationObj != null)
                    {
                        heldObject.transform.SetParent(mainStationObj.transform, false);
                        heldObject.transform.localRotation = Quaternion.identity;
                        heldObject.transform.localScale = originalBottleLocalScale;

                        if (isDI)
                        {
                            heldObject.transform.localPosition = new Vector3(778f, -280f, 290f);
                        }
                        else
                        {
                            heldObject.transform.localPosition = new Vector3(797f, -278.8f, 291f);
                        }
                        Debug.Log($"[Direct Return Success] Restored {heldObject.name} back to exact hand-tested workbench transforms.");
                    }
                    else
                    {
                        heldObject.transform.SetParent(originalBottleParent, false);
                        heldObject.transform.localScale = originalBottleLocalScale;
                    }

                    heldObject = null; 
                    return;
                }
                return; 
            }

            // =====================================================================
            // 2. HARDWARE EQUIPMENT INTERLOCK ROUTES (WAFER / MASKS)
            // =====================================================================
            SpinCoaterController targetCoater = target.GetComponentInParent<SpinCoaterController>();
            ExposureStationController targetAligner = target.GetComponentInParent<ExposureStationController>();
            HotplateController targetHotplate = target.GetComponentInParent<HotplateController>();

            // Hotplate targeting fallback parser
            if (targetHotplate == null && (targetName.Contains("hotplate") || targetName.Contains("top")))
            {
                targetHotplate = Object.FindAnyObjectByType<HotplateController>();
            }

            if (targetName.Contains("spin") || targetName.Contains("coater")) targetAligner = null;

            // --- ROUTE A: EXPOSURE ALIGNER STATION ---
            if (targetAligner != null)
            {
                Transform sharedWaferAnchorNode = targetAligner.transform.Find("Hinge/Rotator/Chuck/Aligner_Wafer_Anchor");
                if (sharedWaferAnchorNode == null) sharedWaferAnchorNode = targetAligner.waferDockPosition;

                if (sharedWaferAnchorNode != null)
                {
                    if (heldObject.name.ToLower().Contains("mask") || heldObject.name.ToLower().Contains("tray"))
                    {
                        if (targetAligner.isVacuumSealed) return;
                        RestoreHeldObjectLayer(); DetachHeldObject();
                        heldObject.transform.SetParent(sharedWaferAnchorNode, false);
                        heldObject.transform.localPosition = new Vector3(0f, 0.015f, 0f); 
                        heldObject.transform.localRotation = Quaternion.identity;
                        heldObject.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);
                        heldObject = null;
                        return;
                    }

                    if (heldObject.name.ToLower().Contains("wafer"))
                    {
                        // Check if TopLid is still on the chuck!
                        bool hasTopLid = false;
                        foreach (Transform child in sharedWaferAnchorNode) { if (child.name.ToLower().Contains("toplid")) hasTopLid = true; }
                        
                        if (hasTopLid) return; // Cannot place wafer until TopLid is removed!

                        RestoreHeldObjectLayer(); DetachHeldObject();
                        heldObject.transform.SetParent(sharedWaferAnchorNode, false);
                        heldObject.transform.localPosition = Vector3.zero; 
                        heldObject.transform.localRotation = Quaternion.identity;
                        heldObject.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);
                        heldObject = null;
                        return;
                    }

                    if (heldObject.name.ToLower().Contains("toplid"))
                    {
                        if (targetAligner.isVacuumSealed) return;
                        RestoreHeldObjectLayer(); DetachHeldObject();
                        heldObject.transform.SetParent(sharedWaferAnchorNode, false);
                        heldObject.transform.localPosition = Vector3.zero;
                        heldObject.transform.localRotation = Quaternion.identity;
                        heldObject.transform.localScale = Vector3.one;
                        heldObject = null;
                        return;
                    }
                }
            }

            // --- ROUTE B: PHOTOMASK HOUSING RACKS ---
            if ((targetName.Contains("storage") || targetName.Contains("dock")) && (heldObject.name.ToLower().Contains("mask") || heldObject.name.ToLower().Contains("tray")))
            {
                RestoreHeldObjectLayer(); DetachHeldObject();
                Transform storageDockNode = target.transform.Find("Photomask_Dock");
                if (storageDockNode == null && target.transform.parent != null) storageDockNode = target.transform.parent.Find("Photomask_Dock");

                if (storageDockNode != null) { heldObject.transform.SetParent(storageDockNode, false); heldObject.transform.localPosition = Vector3.zero; }
                else { heldObject.transform.SetParent(target.transform, false); heldObject.transform.localPosition = new Vector3(0f, 0.6f, 0f); }

                heldObject.transform.localRotation = Quaternion.identity;
                heldObject.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);
                heldObject = null;
                return;
            }

            // --- ROUTE C: POLOS SPIN COATER ---
            if (targetCoater != null && heldObject.name.ToLower().Contains("wafer"))
            {
                RestoreHeldObjectLayer();
                GameObject objectToPlace = heldObject;
                DetachHeldObject(); 
                if (targetCoater.RegisterWaferPlacement(objectToPlace)) heldObject = null;
                else PickUpHeldObject(objectToPlace);
                return;
            }

            // --- ROUTE D: POLOS HOTPLATE UNIT ---
            if (targetHotplate != null && heldObject.name.ToLower().Contains("wafer"))
            {
                if (targetHotplate.currentState == HotplateController.HotplateState.Baking) return;
                RestoreHeldObjectLayer(); DetachHeldObject();
                targetHotplate.RegisterWaferPlacement(heldObject);
                heldObject = null;
                return;
            }

            // --- ROUTE E: CHEMICAL DEVELOPMENT BEAKER DISPLACEMENT ---
            if ((targetName.Contains("beaker") || targetName.Contains("fluid") || targetName.Contains("chemical")) && heldObject.name.ToLower().Contains("wafer"))
            {
                FluidInteractionController liquid = target.GetComponentInParent<FluidInteractionController>();
                if (liquid == null) liquid = Object.FindAnyObjectByType<FluidInteractionController>();
                
                if (liquid != null)
                {
                    RestoreHeldObjectLayer(); DetachHeldObject();
                    heldObject.transform.position = liquid.transform.position + new Vector3(0f, -0.1f, 0f);
                    heldObject.transform.rotation = Quaternion.identity;
                    heldObject.transform.SetParent(liquid.transform);
                    liquid.TriggerWaferInteractionSpike();
                    heldObject = null;
                    return;
                }
            }

            // --- ROUTE F: TOPLID STORAGE ---
            if (targetName.Contains("toplidstorage") && heldObject.name.ToLower().Contains("toplid"))
            {
                RestoreHeldObjectLayer(); DetachHeldObject();
                heldObject.transform.SetParent(target.transform, false);
                heldObject.transform.localPosition = Vector3.zero;
                heldObject.transform.localRotation = Quaternion.identity;
                heldObject.transform.localScale = Vector3.one;
                heldObject = null;
                return;
            }
        }
    }

    void RestoreHeldObjectLayer()
    {
        if (heldObject == null) return;
        heldObject.layer = originalLayer;
        foreach (Transform child in heldObject.transform) { child.gameObject.layer = originalLayer; }
    }

    void DetachHeldObject()
    {
        if (heldObject == null) return;
        
        Collider objCollider = heldObject.GetComponent<Collider>();
        if (objCollider == null) objCollider = heldObject.GetComponentInChildren<Collider>();
        if (objCollider != null) objCollider.enabled = true;

        foreach (Transform child in heldObject.transform) {
            Collider childCol = child.GetComponent<Collider>();
            if (childCol != null) childCol.enabled = true;
        }

        heldObject.transform.SetParent(null);
        if (heldRigidbody != null) 
        {
            heldRigidbody.isKinematic = false;
            heldRigidbody.detectCollisions = true; 
        }
    }

    public void ForcePickUpBakedMask(GameObject bakedMask) { PickUpHeldObject(bakedMask); }
}