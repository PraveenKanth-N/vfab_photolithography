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

    private float cachedSwerveTime = 30f;
    private float targetEndTime = 0f;
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
            float remaining = targetEndTime - Time.time;
            if (remaining > 0f)
            {
                if (swerveTimeText != null) swerveTimeText.text = remaining.ToString("F1") + " s";
            }
            else
            {
                if (swerveTimeText != null) swerveTimeText.text = "0.0 s";
                isCountingDown = false;
                
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
        }
    }

    public void OnClick_SwerveDeveloper()
    {
        float time = SyncTime();
        if (developerFluid != null)
        {
            developerFluid.uiSwerveDuration = time;
            developerFluid.ExecuteUISwerve();
            
            targetEndTime = Time.time + time;
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
            }
        }
        return cachedSwerveTime;
    }
}
