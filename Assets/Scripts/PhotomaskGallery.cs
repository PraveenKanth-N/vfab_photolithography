using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhotomaskGalleryUI : MonoBehaviour
{
    public static PhotomaskGalleryUI Instance { get; private set; }

    [Header("Engine Link")]
    public LithoInferenceEngine lithoEngine;

    [Header("Gallery Buttons (Assign all 5 in order)")]
    public Button[] maskSelectionButtons;

    private GameObject interactingMaskObject;

    void Awake()
    {
        Instance = this;
        gameObject.SetActive(false); 

        // Loop through and automatically assign the graphic previews and click listeners
        for (int i = 0; i < maskSelectionButtons.Length; i++)
        {
            int index = i; 
            maskSelectionButtons[i].onClick.AddListener(() => SelectMaskPattern(index));

            // Find the RawImage component inside the button to hold the visual stencil preview
            RawImage buttonImage = maskSelectionButtons[i].GetComponentInChildren<RawImage>();

            if (buttonImage != null && lithoEngine != null)
            {
                // Generate a dedicated, isolated high-contrast texture preview for this slot
                buttonImage.texture = lithoEngine.GenerateStaticPreviewTexture(index);
            }
            else if (buttonImage == null)
            {
                Debug.LogWarning($"[Gallery UI] Could not find RawImage component inside Button index {index}! Add a RawImage child to the button.");
            }
        }
    }

    public void OpenGallery(GameObject physicalMaskPlate)
    {
        interactingMaskObject = physicalMaskPlate;
        gameObject.SetActive(true);
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SelectMaskPattern(int maskIndex)
    {
        // 1. Fire up the Inference Engine matrix generator arrays
        if (lithoEngine != null)
        {
            lithoEngine.maskDropdownIndexOverride = maskIndex; 
            lithoEngine.BakeSelectedMaskMatrix();
        }

        // Rename the object so you can tell it has a data tracking profile embedded
        interactingMaskObject.name = $"PR_Photomask_Index_{maskIndex}";

        // 2. Shut down selection panel UI overlays cleanly
        gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 3. COMPLETE THE CYCLE: Put the newly baked mask straight into your hand socket!
        PlayerInteraction player = Object.FindAnyObjectByType<PlayerInteraction>();
        if (player != null)
        {
            player.ForcePickUpBakedMask(interactingMaskObject);
            Debug.Log($"[Sequence Log] Design [{maskIndex}] baked successfully. Mask equipped in player hand.");
        }
    }
}