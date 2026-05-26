using UnityEngine;
using UnityEngine.UI;

public class MicroscopeInterfaceManager : MonoBehaviour
{
    public static MicroscopeInterfaceManager Instance; // Singleton for easy access across controllers

    [Header("Chamber References")]
    public GameObject atomicChamberRoot;
    public Camera microscopeCamera;

    [Header("UI Overlay Canvas Panel")]
    public GameObject microscopeScreenOverlay;

    private bool isChamberActive = false;

    private void Awake()
    {
        // Setup Singleton instance
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {   
        // Start completely hidden
        SetChamberState(false);
    }

    public void ToggleMicroscopeView()
    {
        SetChamberState(!isChamberActive);
    }

    public void SetChamberState(bool setActive) {
        isChamberActive = setActive;
        atomicChamberRoot.SetActive(setActive);
        microscopeCamera.enabled = setActive;
        
        if (microscopeScreenOverlay != null)
        {
            microscopeScreenOverlay.SetActive(setActive);
        }
    }
}