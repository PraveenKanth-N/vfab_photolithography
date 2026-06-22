using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class PhotomaskGalleryUI : MonoBehaviour
{
    public static PhotomaskGalleryUI Instance { get; private set; }

    [Header("Engine Link")]
    public LithoInferenceEngine lithoEngine;

    [Header("Gallery Buttons")]
    public Button[] maskSelectionButtons;

    private GameObject interactingMaskObject;
    private MouseLook cameraLookScript;
    public bool isGalleryOpen = false; // Live state tracking property for PlayerInteraction to read
    private Canvas rootCanvas;

    void Awake()
    {
        Instance = this;
        
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null) rootCanvas.gameObject.SetActive(false);
        else gameObject.SetActive(false);

        cameraLookScript = Object.FindAnyObjectByType<MouseLook>();

        for (int i = 0; i < maskSelectionButtons.Length; i++)
        {
            int index = i; 
            maskSelectionButtons[i].onClick.AddListener(() => SelectMaskPattern(index));
        }
        
        RefreshThumbnails();
    }

    public void RefreshThumbnails()
    {
        if (maskSelectionButtons == null || lithoEngine == null) return;
        
        for (int i = 0; i < maskSelectionButtons.Length; i++)
        {
            RawImage buttonImage = maskSelectionButtons[i].GetComponentInChildren<RawImage>();
            if (buttonImage != null)
            {
                buttonImage.texture = lithoEngine.GenerateStaticPreviewTexture(i);
            }
        }
    }

    public void OpenGallery(GameObject physicalMaskPlate)
    {
        interactingMaskObject = physicalMaskPlate;
        
        // 1. Flip tracking property flags FIRST before passing commands to look calculations
        isGalleryOpen = true;
        
        if (rootCanvas != null) rootCanvas.gameObject.SetActive(true);
        else gameObject.SetActive(true);
        
        // 2. Break camera look tracking states cleanly
        if (cameraLookScript != null)
        {
            cameraLookScript.ForceUnlockForUI();
        }
        
        // 3. Absolute direct hardware override enforcement
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SelectMaskPattern(int maskIndex)
    {
        if (lithoEngine != null)
        {
            lithoEngine.maskDropdownIndexOverride = maskIndex; 
            lithoEngine.BakeSelectedMaskMatrix();
            // Removed dynamic painting so the photomask keeps its static glass material
        }

        interactingMaskObject.name = $"PR_Photomask_Index_{maskIndex}";
        
        // 1. Drop active tracking state parameters
        isGalleryOpen = false;
        
        if (rootCanvas != null) rootCanvas.gameObject.SetActive(false);
        else gameObject.SetActive(false);

        // 2. Restore standard look inputs
        if (cameraLookScript != null)
        {
            cameraLookScript.ForceLockFromUI();
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        PlayerInteraction player = Object.FindAnyObjectByType<PlayerInteraction>();
        if (player != null)
        {
            player.ForcePickUpBakedMask(interactingMaskObject);
        }
    }
}