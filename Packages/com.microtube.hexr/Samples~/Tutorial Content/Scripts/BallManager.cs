using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallManager : MonoBehaviour
{
    public Material targetMaterial; // Assign the material in the Inspector or dynamically
    private Color startColor = Color.yellow;
    private Color endColor = new Color(1f, 0f, 0f, 0.5f); // Red with 50% alpha
    private float duration = 2f;
    private Vector3 originalScale;

    private void Start()
    {
        originalScale = transform.localScale;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            StartCoroutine(TransitionMaterial());
        }
    }
    public void BallIsUsed()
    {
        if (targetMaterial != null)
        {
            StartCoroutine(TransitionMaterial());
        }
        else
        {
            Debug.LogError("Target material is not assigned!");
        }
    }
    private IEnumerator TransitionMaterial()
    {
        // Ensure the material starts as opaque yellow
        targetMaterial.color = startColor;
        targetMaterial.SetFloat("_Mode", 0); // Opaque mode for Standard Shader
        targetMaterial.EnableKeyword("_ALPHABLEND_ON");

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // Lerp color and alpha
            Color currentColor = Color.Lerp(startColor, endColor, t);
            targetMaterial.color = currentColor;

            // Gradually increase size
            transform.localScale = Vector3.Lerp(originalScale, originalScale * 2.5f, t);

            // Gradually transition to transparent rendering mode
            if (t >= 0.5f) // Start transitioning to transparent halfway through
            {
                targetMaterial.SetFloat("_Mode", 3); // Transparent mode for Standard Shader
                targetMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            }

            yield return null;
        }

        // Ensure final state is red with 50% alpha, transparent mode, and increased size
        targetMaterial.color = endColor;
        targetMaterial.SetFloat("_Mode", 3); // Transparent mode
        targetMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        transform.localScale = originalScale * 2.5f;
    }

    public void ResetToOriginalState()
    {
        if (targetMaterial != null)
        {
            // Reset to original opaque yellow state
            targetMaterial.color = startColor;
            targetMaterial.SetFloat("_Mode", 0); // Opaque mode
            targetMaterial.EnableKeyword("_ALPHABLEND_ON");
            targetMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");

            // Reset size to original scale
            transform.localScale = originalScale;
        }
        else
        {
            Debug.LogError("Target material is not assigned!");
        }
    }
}