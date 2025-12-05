using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ImpactEffect : MonoBehaviour
{
    public float duration = 0.25f;
    public float maxScale = 1.6f;
    public Image image;

    private RectTransform rect;
    private Color startColor;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        if (image == null)
            image = GetComponent<Image>();
        if (image != null)
            startColor = image.color;
    }

    public void Init(Color color)
    {
        if (image != null)
        {
            image.color = color;
            startColor = color;
        }
        StartCoroutine(DoAnim());
    }

    private IEnumerator DoAnim()
    {
        float t = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one * maxScale;

        while (t < duration)
        {
            t += Time.deltaTime;
            float n = t / duration;

            rect.localScale = Vector3.Lerp(startScale, endScale, n);

            if (image != null)
            {
                Color c = startColor;
                c.a = 1f - n;
                image.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}