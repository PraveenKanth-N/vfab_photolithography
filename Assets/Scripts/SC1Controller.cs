// SC1Controller.cs  -  Machine 1: SC1 Wet Bench / Cleaner_hotplate
// Hooks up to screen-space Canvas UI.
// Attach to: SC1 UI Panel or Cleaner Controller GameObject
// Assign in Inspector: outputText, startButton (optional: wafer, cleaningBeakerFluid)

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SC1Controller : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI outputText;
    public Button startButton;
    public Button closeButton; // Optional close button

    [Header("Beaker / Fluid Interlock")]
    [Tooltip("Reference to the fluid controller on the cleaning beaker. Auto-discovered if left empty.")]
    public FluidInteractionController cleaningBeakerFluid;

    [Header("Optional Animation Anchor")]
    [Tooltip("Optional wafer transform if you want visual dipping animation.")]
    public Transform wafer;

    [Header("Recipe Settings")]
    public float bathTempC = 80f;
    [Tooltip("Total dip time in seconds. Default is 600s (10 min).")]
    public float dipTimeSeconds = 600f;
    [Tooltip("Real time in seconds it takes to simulate the full dip time in Unity.")]
    public float realTimeTestDuration = 3.5f;
    public float etchRate = 4.0f;       // Å/min  [Watanabe 1983]
    public float refDepth = 40.0f;      // Å — critical undercut depth

    const string THETA_DISPLAY = "< 6°";

    private Vector3 waferHome;
    private bool running = false;
    private float elapsed = 0f;
    public bool isComplete = false;

    void Awake()
    {
        // Remove any duplicated HotplateController on the cleaner hotplate so it doesn't conflict with processing hotplates
        foreach (HotplateController hp in GetComponentsInChildren<HotplateController>(true))
        {
            Destroy(hp);
        }
        HotplateController parentHp = GetComponentInParent<HotplateController>();
        if (parentHp != null) Destroy(parentHp);
    }

    void Start()
    {
        if (wafer != null) waferHome = wafer.position;

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(StartClean);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseUI);
        }

        GetBeakerFluid();
        ShowStandby();
    }

    void OnEnable()
    {
        if (!running) ShowStandby();
    }

    private FluidInteractionController GetBeakerFluid()
    {
        if (cleaningBeakerFluid != null) return cleaningBeakerFluid;

        // Auto-discover fluid controller on wet bench / cleaning beaker
        foreach (FluidInteractionController fic in Object.FindObjectsByType<FluidInteractionController>(FindObjectsSortMode.None))
        {
            string fname = (fic.name + " " + fic.gameObject.name).ToLower();
            if (fname.Contains("clean") || fname.Contains("sc") || fname.Contains("rca") || fname.Contains("beaker") || fname.Contains("bath"))
            {
                if (!fname.Contains("dev") && !fname.Contains("di"))
                {
                    cleaningBeakerFluid = fic;
                    return fic;
                }
            }
        }
        return null;
    }

    void Update()
    {
        if (!running)
        {
            // Check fluid interlock dynamically while the menu is open waiting for the player to pour!
            FluidInteractionController fluid = GetBeakerFluid();
            if (fluid != null && !fluid.hasPoured)
            {
                if (startButton != null && startButton.interactable) startButton.interactable = false;
                if (outputText != null && !outputText.text.Contains("BEAKER EMPTY"))
                {
                    outputText.text =
                        "SC1 WET BENCH\n" +
                        "─────────────────────────────\n" +
                        "Bath composition : NH₄OH : H₂O₂ : H₂O = 1 : 1 : 5\n" +
                        $"Bath temperature : {bathTempC}°C  (hotplate, fixed)\n" +
                        $"Dip time         : {dipTimeSeconds / 60f:F1} min  (standard recipe)\n" +
                        "─────────────────────────────\n" +
                        "BEAKER EMPTY: Please pour SC1 chemical solution into the cleaning beaker before starting bath.";
                }
            }
            else
            {
                if (startButton != null && !startButton.interactable)
                {
                    startButton.interactable = true;
                    ShowStandby();
                }
            }
            return;
        }
        
        // Fast-forward simulation: scale real time by ratio so 600s recipe completes in realTimeTestDuration (e.g. 3.5s)
        float timeScale = (realTimeTestDuration > 0f) ? (dipTimeSeconds / realTimeTestDuration) : 1f;
        elapsed += Time.deltaTime * timeScale;
        UpdateDisplay(elapsed);
        
        if (elapsed >= dipTimeSeconds)
        {
            CleanDone();
        }
    }

    void ShowStandby()
    {
        if (outputText == null) return;
        
        FluidInteractionController fluid = GetBeakerFluid();
        bool isPoured = (fluid == null || fluid.hasPoured);

        if (startButton != null) startButton.interactable = isPoured;

        if (!isPoured)
        {
            outputText.text =
                "SC1 WET BENCH\n" +
                "─────────────────────────────\n" +
                "Bath composition : NH4OH : H2O2 : H2O = 1 : 1 : 5\n" +
                $"Bath temperature : {bathTempC} C  (hotplate, fixed)\n" +
                $"Dip time         : {dipTimeSeconds / 60f:F1} min  (standard recipe)\n" +
                "BEAKER EMPTY: Please pour SC1 chemical solution into the cleaning beaker before starting bath.";
        }
        else
        {
            outputText.text =
                "SC1 WET BENCH\n" +
                "─────────────────────────────\n" +
                "Bath composition : NH4OH : H2O2 : H2O = 1 : 1 : 5\n" +
                $"Bath temperature : {bathTempC} C  (hotplate, fixed)\n" +
                $"Dip time         : {dipTimeSeconds / 60f:F1} min  (standard recipe)\n" +
                "Press Start to begin clean";
        }
    }

    void UpdateDisplay(float t)
    {
        if (outputText == null) return;

        float depth = Mathf.Min(etchRate * (t / 60f), refDepth);
        float removal = Mathf.Clamp01(t / dipTimeSeconds) * 100f;

        outputText.text =
            "SC1 WET BENCH  —  Running\n" +
            "─────────────────────────────\n" +
            $"Elapsed       {t:F0} / {dipTimeSeconds:F0} s\n" +
            $"Etch depth    {depth:F1} / {refDepth:F0} Å\n" +
            $"Removal       {removal:F1} %\n" +
            "─────────────────────────────\n" +
            (removal >= 99.9f
                ? "Complete — lifting substrate"
                : "Cleaning in progress…");
    }

    void StartClean()
    {
        FluidInteractionController fluid = GetBeakerFluid();
        if (fluid != null && !fluid.hasPoured)
        {
            Debug.LogWarning("[SC1 Controller] Cannot start clean: beaker is empty!");
            return;
        }

        isComplete = false;
        running = true;
        elapsed = 0f;
        if (startButton != null) startButton.interactable = false;
        
        if (wafer != null)
        {
            StopAllCoroutines();
            StartCoroutine(MoveWafer(waferHome, waferHome + Vector3.down * 0.2f));
        }

        Debug.Log("[SC1 Controller] Clean cycle started.");
    }

    void CleanDone()
    {
        isComplete = true;
        running = false;
        if (outputText != null)
        {
            outputText.text =
                "SC1 WET BENCH  —  Complete\n" +
                "─────────────────────────────\n" +
                $"Etch depth     {refDepth} Å  \n" +
                "Removal        100 %  \n" +
                $"Contact angle  {THETA_DISPLAY}  (Si–OH, hydrophilic)\n" +
                "CLEAN, Proceed to HMDS prime / Oven";
        }

        if (startButton != null) startButton.interactable = true;

        if (wafer != null)
        {
            StopAllCoroutines();
            StartCoroutine(MoveWafer(wafer.position, waferHome));
        }

        Debug.Log("[SC1 Controller] Clean cycle completed successfully.");
    }

    public void ResetCycle()
    {
        running = false;
        elapsed = 0f;
        ShowStandby();
    }

    public void CloseUI()
    {
        if (CleanroomUIManager.Instance != null)
        {
            CleanroomUIManager.Instance.CloseActivePanel();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    IEnumerator MoveWafer(Vector3 from, Vector3 to)
    {
        float t = 0f;
        while (t < 1.5f)
        {
            t += Time.deltaTime;
            wafer.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / 1.5f));
            yield return null;
        }
        wafer.position = to;
    }
}
