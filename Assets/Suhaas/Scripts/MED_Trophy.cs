using System.Collections;
using TMPro;
using UnityEngine;

// An achievement trophy: a gold cup (primitives) with a small world-space label. Pops in when earned and turns slowly.
public class MED_Trophy : MonoBehaviour
{
    [SerializeField] TextMeshPro label;
    [SerializeField] Transform cup;
    [SerializeField] float spinDegreesPerSecond = 25f;

    public MED_Achievement Id { get; private set; }

    public void Show(MED_Achievement id, string title, string hint)
    {
        Id = id;
        if (label != null) label.text = $"<b>{title}</b>\n<size=70%>{hint}</size>";
        MED_FloatingText.Spawn(transform.position + Vector3.up * 0.8f, "Achievement: " + title, new Color(1f, 0.85f, 0.25f), 0.6f);
        StartCoroutine(Pop());
    }

    void Update()
    {
        if (cup != null) cup.Rotate(0f, spinDegreesPerSecond * Time.deltaTime, 0f, Space.Self);
    }

    IEnumerator Pop()
    {
        var target = transform.localScale;
        const float duration = 0.6f, c1 = 1.70158f, c3 = c1 + 1f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float x = t / duration - 1f;
            transform.localScale = target * Mathf.Max(0.01f, 1f + c3 * x * x * x + c1 * x * x);   // ease-out-back
            yield return null;
        }
        transform.localScale = target;
    }
}
