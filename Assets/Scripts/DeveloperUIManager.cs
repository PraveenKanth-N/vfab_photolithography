using UnityEngine;
using TMPro;
using System.Collections;
using System.Text.RegularExpressions;

public class DeveloperUIManager : MonoBehaviour
{
    [Header("Fluid Controllers (Drag the actual Fluid Mesh children here)")]
    public FluidInteractionController developerFluid;
    public FluidInteractionController diFluid;

    [Header("Time Display (Drag your TMP_Text here)")]
    public TMP_Text swerveTimeText;

    [Header("Pattern Rendering")]
    [Tooltip("Drag the Assets/Materials/StaticPhotomaskMat.mat here!")]
    public Material developedPatternMaterial;

    private float cachedSwerveTime = 30f;
    private float targetEndTime = 0f;
    private float swerveStartTime = 0f;
    private bool decalSpawned = false;
    private bool isCountingDown = false;
    private bool pendingRestore = false;
    private float restoreTime = 0f;

    private void OnEnable()
    {
        // If the UI is reopened and the timer finished while it was closed
        if (isCountingDown && Time.time >= targetEndTime)
        {
            isCountingDown = false;
            RestoreTimeText();
        }
        else if (pendingRestore && Time.time >= restoreTime)
        {
            pendingRestore = false;
            RestoreTimeText();
        }
    }

    private void Update()
    {
        if (isCountingDown)
        {
            // Spawn pattern after exactly 5 seconds of swerving
            if (!decalSpawned && (Time.time - swerveStartTime) >= 5.0f)
            {
                SpawnWaferDecal();
                decalSpawned = true;
            }

            float remaining = targetEndTime - Time.time;
            if (remaining > 0f)
            {
                if (swerveTimeText != null) swerveTimeText.text = remaining.ToString("F1") + " s";
            }
            else
            {
                if (swerveTimeText != null) swerveTimeText.text = "0.0 s";
                isCountingDown = false;
                
                // The timer has naturally hit zero! Now we push the calculation to the AI model.
                PushTimeToDevelopmentEngine(cachedSwerveTime);
                if (!decalSpawned)
                {
                    SpawnWaferDecal(); // Failsafe if time was less than 5s
                    decalSpawned = true;
                }

                pendingRestore = true;
                restoreTime = Time.time + 1.5f;
            }
        }
        else if (pendingRestore)
        {
            if (Time.time >= restoreTime)
            {
                pendingRestore = false;
                RestoreTimeText();
            }
        }
    }

    public void OnClick_Skip()
    {
        if (isCountingDown)
        {
            // Instantly kill the countdown
            isCountingDown = false;
            targetEndTime = Time.time;
            
            if (swerveTimeText != null) swerveTimeText.text = "0.0 s";

            // Push the calculation immediately
            PushTimeToDevelopmentEngine(cachedSwerveTime);
            if (!decalSpawned) SpawnWaferDecal();

            pendingRestore = true;
            restoreTime = Time.time + 1.5f;

            // Optional: If you want to skip the fluid animations too, you could stop them here.
            if (developerFluid != null) developerFluid.StopAllCoroutines();
            if (diFluid != null) diFluid.StopAllCoroutines();
        }
        else
        {
            // If not counting down, but user clicks skip, just compute right away
            SyncTime(); // ensure cached time is up to date
            PushTimeToDevelopmentEngine(cachedSwerveTime);
        }
    }

    private void RestoreTimeText()
    {
        if (swerveTimeText != null)
        {
            swerveTimeText.text = cachedSwerveTime.ToString("F1") + " s";
        }
    }

    public void UpdateTimeFromSlider(float value)
    {
        // Only allow slider updates if we aren't actively running a countdown
        if (!isCountingDown && !pendingRestore)
        {
            cachedSwerveTime = value;
            if (swerveTimeText != null)
            {
                swerveTimeText.text = value.ToString("F1") + " s";
            }
            // User requested that image calculation ONLY happens on Swerve click, 
            // so we do NOT push this to the DevEngine here!
        }
    }

    private void PushTimeToDevelopmentEngine(float time)
    {
        LithoDevelopmentEngine devEngine = Object.FindAnyObjectByType<LithoDevelopmentEngine>();
        if (devEngine != null)
        {
            devEngine.SetWashTimeAndExecute(time);
        }
    }

