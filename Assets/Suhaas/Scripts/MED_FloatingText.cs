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
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.fontStyle = FontStyles.Bold;
        t.color = color;
        t.rectTransform.sizeDelta = new Vector2(2f, 0.3f);
        go.AddComponent<MED_Billboard>();
        var f = go.AddComponent<MED_FloatingText>();
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
