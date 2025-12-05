using System.Collections;
using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    public TextMeshProUGUI text;
    public float moveUpDistance = 80f;
    public float duration = 0.6f;

    private RectTransform rect;
    private Color startColor;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        if (text == null)
            text = GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
            startColor = text.color;
    }

    public void Init(int amount, bool isCrit)
    {
        if (text == null) return;

        text.text = amount.ToString();
        if (isCrit)
        {
            text.fontSize *= 1.2f;
        }

        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        Vector2 startPos = rect.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0f, moveUpDistance);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = t / duration;

            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, normalized);

            if (text != null)
            {
                Color c = startColor;
                c.a = 1f - normalized;
                text.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}