    private void SpawnWaferDecal()
    {
        if (developerFluid == null) 
        {
            Debug.LogError("[DeveloperUI] developerFluid is NULL! Cannot spawn pattern.");
            return;
        }
        
        // Locate the physical wafer sitting in the developer basin
        Transform physicalWafer = null;
        foreach (Transform child in developerFluid.transform)
        {
            if (child.name.ToLower().Contains("wafer") || child.name.ToLower().Contains("substrate"))
            {
                physicalWafer = child;
                break;
            }
        }

        if (physicalWafer == null)
        {
            Debug.LogError("[DeveloperUI] Could not find the wafer inside the developerFluid! It might be parented somewhere else.");
            
            // Fallback: search the whole scene
            WaferPatternAnimator[] anims = Object.FindObjectsByType<WaferPatternAnimator>(FindObjectsSortMode.None);
            if (anims.Length > 0) return; // already spawned

            GameObject fallback = GameObject.Find("PR_Wafer");
            if (fallback == null) fallback = GameObject.Find("Substrate");
            if (fallback != null) physicalWafer = fallback.transform;
            
            if (physicalWafer == null)
            {
                Debug.LogError("[DeveloperUI] FALLBACK FAILED. No wafer found in the entire scene.");
                return;
            }
        }

        Debug.Log($"[DeveloperUI] Spawning pattern decal exactly on {physicalWafer.name}");
            // Create a microscopic 3D cylinder to act as a 2D circle decal mask
            GameObject decal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            decal.name = "Developed_Pattern_Decal";
            
            // Destroy physical collider to prevent interaction glitches
            Destroy(decal.GetComponent<Collider>());
            
            // Snap the decal exactly to the wafer's local coordinate space
            decal.transform.SetParent(physicalWafer, false);
            
            // Position it exactly on the top surface of the wafer. 
            decal.transform.localPosition = new Vector3(0f, 1.02f, 0f);
            decal.transform.localRotation = Quaternion.identity;
            
            // Scale the cylinder to match the wafer diameter. 
            // We use 0.05f for thickness because the parent wafer is already scaled down to 0.01f.
            // Using 0.0001f caused the world Y scale to drop to 0.000001f, which Unity culls entirely!
            decal.transform.localScale = new Vector3(1.0f, 0.05f, 1.0f);
            
            if (developedPatternMaterial != null)
            {
                decal.GetComponent<MeshRenderer>().material = developedPatternMaterial;
            }
            else
            {
                Debug.LogWarning("[Developer UI] No material assigned for the pattern decal!");
            }
            
            // Attach the animator script to smoothly fade it in over 2 seconds
            decal.AddComponent<WaferPatternAnimator>();
    }

    public void OnClick_SwerveDeveloper()
    {
        float time = SyncTime();
        if (developerFluid != null)
        {
            developerFluid.uiSwerveDuration = time;
            developerFluid.ExecuteUISwerve();
            
            targetEndTime = Time.time + time;
            swerveStartTime = Time.time;
            decalSpawned = false;
            isCountingDown = true;
            pendingRestore = false;
        }
        else
        {
            Debug.LogWarning("Developer Fluid Controller is not assigned in DeveloperUIManager!");
        }
    }

    public void OnClick_SwerveDI()
    {
        float time = SyncTime();
        if (diFluid != null)
        {
            diFluid.uiSwerveDuration = time;
            diFluid.ExecuteUISwerve();
            
            targetEndTime = Time.time + time;
            swerveStartTime = Time.time;
            decalSpawned = false;
            isCountingDown = true;
            pendingRestore = false;
        }
        else
        {
            Debug.LogWarning("DI Fluid Controller is not assigned in DeveloperUIManager!");
        }
    }

    private float SyncTime()
    {
        if (swerveTimeText != null)
        {
            // Strip any letters or spaces (like "s" or " sec") and just parse the number
            string rawNumbers = Regex.Replace(swerveTimeText.text, "[^0-9.]", "");
            if (float.TryParse(rawNumbers, out float parsed))
            {
                cachedSwerveTime = parsed;
                // Removed PushTimeToDevelopmentEngine(parsed) from here!
                // It will only be pushed when the timer hits zero or Skip is clicked.
            }
        }
        return cachedSwerveTime;
    }
}
