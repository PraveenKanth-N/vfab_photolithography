using UnityEngine;
using System.Collections;

public class WaferPatternAnimator : MonoBehaviour
{
    public float fadeDuration = 2.0f;
    private Material decalMaterial;

    void Start()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null)
        {
            // Instantiates a unique material instance so we don't fade the global asset
            decalMaterial = mr.material;
            
            // Set initial alpha to 0
            SetMaterialAlpha(0f);
            
            // Begin fade-in
            StartCoroutine(FadeInRoutine());
        }
    }

    private void SetMaterialAlpha(float alpha)
    {
        if (decalMaterial == null) return;
        
        if (decalMaterial.HasProperty("_BaseColor"))
        {
            Color c = decalMaterial.GetColor("_BaseColor");
            c.a = alpha;
            decalMaterial.SetColor("_BaseColor", c);
        }
        else if (decalMaterial.HasProperty("_Color"))
        {
            Color c = decalMaterial.GetColor("_Color");
            c.a = alpha;
            decalMaterial.SetColor("_Color", c);
        }
    }

    private IEnumerator FadeInRoutine()
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / fadeDuration);
            SetMaterialAlpha(t);
            yield return null;
        }
        
        // Ensure it strictly locks to 1.0 at the end
        SetMaterialAlpha(1f);
    }
}
