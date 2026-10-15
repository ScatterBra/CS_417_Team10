using UnityEngine;
using UnityEngine.InputSystem;

public class DEF_CameraRig : MonoBehaviour
{
    public float panSpeed = 25f;
    public float zoomSpeed = 25f;
    public float rotateSpeed = 90f;
    public float minHeight = 8f;
    public float maxHeight = 50f;
    public float startHeight = 30f;
    public float bound = 22f;

    Camera cam;
    float height;
    float yaw;

    void Start()
    {
        cam = GetComponentInChildren<Camera>();
        height = startHeight;
    }

    void Update()
    {
        if (DEF_VRInput.Active || UnityEngine.XR.XRSettings.isDeviceActive) return;
        var kb = Keyboard.current;
        var ms = Mouse.current;
        if (kb == null || ms == null || cam == null) return;
        float dt = Time.unscaledDeltaTime;

        Vector3 fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        fwd.y = 0f;
        right.y = 0f;
        fwd.Normalize();
        right.Normalize();

        Vector3 move = Vector3.zero;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move += fwd;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move -= fwd;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move += right;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move -= right;
        transform.position += move.normalized * panSpeed * dt;

        if (ms.rightButton.isPressed)
        {
            Vector2 d = ms.delta.ReadValue();
            transform.position -= (right * d.x + fwd * d.y) * (height / 500f);
        }

        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, -bound, bound);
        p.z = Mathf.Clamp(p.z, -bound, bound);
        p.y = 0f;
        transform.position = p;

        float notches = ms.scroll.ReadValue().y / 120f;
        float keys = 0f;
        if (kb.equalsKey.isPressed || kb.numpadPlusKey.isPressed) keys += 1f;
        if (kb.minusKey.isPressed || kb.numpadMinusKey.isPressed) keys -= 1f;
        height = Mathf.Clamp(height - notches * 2.5f - keys * zoomSpeed * dt, minHeight, maxHeight);

        if (kb.qKey.isPressed) yaw += rotateSpeed * dt;
        if (kb.eKey.isPressed) yaw -= rotateSpeed * dt;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (kb.pKey.wasPressedThisFrame) ToggleProjection();

        cam.transform.localPosition = new Vector3(0f, height, -height * 0.55f);
        cam.transform.LookAt(transform.position + Vector3.up * 2f);
        if (cam.orthographic) cam.orthographicSize = height * 0.6f;
    }

    public void ToggleProjection()
    {
        if (cam != null) cam.orthographic = !cam.orthographic;
    }

    public bool IsOrtho()
    {
        return cam != null && cam.orthographic;
    }
}
