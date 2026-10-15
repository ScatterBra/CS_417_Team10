using UnityEngine;
using UnityEngine.UI;

// A real world-space canvas: the text stays next to the mechanic that caused it.
public class DEF_SpatialHint : MonoBehaviour
{
    public Text label;
    public Transform anchor;
    public Vector3 worldPoint;
    public float seconds = 4f;
    float expires;
    public void Show(Transform target, Vector3 point, string message)
    {
        anchor = target; worldPoint = point; label.text = message;
        expires = Time.unscaledTime + seconds;
    }
    void LateUpdate()
    {
        if (Time.unscaledTime >= expires) { Destroy(gameObject); return; }
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 point = anchor != null ? anchor.position : worldPoint;
        transform.position = point + Vector3.up * 1.8f;
        transform.rotation = cam.transform.rotation;
        // Keep world text legible from a defense window without growing with screen resolution.
        float distance = Vector3.Distance(transform.position, cam.transform.position);
        float scale = Mathf.Clamp(distance * 0.0014f, 0.004f, 0.022f);
        transform.localScale = Vector3.one * scale;
    }
}
