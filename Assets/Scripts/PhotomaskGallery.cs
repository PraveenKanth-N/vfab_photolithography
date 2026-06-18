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

    void Awake()
    {
        Instance = this;
        gameObject.SetActive(false); 
        cameraLookScript = Object.FindAnyObjectByType<MouseLook>();

        for (int i = 0; i < maskSelectionButtons.Length; i++)
        {
            int index = i; 
            maskSelectionButtons[i].onClick.AddListener(() => SelectMaskPattern(index));

            RawImage buttonImage = maskSelectionButtons[i].GetComponentInChildren<RawImage>();
            if (buttonImage != null && lithoEngine != null)
            {
                buttonImage.texture = lithoEngine.GenerateStaticPreviewTexture(index);
            }
        }
    }

    public void OpenGallery(GameObject physicalMaskPlate)
    {
        interactingMaskObject = physicalMaskPlate;
        
        // 1. Flip tracking property flags FIRST before passing commands to look calculations
        isGalleryOpen = true;
        gameObject.SetActive(true);
        
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
        gameObject.SetActive(false);
        isGalleryOpen = false;

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