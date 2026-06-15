using UnityEngine;
using UnityEngine.UI;

public class PostBakeButtonTrigger : MonoBehaviour
{
    private Button physicalButton;

    void Start()
    {
        physicalButton = GetComponent<Button>();
        if (physicalButton != null)
        {
            physicalButton.onClick.AddListener(TriggerPostBakeInferencePass);
        }
    }

    void TriggerPostBakeInferencePass()
    {
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
    }
}