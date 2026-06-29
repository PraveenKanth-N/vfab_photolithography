using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class PostBakeButtonTrigger : MonoBehaviour
{
    private Button physicalButton;

    [Header("UI Elements")]
    public TMP_Text PB_time;
    public GameObject PB_image;

    public bool postBakeStarted = false;
    public bool postBakeComplete = false;

    void Start()
    {
        physicalButton = GetComponent<Button>();
        if (physicalButton != null)
        {
            // Wipe out any accidental direct links in the Inspector that bypass the timer!
            physicalButton.onClick.RemoveAllListeners();
            physicalButton.onClick.AddListener(StartCountdown);
        }
    }

    // Restored this method as public so the Unity Inspector's OnClick event can find it!
    public void TriggerPostBakeInferencePass()
    {
        StartCountdown();
    }

    void StartCountdown()
    {
        if (postBakeStarted) return;
        postBakeStarted = true;
        StartCoroutine(FastCountdownSequence());
    }

    private IEnumerator FastCountdownSequence()
    {
        if (physicalButton != null) physicalButton.interactable = false;

        float countdownTime = 60f;
        float speedUpFactor = 20f; // Completes 60s in about 3 real seconds

        while (countdownTime > 0)
        {
            countdownTime -= Time.deltaTime * speedUpFactor;
            if (countdownTime < 0) countdownTime = 0;

            if (PB_time != null)
                PB_time.text = $"Time: {Mathf.CeilToInt(countdownTime)}s";

            yield return null;
        }

        if (PB_time != null)
            PB_time.text = "Time: 0s";

        // Dynamically references your main global model manager to kick off Stage 2 calculations
        LithoInferenceEngine engine = Object.FindAnyObjectByType<LithoInferenceEngine>();
        if (engine != null)
        {
            engine.ExecutePostBakeChemicalPipeline();
            Debug.Log("[Cleanroom Interaction] Post-Bake chemical computing sequence fired successfully from hotplate terminal button.");
        }
        else
        {
            Debug.LogError("[Cleanroom Error] Could not trigger Post-Bake: LithoInferenceEngine instance missing in active scene context!");
        }

        // Show the PEB image only after baking and AI inference is complete
        if (PB_image != null) PB_image.SetActive(true);
        postBakeComplete = true;
        if (physicalButton != null) physicalButton.interactable = true;
    }
}