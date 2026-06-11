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

        // FREEZE FIREWALL GATE: If a standard system panel is visible OR our custom collage gallery is open,
        // completely shut down world crosshair click processing loops instantly!
        if (CleanroomUIManager.Instance != null && CleanroomUIManager.Instance.IsAnyPanelOpen()) return;
        if (PhotomaskGalleryUI.Instance != null && PhotomaskGalleryUI.Instance.isGalleryOpen) return;
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

            ExposureStationController aligner = target.GetComponentInParent<ExposureStationController>();
            if (aligner == null) aligner = Object.FindAnyObjectByType<ExposureStationController>();

            // --- DEEP CASE SCAN: CLICKED MASK ALIGNER FRAME DIRECTLY WHILE EMPTY HANDED ---
            if (aligner != null && (nameLower.Contains("aligner") || nameLower.Contains("machine") || nameLower.Contains("chuck") || nameLower.Contains("dock") || nameLower.Contains("hinge")))
            {
                Transform anchorNode = aligner.transform.Find("Hinge/Rotator/Chuck/Aligner_Wafer_Anchor");
                if (anchorNode == null) anchorNode = aligner.waferDockPosition;

                if (anchorNode != null && anchorNode.childCount > 0)
                {
                    // Scan the anchor for nested targets to pull out cleanly
                    Transform nestedItem = null;
                    for (int i = 0; i < anchorNode.childCount; i++)
                    {
                        Transform child = anchorNode.GetChild(i);
                        if (child.name.ToLower().Contains("mask") || child.name.ToLower().Contains("wafer") || child.name.ToLower().Contains("tray"))
                        {
                            nestedItem = child;
                            break;
                        }
                    }

                    if (nestedItem != null)
                    {
                        if (aligner.isVacuumSealed)
                        {
                            Debug.LogWarning("Pickup Denied: Vacuum seals are active inside Aligner housing.");
                            return;
                        }

                        aligner.isMaskTrayInserted = false;
                        PickUpHeldObject(nestedItem.gameObject);
                        return;
                    }
                }
            }

            // Standard direct object click processing fallbacks
            if (nameLower.Contains("wafer")) 
            {
                PickUpHeldObject(target);
                return;
            }
            
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
                    if (aligner != null) aligner.isMaskTrayInserted = false; 
                    PickUpHeldObject(target);
                }
                return;
            }

            // Spin coater direct fetch
            SpinCoaterController coater = target.GetComponentInParent<SpinCoaterController>();
            if (coater != null)
            {
                GameObject extractedWafer = coater.RemoveWafer();
                if (extractedWafer != null) PickUpHeldObject(extractedWafer);
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
        heldObject.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);
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

            SpinCoaterController targetCoater = target.GetComponentInParent<SpinCoaterController>();
            ExposureStationController targetAligner = target.GetComponentInParent<ExposureStationController>();

            // --- DETECT INTERFERENCE: STOP ALIGNER FROM STEALING SPIN COATER CLICK TARGETS ---
            if (targetName.Contains("spin") || targetName.Contains("coater")) targetAligner = null;

            // --- TRACK A: EXPLICIT SPIN COATER PROCESSING ---
            if (targetCoater != null)
            {
                if (heldObject.name.ToLower().Contains("mask")) return;

                RestoreHeldObjectLayer();
                GameObject objectToPlace = heldObject;
                DetachHeldObject(); 

                if (targetCoater.RegisterWaferPlacement(objectToPlace))
                {
                    heldObject = null;
                }
                else
                {
                    PickUpHeldObject(objectToPlace);
                }
                return;
            }

            // --- TRACK B: EXPLICIT MASK ALIGNER PROCESSING ---
            if (targetAligner != null)
            {
                Transform sharedWaferAnchorNode = targetAligner.transform.Find("Hinge/Rotator/Chuck/Aligner_Wafer_Anchor");
                if (sharedWaferAnchorNode == null) sharedWaferAnchorNode = targetAligner.waferDockPosition;

                if (sharedWaferAnchorNode != null)
                {
                    if (heldObject.name.ToLower().Contains("mask") || heldObject.name.ToLower().Contains("tray"))
                    {
                        if (targetAligner.isVacuumSealed) return;

                        RestoreHeldObjectLayer();
                        DetachHeldObject();

                        heldObject.transform.SetParent(sharedWaferAnchorNode, false);
                        heldObject.transform.localPosition = new Vector3(0f, 0.015f, 0f); 
                        heldObject.transform.localRotation = Quaternion.identity;
                        heldObject.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);

                        targetAligner.isMaskTrayInserted = true;
                        heldObject = null;
                        return;
                    }

                    if (heldObject.name.ToLower().Contains("wafer"))
                    {
                        RestoreHeldObjectLayer();
                        DetachHeldObject();

                        heldObject.transform.SetParent(sharedWaferAnchorNode, false);
                        heldObject.transform.localPosition = Vector3.zero; 
                        heldObject.transform.localRotation = Quaternion.identity;
                        heldObject.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);
                        heldObject = null;
                        return;
                    }
                }
            }

            // --- TRACK C: EXPLICIT PHOTOMASK STORAGE DOCKING ---
            if (targetName.Contains("storage") || targetName.Contains("dock"))
            {
                if (heldObject.name.ToLower().Contains("mask") || heldObject.name.ToLower().Contains("tray"))
                {
                    RestoreHeldObjectLayer();
                    DetachHeldObject();
                    
                    Transform storageDockNode = target.transform.Find("Photomask_Dock");
                    if (storageDockNode == null && target.transform.parent != null)
                    {
                        storageDockNode = target.transform.parent.Find("Photomask_Dock");
                    }

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
                    heldObject = null;
                    return;
                }
            }

            // --- TRACK D: HOTPLATE STATION PROCESSING ---
            HotplateController hotplate = target.GetComponentInParent<HotplateController>();
            if (hotplate == null && targetName.Contains("top")) hotplate = Object.FindAnyObjectByType<HotplateController>();

            if (hotplate != null && heldObject.name.ToLower().Contains("wafer"))
            {
                if (hotplate.currentState == HotplateController.HotplateState.Baking) return;

                RestoreHeldObjectLayer();
                DetachHeldObject();
                hotplate.RegisterWaferPlacement(heldObject);
                heldObject = null;
                return;
            }

            // --- TRACK E: BEAKER CHEMICAL STATIONS ---
            if (targetName.Contains("beaker") || targetName.Contains("fluid") || targetName.Contains("chemical"))
            {
                if (heldObject.name.ToLower().Contains("wafer"))
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