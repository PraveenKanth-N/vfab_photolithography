using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePortalTrigger : MonoBehaviour
{
    [Header("Scene Destination")]
    [Tooltip("The exact name of the Unity Scene to load (must be added in Build Settings)")]
    public string targetSceneName = "MasterScene";

    [Header("Trigger Options")]
    [Tooltip("If true, walking into this object's Trigger Collider will start loading")]
    public bool loadOnTriggerEnter = true;

    [Tooltip("Time in seconds to wait before loading (useful for playing door sounds or fade animations)")]
    public float delayBeforeLoad = 0.5f;

    private bool isTransitioning = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!loadOnTriggerEnter || isTransitioning) return;

        // Check if the colliding object is the Player or character controller
        if (other.CompareTag("Player") || other.GetComponentInParent<CharacterController>() != null || other.name.ToLower().Contains("player"))
        {
            StartSceneTransition();
        }
    }

    // This can also be called directly from a UI Button click or an OnMouseDown / interaction event!
    public void StartSceneTransition()
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        isTransitioning = true;
        Debug.Log($"[ScenePortal] Triggered! Transitioning to scene: {targetSceneName} in {delayBeforeLoad} seconds...");

        if (delayBeforeLoad > 0f)
        {
            yield return new WaitForSeconds(delayBeforeLoad);
        }

        // Standard scene load: completely frees up current scene memory and loads destination
        SceneManager.LoadScene(targetSceneName);
    }
}
