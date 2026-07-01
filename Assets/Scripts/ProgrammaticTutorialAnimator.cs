using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ProgrammaticTutorialAnimator : MonoBehaviour
{
    [Header("WASD Keys")]
    public RectTransform wKey;
    public RectTransform aKey;
    public RectTransform sKey;
    public RectTransform dKey;
    public Color activeKeyColor = Color.cyan;
    public Color defaultKeyColor = Color.white;

    [Header("Mouse Setup")]
    public RectTransform mouseGraphic;
    public RectTransform leftClickIndicator; // A small circle/pulse ring

    [Header("Animation Settings")]
    public float wasdSpeed = 0.5f; // time per key
    public float mousePanDistance = 30f;
    public float mousePanSpeed = 1f;

    private Vector3[] originalKeyScales;
    private Vector3 originalMouseScale;

    private void Start()
    {
        originalKeyScales = new Vector3[4];
        if (wKey != null) originalKeyScales[0] = wKey.localScale;
        if (aKey != null) originalKeyScales[1] = aKey.localScale;
        if (sKey != null) originalKeyScales[2] = sKey.localScale;
        if (dKey != null) originalKeyScales[3] = dKey.localScale;

        if (mouseGraphic != null)
        {
            originalMouseScale = mouseGraphic.localScale;
            StartCoroutine(AnimateMouse());
        }
        if (wKey != null) StartCoroutine(AnimateWASD());
    }

    private IEnumerator AnimateWASD()
    {
        RectTransform[] keys = { wKey, aKey, sKey, dKey };
        
        while (true)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                if (key == null) continue;
                
                Image img = key.GetComponent<Image>();
                if (img != null) img.color = activeKeyColor;
                key.localScale = originalKeyScales[i] * 1.1f;

                yield return new WaitForSeconds(wasdSpeed);

                if (img != null) img.color = defaultKeyColor;
                key.localScale = originalKeyScales[i];
            }
            // Small pause before looping
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator AnimateMouse()
    {
        Vector3 startPos = mouseGraphic.anchoredPosition;
        if (leftClickIndicator != null) leftClickIndicator.gameObject.SetActive(false);

        while (true)
        {
            // 1. Pan Left
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * mousePanSpeed;
                mouseGraphic.anchoredPosition = Vector3.Lerp(startPos, startPos + new Vector3(-mousePanDistance, 0, 0), Mathf.SmoothStep(0, 1, t));
                yield return null;
            }

            // 2. Pan Right
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * mousePanSpeed;
                mouseGraphic.anchoredPosition = Vector3.Lerp(startPos + new Vector3(-mousePanDistance, 0, 0), startPos + new Vector3(mousePanDistance, 0, 0), Mathf.SmoothStep(0, 1, t));
                yield return null;
            }

            // 3. Pan Center
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * mousePanSpeed;
                mouseGraphic.anchoredPosition = Vector3.Lerp(startPos + new Vector3(mousePanDistance, 0, 0), startPos, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }

            // 4. Click (Scale down slightly, emit pulse)
            mouseGraphic.localScale = originalMouseScale * 0.9f;
            
            if (leftClickIndicator != null)
            {
                leftClickIndicator.gameObject.SetActive(true);
                leftClickIndicator.localScale = Vector3.zero;
                
                Image clickImg = leftClickIndicator.GetComponent<Image>();
                if (clickImg != null)
                {
                    Color c = clickImg.color;
                    c.a = 1f;
                    clickImg.color = c;
                }

                float pulseT = 0f;
                while (pulseT < 1f)
                {
                    pulseT += Time.deltaTime * 3f;
                    leftClickIndicator.localScale = Vector3.Lerp(Vector3.zero, new Vector3(2f, 2f, 1f), pulseT);
                    
                    if (clickImg != null)
                    {
                        Color c = clickImg.color;
                        c.a = Mathf.Lerp(1f, 0f, pulseT);
                        clickImg.color = c;
                    }
                    yield return null;
                }
                leftClickIndicator.gameObject.SetActive(false);
            }
            else
            {
                yield return new WaitForSeconds(0.3f);
            }

            mouseGraphic.localScale = originalMouseScale;

            // Pause before next loop
            yield return new WaitForSeconds(0.5f);
        }
    }
}
