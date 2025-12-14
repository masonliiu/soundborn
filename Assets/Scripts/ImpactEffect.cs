using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ImpactEffect : MonoBehaviour
{
    public float duration = 0.25f;
    public float maxScale = 1.6f;
    public Image image;

    private RectTransform rect;
    private Color pendingColor = Color.white;
    private bool pendingPlay = false;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public void Init(Color color)
    {
        pendingColor = color;
        pendingPlay = true;

        if (image != null)
            image.color = pendingColor;

        if (gameObject.activeInHierarchy)
        {
            pendingPlay = false;
            StartCoroutine(Animate());
        }
    }

    private void OnEnable()
    {
        if (pendingPlay)
        {
            pendingPlay = false;
            StartCoroutine(Animate());
        }
    }

    private IEnumerator Animate()
    {
        if (rect == null) yield break;

        Vector3 startScale = Vector3.one;
        Vector3 endScale = Vector3.one * maxScale;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);

            rect.localScale = Vector3.Lerp(startScale, endScale, n);

            if (image != null)
            {
                var c = pendingColor;
                c.a = 1f - n;
                image.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}