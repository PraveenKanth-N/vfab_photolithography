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

    private void Start()
    {
        if (wKey != null) StartCoroutine(AnimateWASD());
        if (mouseGraphic != null) StartCoroutine(AnimateMouse());
    }

    private IEnumerator AnimateWASD()
    {
        RectTransform[] keys = { wKey, aKey, sKey, dKey };
        
        while (true)
        {
            foreach (var key in keys)
            {
                if (key == null) continue;
                
                Image img = key.GetComponent<Image>();
                if (img != null) img.color = activeKeyColor;
                key.localScale = new Vector3(1.1f, 1.1f, 1f);

                yield return new WaitForSeconds(wasdSpeed);

                if (img != null) img.color = defaultKeyColor;
                key.localScale = Vector3.one;
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
            mouseGraphic.localScale = new Vector3(0.9f, 0.9f, 1f);
            
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

            mouseGraphic.localScale = Vector3.one;

            // Pause before next loop
            yield return new WaitForSeconds(0.5f);
        }
    }
}
