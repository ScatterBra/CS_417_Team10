using TMPro;
using UnityEngine;

// World-space text that rises and fades ("+1 Herbs", "Use the Sickle"). Created from code.
public class MED_FloatingText : MonoBehaviour
{
    TextMeshPro label;
    float age;
    float lifetime = 1.1f;
    float riseSpeed = 0.5f;
    Color baseColor;

    public static void Spawn(Vector3 position, string text, Color color, float fontSize = 0.6f)
    {
        var go = new GameObject("MED_FloatingText");
        go.transform.position = position;
        var t = go.AddComponent<TextMeshPro>();
        t.text = text;
        t.fontSize = fontSize;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        t.color = color;
        go.AddComponent<MED_Billboard>();
        var f = go.AddComponent<MED_FloatingText>();
        if (text.Length > 22)
        {
            // Explanatory messages (why it failed + how to fix it): smaller, wrapped, and on screen longer.
            t.fontSize = fontSize * 0.55f;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.rectTransform.sizeDelta = new Vector2(2.6f, 0.5f);
            f.lifetime = 3.2f;
        }
        else
        {
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.rectTransform.sizeDelta = new Vector2(2f, 0.3f);
        }
        f.label = t;
        f.baseColor = color;
    }

    void Update()
    {
        age += Time.deltaTime;
        transform.position += Vector3.up * (riseSpeed * Time.deltaTime);
        float fade = Mathf.Clamp01(1f - (age - lifetime * 0.4f) / (lifetime * 0.6f));
        var c = baseColor;
        c.a = baseColor.a * fade;
        label.color = c;
        if (age >= lifetime) Destroy(gameObject);
    }
}
