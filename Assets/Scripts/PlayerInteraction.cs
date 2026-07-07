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

    public GameObject heldObject = null;
    private Rigidbody heldRigidbody = null;
    
    private int originalLayer; 
    private int ignoreRaycastLayer;

    // --- HARD COORDINATE RESISTANCE TRACKING    // Memory for restoring rinse bottle
    private Transform originalBottleParent;
    private Vector3 originalBottleLocalPosition;
    private Vector3 originalBottleLocalScale;
    private Vector3 originalBottleWorldScale;
    private Quaternion originalBottleRotation; // Prevent weird orientations

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
    }

    public string lastDockPlacedName = "";

    void Update()
    {
        Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactionDistance, Color.red);

        if (CleanroomUIManager.Instance != null && CleanroomUIManager.Instance.IsAnyPanelOpen()) return;
        if (PhotomaskGalleryUI.Instance != null && PhotomaskGalleryUI.Instance.isGalleryOpen) return;
        if (HelpManualController.Instance != null && HelpManualController.Instance.isOpen) return;
        
        var mainWindow = Object.FindAnyObjectByType<MainWindowController>();
        if (mainWindow != null && mainWindow.IsPanelCurrentlyOpen()) return;
        
        WelcomeScreenController wsc = Object.FindAnyObjectByType<WelcomeScreenController>();
        if (wsc != null && wsc.gameObject.activeInHierarchy) return;

        WorkflowManager wm = Object.FindAnyObjectByType<WorkflowManager>();
        if (wm != null && wm.contextImgObj != null && wm.contextImgObj.activeInHierarchy) return;

        DeveloperUIManager devUI = Object.FindAnyObjectByType<DeveloperUIManager>();
        if (devUI != null && devUI.gameObject.activeInHierarchy) return;

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

            // Fallback: If we hit a UI element but it wasn't an interactive component (e.g. a background panel)
            // We should STILL block the click from passing through into the 3D world!
            if (UI_Target.layer == LayerMask.NameToLayer("UI"))
            {
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

            // --- 3. BOTTLE PICKUP (DIRECT & DOCK WRAPPER INTERCEPT) ---
            if (nameLower.Contains("dock") || nameLower.Contains("station") || nameLower.Contains("area") || nameLower.Contains("wet") || nameLower.Contains("bench") || nameLower.Contains("table") || nameLower.Contains("clean"))
            {
                foreach (Transform child in target.GetComponentsInChildren<Transform>())
                {
                    if (child != target.transform && (child.name.ToLower().Contains("bottle") || child.name.ToLower().Contains("rca") || child.name.ToLower().Contains("clean") || child.name.ToLower().Contains("dev") || child.name.ToLower().Contains("di")) && !child.name.ToLower().Contains("dock") && !child.name.ToLower().Contains("station") && !child.name.ToLower().Contains("area"))
                    {
                        PickUpHeldObject(child.gameObject);
                        return;
                    }
                }
            }

            if ((nameLower.Contains("bottle") || nameLower.Contains("rca") || nameLower.Contains("clean") || nameLower.Contains("dev") || nameLower.Contains("di")) && !nameLower.Contains("dock") && !nameLower.Contains("station") && !nameLower.Contains("area") && !nameLower.Contains("wet") && !nameLower.Contains("bench") && !nameLower.Contains("table"))
            {
                GameObject parentWrapper = target;
                if (target.transform.parent != null && (target.transform.parent.name.ToLower().Contains("bottle") || target.transform.parent.name.ToLower().Contains("rca") || target.transform.parent.name.ToLower().Contains("clean")) && !target.transform.parent.name.ToLower().Contains("dock"))
                {
                    parentWrapper = target.transform.parent.gameObject;
                }
                
                PickUpHeldObject(parentWrapper);
                return;
            }

            // --- 4. SPIN COATER GENUINE SUBSTRATE EXTRACTION ---
            if (nameLower.Contains("wafer") && !nameLower.Contains("carrier"))
            {
                SpinCoaterController coater = Object.FindAnyObjectByType<SpinCoaterController>();
                if (coater != null && coater.currentCoaterState != SpinCoaterController.CoaterState.Empty)
                {
                    Transform searchRoot = coater.coaterWaferAnchor != null ? coater.coaterWaferAnchor : coater.rotorSpindleChuck;
                    if (searchRoot != null && target.transform.IsChildOf(searchRoot))
                    {
                        if (coater.lidController != null && !coater.lidController.isOpen) return; // REJECT if lid closed

                        GameObject extractedWafer = coater.RemoveWafer();
                        if (extractedWafer != null) PickUpHeldObject(extractedWafer);
                        return;
                    }
                }
            }

            // --- 5. MASK ALIGNER SUBSTRATE/MASK HOVER PICKUP EXTRACTION ---
            if ((nameLower.Contains("wafer") && !nameLower.Contains("carrier")) || nameLower.Contains("mask") || nameLower.Contains("tray"))
            {
                ExposureStationController alignerController = Object.FindAnyObjectByType<ExposureStationController>();
                if (alignerController != null)
                {
                    Transform sharedWaferAnchorNode = alignerController.transform.Find("Hinge/Rotator/Chuck/Aligner_Wafer_Anchor");
                    if (sharedWaferAnchorNode == null) sharedWaferAnchorNode = alignerController.waferDockPosition;

                    if (sharedWaferAnchorNode != null && target.transform.IsChildOf(sharedWaferAnchorNode))
                    {
                        GameObject extractedObject = sharedWaferAnchorNode.GetChild(0).gameObject;
                        if (extractedObject.name.ToLower().Contains("mask")) alignerController.isMaskTrayInserted = false;
                        PickUpHeldObject(extractedObject);
                        return;
                    }
                }
            }

            // --- 4b. HOTPLATE SUBSTRATE EXTRACTION (clicking the hotplate top) ---
            if (nameLower.Contains("hotplate") || (nameLower.Contains("top") && !nameLower.Contains("toplid")))
            {
                HotplateController hp = target.GetComponentInParent<HotplateController>();
                if (hp == null || HotplateController.IsCleanerHotplate(hp)) hp = HotplateController.GetProcessingHotplate();
                
                if (hp != null && !HotplateController.IsCleanerHotplate(hp) && (hp.currentState == HotplateController.HotplateState.Complete || hp.currentState == HotplateController.HotplateState.WaferLoaded))
                {
                    Transform anchor = hp.hotplateWaferAnchor;
                    if (anchor != null && anchor.childCount > 0)
                    {
                        GameObject wafer = anchor.GetChild(0).gameObject;
                        PickUpHeldObject(wafer);
                        hp.currentState = HotplateController.HotplateState.Idle; // Reset state so workflow advances
                        return;
                    }
                }
            }

            // --- 1. STANDALONE WAFER PICKUP ---
            if (nameLower.Contains("wafer") && !nameLower.Contains("carrier")) 
            {
                PickUpHeldObject(target);
                
                // If it was on a hotplate, reset the hotplate
                HotplateController hp = target.GetComponentInParent<HotplateController>();
                if (hp != null) hp.currentState = HotplateController.HotplateState.Idle;
                
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
                    PickUpHeldObject(target);
                }
                return;
            }

            // --- 7. WAFER CARRIER SEQUENCE ---
            WaferCarrierController carrier = target.GetComponent<WaferCarrierController>();
            if (carrier == null) carrier = target.GetComponentInParent<WaferCarrierController>();

            if (carrier != null)
            {
                if (carrier.isAnimating) return; // Prevent spam clicking

                if (carrier.hasWafer && carrier.isReadyForExtraction)
                {
                    // Sequence: Carrier was moved and placed down. Now we extract!
                    carrier.ExtractWaferSequence((GameObject extracted) => {
                        if (extracted != null) PickUpHeldObject(extracted);
                    });
                }
                else
                {
                    // Sequence: Carrier is either empty, OR loaded but hasn't been transported yet.
                    PickUpHeldObject(carrier.gameObject);
                }
                return;
            }

            // --- 6. TOPLID PICKUP ---
            if (nameLower.Contains("toplid"))
            {
                GameObject lidToPickup = target;
                
                // If they clicked the storage furniture instead of the lid itself, find the lid!
                if (nameLower.Contains("storage"))
                {
                    lidToPickup = null;
                    foreach (Transform child in target.transform)
                    {
                        if (child.name.ToLower().Contains("toplid") && !child.name.ToLower().Contains("storage"))
                        {
                            lidToPickup = child.gameObject;
                            break;
                        }
                    }
                }

                if (lidToPickup != null)
                {
                    // Only allow pickup if no photomask is currently sitting on top of it in the chuck
                    bool isBlockedByMask = false;
                    if (lidToPickup.transform.parent != null && !lidToPickup.transform.parent.name.ToLower().Contains("storage"))
                    {
                        // We only care about mask blocking if it's in the Exposure Aligner Chuck!
                        foreach (Transform sibling in lidToPickup.transform.parent)
                        {
                            if (sibling.name.ToLower().Contains("mask")) isBlockedByMask = true;
                        }
                    }
                    
                    if (!isBlockedByMask)
                    {
                        PickUpHeldObject(lidToPickup);
                    }
                }
                return;
            }

            // --- 8. DEHYDRATION OVEN INTERACTION ---
            DehydrationOvenController oven = target.GetComponent<DehydrationOvenController>();
            if (oven == null) oven = target.GetComponentInParent<DehydrationOvenController>();
            if (oven == null && target.transform.parent != null) oven = target.transform.parent.GetComponentInParent<DehydrationOvenController>();
            if ((oven != null || nameLower.Contains("oven") || nameLower.Contains("dehydrat")) && !nameLower.Contains("interaction") && target.GetComponent<MachineInteractionAnchor>() == null)
            {
                if (oven == null) oven = Object.FindAnyObjectByType<DehydrationOvenController>();
                if (oven != null && !nameLower.Contains("wafer") && !nameLower.Contains("substrate") && !nameLower.Contains("bottle") && !nameLower.Contains("beaker") && !nameLower.Contains("interaction"))
                {
                    if (oven.isAnimating) return;
                    if (oven.hasWafer)
                    {
                        oven.ExtractWaferSequence((GameObject extracted) => {
                            if (extracted != null) PickUpHeldObject(extracted);
                        });
                    }
                    else
                    {
                        oven.ToggleOvenDoors();
                    }
                    return;
                }
            }
        }
    }

    void PickUpHeldObject(GameObject targetAsset)
    {
        if (heldObject == targetAsset) return;

        heldObject = targetAsset;
        heldRigidbody = heldObject.GetComponent<Rigidbody>();
        
        if (heldRigidbody == null) 
        {
            Rigidbody parentRb = heldObject.GetComponentInParent<Rigidbody>();
            if (parentRb != null && !parentRb.gameObject.name.ToLower().Contains("player") && !parentRb.gameObject.CompareTag("Player"))
            {
                heldRigidbody = parentRb;
            }
        }

        if (heldRigidbody != null) 
        {
            heldRigidbody.isKinematic = true; 
            heldRigidbody.detectCollisions = false; 
        }

        Collider[] allColliders = heldObject.GetComponentsInChildren<Collider>(true);
        foreach (Collider col in allColliders) 
        {
            col.enabled = false;
        }

        originalBottleParent = heldObject.transform.parent;
        originalBottleLocalPosition = heldObject.transform.localPosition;
        originalBottleLocalScale = heldObject.transform.localScale;
        originalBottleWorldScale = heldObject.transform.lossyScale;
        originalBottleRotation = heldObject.transform.rotation; // Capture exact world rotation

        Vector3 tableWorldScale = heldObject.transform.lossyScale;
        Quaternion tableWorldRotation = heldObject.transform.rotation;

        heldObject.transform.SetParent(waferHolderSocket, false);

        bool isBottleOrBeaker = heldObject.name.ToLower().Contains("bottle") || heldObject.name.ToLower().Contains("beaker") || heldObject.name.ToLower().Contains("flask") || heldObject.name.ToLower().Contains("rca");
        if (heldObject.name.ToLower().Contains("substrate") && !isBottleOrBeaker)
        {
            heldObject.transform.localPosition = new Vector3(0.2f, -0.25f, 0.45f); 
            heldObject.transform.rotation = tableWorldRotation;
            
            heldObject.transform.localScale = new Vector3(
                tableWorldScale.x / waferHolderSocket.lossyScale.x,
                tableWorldScale.y / waferHolderSocket.lossyScale.y,
                tableWorldScale.z / waferHolderSocket.lossyScale.z
            );
        }
        else if (heldObject.name.ToLower().Contains("carrier"))
        {
            // Do not aggressively flatten the carrier! Keep it proportional.
            heldObject.transform.localPosition = new Vector3(0.3f, -0.2f, 0.5f);
            heldObject.transform.localRotation = Quaternion.identity;
            
            // Reconstruct absolute world scale matching
            heldObject.transform.localScale = new Vector3(
                tableWorldScale.x / waferHolderSocket.lossyScale.x,
                tableWorldScale.y / waferHolderSocket.lossyScale.y,
                tableWorldScale.z / waferHolderSocket.lossyScale.z
            );
        }
        else if (isBottleOrBeaker)
        {
            heldObject.transform.localPosition = new Vector3(0.2f, -0.2f, 0.5f);
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

        // Notify all placement indicators in the scene to check if they should pulse
        PlacementPulseIndicator[] indicators = Object.FindObjectsByType<PlacementPulseIndicator>(FindObjectsSortMode.None);
        foreach (var indicator in indicators)
        {
            indicator.CheckAndTogglePulse(heldObject.name);
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
            bool isBottle = heldObject.name.ToLower().Contains("bottle");
            bool isDI = heldObject.name.ToLower().Contains("di");
            
            foreach (Transform child in heldObject.GetComponentsInChildren<Transform>(true))
            {
                string cname = child.name.ToLower();
                if (cname.Contains("bottle")) isBottle = true;
                if (cname.Contains("di")) isDI = true;
            }

            if (isBottle)
            {
                FluidInteractionController pourEngine = target.GetComponent<FluidInteractionController>();
                if (pourEngine == null) pourEngine = target.GetComponentInParent<FluidInteractionController>();
                if (pourEngine == null) pourEngine = target.GetComponentInChildren<FluidInteractionController>(true);

                // CASE A: Pour mechanism injection target matches
                if (pourEngine != null || targetName.Contains("beaker") || targetName.Contains("fluid") || targetName.Contains("chemical"))
                {
                    if (pourEngine != null)
                    {
                        DeveloperUIManager devUI = Object.FindAnyObjectByType<DeveloperUIManager>();
                        if (devUI != null)
                        {
                            if (isDI && (pourEngine == devUI.developerFluid || pourEngine.name.ToLower().Contains("dev") || targetName.Contains("dev") || targetName.Contains("right")))
                            {
                                Debug.LogWarning("[Chemical Interlock] Cannot pour DI water into Developer beaker!");
                                return;
                            }
                            if (!isDI && (pourEngine == devUI.diFluid || pourEngine.name.ToLower().Contains("di") || targetName.Contains("di") || targetName.Contains("left")))
                            {
                                Debug.LogWarning("[Chemical Interlock] Cannot pour Developer solution into DI water beaker!");
                                return;
                            }
                        }
                        pourEngine.ExecuteChemicalPour(heldObject);
                    }
                    return;
                }

                // CASE B: Workbench landing target triggers
                if (targetName.Contains("developer") || targetName.Contains("station") || targetName.Contains("table") || targetName.Contains("desk") || targetName.Contains("bench") || targetName.Contains("dock") || targetName.Contains("clean") || targetName.Contains("rca") || targetName.Contains("area") || targetName.Contains("wet"))
                {
                    RestoreHeldObjectLayer();
                    DetachHeldObject(); 

                    bool isSC1 = heldObject.name.ToLower().Contains("sc") || heldObject.name.ToLower().Contains("clean") || heldObject.name.ToLower().Contains("solvent") || heldObject.name.ToLower().Contains("piranha") || heldObject.name.ToLower().Contains("acetone") || heldObject.name.ToLower().Contains("rca");
                    bool isDev = !isSC1 && (heldObject.name.ToLower().Contains("dev") || heldObject.name.ToLower().Contains("developer"));

                    if (originalBottleParent != null)
                    {
                        heldObject.transform.SetParent(originalBottleParent, true);
                        heldObject.transform.rotation = originalBottleRotation;
                        heldObject.transform.localPosition = originalBottleLocalPosition;
                        
                        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
                        if (rb == null) rb = heldObject.GetComponentInParent<Rigidbody>();
                        if (rb != null) rb.isKinematic = true;

                        Debug.Log($"[Direct Return Success] Restored {heldObject.name} back to original parent {originalBottleParent.name}.");
                    }
                    else
                    {
                        GameObject dockObj = null;
                        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                        {
                            if (isDI && t.name.Equals("DI_Water_Station_Dock", System.StringComparison.OrdinalIgnoreCase)) { dockObj = t.gameObject; break; }
                            else if (isSC1 && !t.name.ToLower().Contains("carrier") && !t.name.ToLower().Contains("wafer") && (t.name.IndexOf("SC1", System.StringComparison.OrdinalIgnoreCase) >= 0 || t.name.IndexOf("Clean", System.StringComparison.OrdinalIgnoreCase) >= 0 || t.name.IndexOf("Solvent", System.StringComparison.OrdinalIgnoreCase) >= 0 || t.name.IndexOf("Acetone", System.StringComparison.OrdinalIgnoreCase) >= 0 || t.name.IndexOf("Piranha", System.StringComparison.OrdinalIgnoreCase) >= 0 || t.name.IndexOf("RCA", System.StringComparison.OrdinalIgnoreCase) >= 0) && t.name.IndexOf("Dock", System.StringComparison.OrdinalIgnoreCase) >= 0) { dockObj = t.gameObject; break; }
                            else if (isDev && t.name.Equals("Dev_Bottle_Station_Dock", System.StringComparison.OrdinalIgnoreCase)) { dockObj = t.gameObject; break; }
                        }

                        if (dockObj != null)
                        {
                            heldObject.transform.SetParent(dockObj.transform, true); // Keep world transform during parent swap
                            heldObject.transform.rotation = originalBottleRotation;  // Snap to exact original upright rotation
                            heldObject.transform.localPosition = Vector3.zero;       // Snap exactly to the dock's center
                            
                            // Stop physics engine from tipping the bottle over!
                            Rigidbody rb = heldObject.GetComponent<Rigidbody>();
                            if (rb == null) rb = heldObject.GetComponentInParent<Rigidbody>();
                            if (rb != null) rb.isKinematic = true;

                            Debug.Log($"[Direct Return Success] Restored {heldObject.name} back to {dockObj.name}.");
                        }
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
            if (HotplateController.IsCleanerHotplate(targetHotplate)) targetHotplate = null;

            // Hotplate targeting fallback parser
            if (targetHotplate == null && (targetName.Contains("hotplate") || (targetName.Contains("top") && !targetName.Contains("toplid"))))
            {
                targetHotplate = HotplateController.GetProcessingHotplate();
            }

            if (targetName.Contains("spin") || targetName.Contains("coater")) targetAligner = null;

            bool isHeldWafer = heldObject.name.ToLower().Contains("wafer") && !heldObject.name.ToLower().Contains("carrier");

            // --- ROUTE A: EXPOSURE ALIGNER STATION ---
            if (targetAligner != null)
            {
                Transform sharedWaferAnchorNode = targetAligner.transform.Find("Hinge/Rotator/Chuck/Aligner_Wafer_Anchor");
                if (sharedWaferAnchorNode == null) sharedWaferAnchorNode = targetAligner.waferDockPosition;

                if (sharedWaferAnchorNode != null)
                {
                    bool hasTopLid = false;
                    foreach (Transform child in sharedWaferAnchorNode) { if (child.name.ToLower().Contains("toplid")) hasTopLid = true; }

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

                    if (isHeldWafer)
                    {
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

            // --- ROUTE C: WAFER CARRIER SEQUENCE ---
            WaferCarrierController carrier = target.GetComponent<WaferCarrierController>();
            if (carrier == null) carrier = target.GetComponentInParent<WaferCarrierController>();

            if (carrier != null && isHeldWafer)
            {
                if (carrier.isAnimating || carrier.hasWafer) return;
                
                // Sequence 1: Holding wafer, click empty carrier -> Insert sequence
                GameObject waferToInsert = heldObject;
                RestoreHeldObjectLayer(); DetachHeldObject();
                heldObject = null;
                
                carrier.InsertWaferSequence(waferToInsert);
                return;
            }

            if (targetName.Contains("carrier_dock") && heldObject.name.ToLower().Contains("carrier"))
            {
                lastDockPlacedName = target.name;
                
                // Sequence: Place carrier onto dock
                Transform hinge = heldObject.transform.Find("Hinge");
                if (hinge != null)
                {
                    hinge.localRotation = Quaternion.Euler(Vector3.zero); // Close lid
                }

                // Snap into position
                RestoreHeldObjectLayer(); DetachHeldObject();
                heldObject.transform.SetParent(target.transform, false);
                heldObject.transform.localPosition = Vector3.zero;
                heldObject.transform.localRotation = Quaternion.identity;

                // Advance the sequence state if it's loaded!
                WaferCarrierController heldCarrier = heldObject.GetComponent<WaferCarrierController>();
                if (heldCarrier != null && heldCarrier.hasWafer)
                {
                    heldCarrier.isReadyForExtraction = true;
                }

                heldObject = null;
                return;
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
            if (targetCoater != null && isHeldWafer)
            {
                RestoreHeldObjectLayer();
                GameObject objectToPlace = heldObject;
                DetachHeldObject(); 
                heldObject = null; // Clear the reference so PickUpHeldObject doesn't abort early
                if (!targetCoater.RegisterWaferPlacement(objectToPlace)) 
                {
                    PickUpHeldObject(objectToPlace); // Recover the wafer back to the player's hand
                }
                return;
            }

            // --- ROUTE D: POLOS HOTPLATE UNIT ---
            if (targetHotplate != null && isHeldWafer)
            {
                if (targetHotplate.currentState == HotplateController.HotplateState.Baking) return;
                RestoreHeldObjectLayer(); DetachHeldObject();
                targetHotplate.RegisterWaferPlacement(heldObject);
                heldObject = null;
                return;
            }

            // --- ROUTE D2: DEHYDRATION OVEN INTERACTION ---
            DehydrationOvenController targetOven = target.GetComponentInParent<DehydrationOvenController>();
            if (targetOven == null && (targetName.Contains("oven") || targetName.Contains("dehydrat")) && !targetName.Contains("interaction") && target.GetComponent<MachineInteractionAnchor>() == null)
            {
                targetOven = Object.FindAnyObjectByType<DehydrationOvenController>();
            }

            if (targetOven != null && !targetName.Contains("interaction") && target.GetComponent<MachineInteractionAnchor>() == null)
            {
                if (targetOven.isAnimating) return;
                if (isHeldWafer && !targetOven.hasWafer)
                {
                    RestoreHeldObjectLayer();
                    GameObject waferToInsert = heldObject;
                    DetachHeldObject();
                    heldObject = null;
                    targetOven.InsertWaferSequence(waferToInsert);
                    return;
                }
                else if (!targetOven.isOpen)
                {
                    targetOven.ToggleOvenDoors();
                    return;
                }
            }

            // --- ROUTE E: CHEMICAL DEVELOPMENT BEAKER DISPLACEMENT ---
            bool isWafer = isHeldWafer || (heldObject.name.ToLower().Contains("substrate") && !isBottle);
            
            if (isWafer)
            {
                FluidInteractionController liquid = target.GetComponent<FluidInteractionController>();
                if (liquid == null) liquid = target.GetComponentInParent<FluidInteractionController>();
                if (liquid == null) liquid = target.GetComponentInChildren<FluidInteractionController>(true);
                
                if (liquid != null || targetName.Contains("beaker") || targetName.Contains("fluid") || targetName.Contains("chemical"))
                {
                    if (liquid != null)
                    {
                        RestoreHeldObjectLayer(); DetachHeldObject();
                        heldObject.transform.position = liquid.transform.position + new Vector3(0f, -0.1f, 0f);
                        heldObject.transform.rotation = Quaternion.identity;
                        heldObject.transform.SetParent(liquid.transform);
                        liquid.TriggerWaferInteractionSpike();
                        heldObject = null;
                    }
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
        
        Collider[] allColliders = heldObject.GetComponentsInChildren<Collider>(true);
        foreach (Collider col in allColliders) 
        {
            col.enabled = true;
        }

        heldObject.transform.SetParent(null);
        if (heldRigidbody != null) 
        {
            heldRigidbody.isKinematic = false;
            heldRigidbody.detectCollisions = true; 
        }

        // Turn off all placement indicators when item is dropped
        PlacementPulseIndicator[] indicators = Object.FindObjectsByType<PlacementPulseIndicator>(FindObjectsSortMode.None);
        foreach (var indicator in indicators)
        {
            indicator.CheckAndTogglePulse(null);
        }
    }

    public void ForcePickUpBakedMask(GameObject bakedMask) { PickUpHeldObject(bakedMask); }
}